using Microsoft.Extensions.Logging;
using TzarBot.NeuralNetwork.Models;

namespace TzarBot.Training.Tournament;

/// <summary>
/// Manages Swiss-system tournaments for genome evaluation.
///
/// Swiss-system tournament:
/// - Players are paired based on similar scores/ratings
/// - Each round, players with similar standings play
/// - No elimination - all players play all rounds
/// - Good for ranking without requiring n*(n-1)/2 matches
///
/// Pairing rules:
/// 1. Sort by points/rating
/// 2. Split into upper and lower halves
/// 3. Pair top of upper with top of lower
/// 4. Avoid rematches when possible
/// </summary>
public sealed class TournamentManager : ITournamentManager
{
    private readonly ILogger<TournamentManager>? _logger;
    private readonly EloCalculator _eloCalculator;
    private readonly int _totalRounds;
    private readonly int _initialRating;

    private readonly Dictionary<Guid, int> _ratings = new();
    private readonly Dictionary<Guid, TournamentStanding> _standings = new();
    private readonly List<MatchResult> _matches = new();
    private readonly HashSet<(Guid, Guid)> _playedPairs = new();
    private int _currentRound;

    /// <inheritdoc />
    public TournamentState State => new()
    {
        CurrentRound = _currentRound,
        TotalRounds = _totalRounds,
        MatchesCompleted = _matches.Count,
        MatchesRemaining = 0 // Calculated per round
    };

    /// <inheritdoc />
    public EloCalculator EloCalculator => _eloCalculator;

    /// <summary>
    /// Creates a tournament manager.
    /// </summary>
    public TournamentManager(
        int totalRounds = 5,
        int initialRating = 1000,
        int kFactor = 32,
        ILogger<TournamentManager>? logger = null)
    {
        _totalRounds = totalRounds;
        _initialRating = initialRating;
        _eloCalculator = new EloCalculator(kFactor, initialRating);
        _logger = logger;
    }

    /// <inheritdoc />
    public int GetRating(Guid genomeId)
    {
        return _ratings.GetValueOrDefault(genomeId, _initialRating);
    }

    /// <inheritdoc />
    public void SetRating(Guid genomeId, int rating)
    {
        _ratings[genomeId] = rating;
        EnsureStanding(genomeId).Rating = rating;
    }

    /// <inheritdoc />
    public IReadOnlyList<(Guid Genome1, Guid Genome2)> GeneratePairings(IReadOnlyList<NetworkGenome> genomes)
    {
        if (genomes.Count < 2)
            return Array.Empty<(Guid, Guid)>();

        _currentRound++;

        // Ensure all genomes have ratings and standings
        foreach (var genome in genomes)
        {
            if (!_ratings.ContainsKey(genome.Id))
            {
                _ratings[genome.Id] = _initialRating;
            }
            EnsureStanding(genome.Id);
        }

        // Sort by rating/points for Swiss pairing
        var sorted = genomes
            .OrderByDescending(g => GetRating(g.Id))
            .ThenByDescending(g => _standings.GetValueOrDefault(g.Id)?.Points ?? 0)
            .ToList();

        var pairings = new List<(Guid, Guid)>();
        var paired = new HashSet<Guid>();

        // Swiss pairing: pair similar-ranked players
        for (int i = 0; i < sorted.Count; i++)
        {
            if (paired.Contains(sorted[i].Id))
                continue;

            var player1 = sorted[i];

            // Find best opponent (similar rating, not yet paired, not rematched if possible)
            NetworkGenome? bestOpponent = null;
            int bestDiff = int.MaxValue;
            bool foundNewPair = false;

            for (int j = i + 1; j < sorted.Count; j++)
            {
                if (paired.Contains(sorted[j].Id))
                    continue;

                var candidate = sorted[j];
                var pairKey = MakePairKey(player1.Id, candidate.Id);
                bool isRematch = _playedPairs.Contains(pairKey);

                int ratingDiff = Math.Abs(GetRating(player1.Id) - GetRating(candidate.Id));

                // Prefer new pairings over rematches
                if (!isRematch && !foundNewPair)
                {
                    bestOpponent = candidate;
                    bestDiff = ratingDiff;
                    foundNewPair = true;
                }
                else if (isRematch == !foundNewPair && ratingDiff < bestDiff)
                {
                    bestOpponent = candidate;
                    bestDiff = ratingDiff;
                }
            }

            if (bestOpponent != null)
            {
                pairings.Add((player1.Id, bestOpponent.Id));
                paired.Add(player1.Id);
                paired.Add(bestOpponent.Id);
            }
        }

        _logger?.LogDebug("Round {Round}: Generated {Count} pairings from {Total} genomes",
            _currentRound, pairings.Count, genomes.Count);

        return pairings;
    }

