using TzarBot.NeuralNetwork.Models;

namespace TzarBot.Training.Tournament;

/// <summary>
/// Interface for tournament management.
/// Organizes matches between genomes for fitness evaluation.
/// </summary>
public interface ITournamentManager
{
    /// <summary>
    /// Current tournament state.
    /// </summary>
    TournamentState State { get; }

    /// <summary>
    /// ELO calculator instance.
    /// </summary>
    EloCalculator EloCalculator { get; }

    /// <summary>
    /// Gets the current ELO rating for a genome.
    /// </summary>
    int GetRating(Guid genomeId);

    /// <summary>
    /// Sets the ELO rating for a genome.
    /// </summary>
    void SetRating(Guid genomeId, int rating);

    /// <summary>
    /// Generates pairings for the next round using Swiss-system.
    /// </summary>
    /// <param name="genomes">Genomes to pair.</param>
    /// <returns>List of pairings (genome1, genome2).</returns>
    IReadOnlyList<(Guid Genome1, Guid Genome2)> GeneratePairings(IReadOnlyList<NetworkGenome> genomes);

    /// <summary>
    /// Records a match result and updates ratings.
    /// </summary>
    /// <param name="result">Match result.</param>
    void RecordMatch(MatchResult result);

    /// <summary>
    /// Gets the current standings (sorted by rating).
    /// </summary>
    IReadOnlyList<TournamentStanding> GetStandings();

    /// <summary>
    /// Resets the tournament for a new generation.
    /// </summary>
    /// <param name="preserveRatings">Whether to keep existing ratings.</param>
    void ResetTournament(bool preserveRatings = true);

    /// <summary>
    /// Gets match history for a specific genome.
    /// </summary>
    IReadOnlyList<MatchResult> GetMatchHistory(Guid genomeId);

    /// <summary>
    /// Gets all matches from the current tournament.
    /// </summary>
    IReadOnlyList<MatchResult> GetAllMatches();

    /// <summary>
    /// Calculates fitness based on ELO rating and tournament performance.
    /// </summary>
    float CalculateTournamentFitness(Guid genomeId);
}

/// <summary>
/// Current state of the tournament.
/// </summary>
public sealed class TournamentState
{
    /// <summary>
    /// Current round number.
    /// </summary>
    public int CurrentRound { get; set; }

    /// <summary>
    /// Total rounds planned.
    /// </summary>
    public int TotalRounds { get; set; }

    /// <summary>
    /// Number of matches completed.
    /// </summary>
    public int MatchesCompleted { get; set; }

    /// <summary>
    /// Number of matches remaining in current round.
    /// </summary>
    public int MatchesRemaining { get; set; }

    /// <summary>
    /// Whether the tournament is complete.
    /// </summary>
    public bool IsComplete => CurrentRound >= TotalRounds && MatchesRemaining == 0;
}

/// <summary>
/// Standing of a genome in the tournament.
/// </summary>
public sealed class TournamentStanding
{
    /// <summary>
    /// Genome ID.
    /// </summary>
    public required Guid GenomeId { get; init; }

    /// <summary>
    /// Current ELO rating.
    /// </summary>
    public int Rating { get; set; }

    /// <summary>
    /// Number of wins.
    /// </summary>
    public int Wins { get; set; }

    /// <summary>
    /// Number of losses.
    /// </summary>
    public int Losses { get; set; }

    /// <summary>
    /// Number of draws.
    /// </summary>
    public int Draws { get; set; }

    /// <summary>
    /// Total matches played.
    /// </summary>
    public int MatchesPlayed => Wins + Losses + Draws;

    /// <summary>
    /// Win rate.
    /// </summary>
    public float WinRate => MatchesPlayed > 0 ? (float)Wins / MatchesPlayed : 0f;

    /// <summary>
    /// Points (Win=1, Draw=0.5, Loss=0).
    /// </summary>
    public float Points => Wins + Draws * 0.5f;

    /// <summary>
    /// Current rank (1-based).
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// Total ELO change from all matches.
    /// </summary>
    public int TotalEloChange { get; set; }

    public override string ToString()
    {
        return $"#{Rank} [{GenomeId:N8}] Rating={Rating}, W/L/D={Wins}/{Losses}/{Draws}";
    }
}