    /// <inheritdoc />
    public void RecordMatch(MatchResult result)
    {
        // Calculate and apply ELO changes
        int rating1 = GetRating(result.Genome1Id);
        int rating2 = GetRating(result.Genome2Id);

        _eloCalculator.UpdateMatchWithEloChanges(result, rating1, rating2);

        var (newRating1, newRating2) = _eloCalculator.CalculateNewRatings(
            rating1, rating2, result.Outcome);

        _ratings[result.Genome1Id] = newRating1;
        _ratings[result.Genome2Id] = newRating2;

        // Update standings
        var standing1 = EnsureStanding(result.Genome1Id);
        var standing2 = EnsureStanding(result.Genome2Id);

        standing1.Rating = newRating1;
        standing2.Rating = newRating2;
        standing1.TotalEloChange += result.EloChange1;
        standing2.TotalEloChange += result.EloChange2;

        switch (result.Outcome)
        {
            case MatchOutcome.Win:
                standing1.Wins++;
                standing2.Losses++;
                break;
            case MatchOutcome.Loss:
                standing1.Losses++;
                standing2.Wins++;
                break;
            default:
                standing1.Draws++;
                standing2.Draws++;
                break;
        }

        // Record the pairing
        _playedPairs.Add(MakePairKey(result.Genome1Id, result.Genome2Id));
        _matches.Add(result);

        _logger?.LogDebug("Match recorded: {Result}, ELO: {R1}({C1:+#;-#;0})->{NR1}, {R2}({C2:+#;-#;0})->{NR2}",
            result, rating1, result.EloChange1, newRating1, rating2, result.EloChange2, newRating2);
    }

    /// <inheritdoc />
    public IReadOnlyList<TournamentStanding> GetStandings()
    {
        var standings = _standings.Values.ToList();

        // Sort by rating, then by points
        standings.Sort((a, b) =>
        {
            int cmp = b.Rating.CompareTo(a.Rating);
            if (cmp != 0) return cmp;
            return b.Points.CompareTo(a.Points);
        });

        // Assign ranks
        for (int i = 0; i < standings.Count; i++)
        {
            standings[i].Rank = i + 1;
        }

        return standings;
    }

    /// <inheritdoc />
    public void ResetTournament(bool preserveRatings = true)
    {
        _currentRound = 0;
        _matches.Clear();
        _playedPairs.Clear();

        if (preserveRatings)
        {
            // Keep ratings but reset match stats
            foreach (var standing in _standings.Values)
            {
                standing.Wins = 0;
                standing.Losses = 0;
                standing.Draws = 0;
                standing.TotalEloChange = 0;
            }
        }
        else
        {
            _ratings.Clear();
            _standings.Clear();
        }

        _logger?.LogInformation("Tournament reset (preserveRatings={Preserve})", preserveRatings);
    }

    /// <inheritdoc />
    public IReadOnlyList<MatchResult> GetMatchHistory(Guid genomeId)
    {
        return _matches
            .Where(m => m.Genome1Id == genomeId || m.Genome2Id == genomeId)
            .OrderByDescending(m => m.Timestamp)
            .ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<MatchResult> GetAllMatches()
    {
        return _matches.AsReadOnly();
    }

    /// <inheritdoc />
    public float CalculateTournamentFitness(Guid genomeId)
    {
        var standing = _standings.GetValueOrDefault(genomeId);
        if (standing == null)
            return 0f;

        // Fitness based on:
        // 1. ELO rating (normalized around initial)
        // 2. Win rate
        // 3. Points

        float ratingComponent = (standing.Rating - _initialRating) / 100f;
        float winRateComponent = standing.WinRate * 50f;
        float pointsComponent = standing.Points * 10f;

        return ratingComponent + winRateComponent + pointsComponent;
    }

    /// <summary>
    /// Loads ratings from a dictionary (e.g., from checkpoint).
    /// </summary>
    public void LoadRatings(Dictionary<Guid, int> ratings)
    {
        foreach (var (genomeId, rating) in ratings)
        {
            _ratings[genomeId] = rating;
            EnsureStanding(genomeId).Rating = rating;
        }

        _logger?.LogInformation("Loaded {Count} ratings", ratings.Count);
    }

    /// <summary>
    /// Exports current ratings.
    /// </summary>
    public Dictionary<Guid, int> ExportRatings()
    {
        return new Dictionary<Guid, int>(_ratings);
    }

    /// <summary>
    /// Gets or creates a standing for a genome.
    /// </summary>
    private TournamentStanding EnsureStanding(Guid genomeId)
    {
        if (!_standings.TryGetValue(genomeId, out var standing))
        {
            standing = new TournamentStanding
            {
                GenomeId = genomeId,
                Rating = _ratings.GetValueOrDefault(genomeId, _initialRating)
            };
            _standings[genomeId] = standing;
        }
        return standing;
    }

    /// <summary>
    /// Creates a canonical pair key for rematch tracking.
    /// </summary>
    private static (Guid, Guid) MakePairKey(Guid id1, Guid id2)
    {
        return id1.CompareTo(id2) < 0 ? (id1, id2) : (id2, id1);
    }
}
