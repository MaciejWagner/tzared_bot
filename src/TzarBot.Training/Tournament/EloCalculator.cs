namespace TzarBot.Training.Tournament;

/// <summary>
/// Calculates ELO ratings for genome matches.
///
/// ELO Rating System:
/// - Uses the standard FIDE formula for expected score and rating change
/// - K-factor determines rating volatility (higher = faster changes)
/// - Supports wins, losses, and draws
///
/// Mathematics:
/// Expected score: E = 1 / (1 + 10^((R_opponent - R_player) / 400))
/// New rating: R' = R + K * (S - E)
/// where S = 1 for win, 0.5 for draw, 0 for loss
/// </summary>
public sealed class EloCalculator
{
    private readonly int _kFactor;
    private readonly int _initialRating;
    private readonly int _minRating;
    private readonly int _maxRating;

    /// <summary>
    /// Default K-factor (moderate volatility).
    /// </summary>
    public const int DefaultKFactor = 32;

    /// <summary>
    /// Default initial rating.
    /// </summary>
    public const int DefaultInitialRating = 1000;

    /// <summary>
    /// K-factor for rating calculation.
    /// Higher values = more volatile ratings.
    /// </summary>
    public int KFactor => _kFactor;

    /// <summary>
    /// Initial rating for new genomes.
    /// </summary>
    public int InitialRating => _initialRating;

    /// <summary>
    /// Creates an ELO calculator with specified parameters.
    /// </summary>
    /// <param name="kFactor">K-factor (default: 32)</param>
    /// <param name="initialRating">Initial rating (default: 1000)</param>
    /// <param name="minRating">Minimum allowed rating (default: 100)</param>
    /// <param name="maxRating">Maximum allowed rating (default: 3000)</param>
    public EloCalculator(
        int kFactor = DefaultKFactor,
        int initialRating = DefaultInitialRating,
        int minRating = 100,
        int maxRating = 3000)
    {
        if (kFactor < 1)
            throw new ArgumentOutOfRangeException(nameof(kFactor), "K-factor must be positive");
        if (initialRating < minRating || initialRating > maxRating)
            throw new ArgumentOutOfRangeException(nameof(initialRating), "Initial rating must be within min/max range");
        if (minRating >= maxRating)
            throw new ArgumentException("Min rating must be less than max rating");

        _kFactor = kFactor;
        _initialRating = initialRating;
        _minRating = minRating;
        _maxRating = maxRating;
    }

    /// <summary>
    /// Calculates the expected score for a player.
    ///
    /// The expected score is the probability of winning plus half the probability of drawing.
    /// It ranges from 0 (certain loss) to 1 (certain win).
    ///
    /// Formula: E = 1 / (1 + 10^((R_opponent - R_player) / 400))
    /// </summary>
    /// <param name="playerRating">Rating of the player.</param>
    /// <param name="opponentRating">Rating of the opponent.</param>
    /// <returns>Expected score (0 to 1).</returns>
    public float CalculateExpectedScore(int playerRating, int opponentRating)
    {
        double exponent = (opponentRating - playerRating) / 400.0;
        return (float)(1.0 / (1.0 + Math.Pow(10, exponent)));
    }

    /// <summary>
    /// Calculates the rating change for a match result.
    ///
    /// Formula: delta = K * (S - E)
    /// where S = actual score (1=win, 0.5=draw, 0=loss)
    /// and E = expected score
    /// </summary>
    /// <param name="playerRating">Current rating of the player.</param>
    /// <param name="opponentRating">Rating of the opponent.</param>
    /// <param name="outcome">Match outcome from player's perspective.</param>
    /// <returns>Rating change (can be negative).</returns>
    public int CalculateRatingChange(int playerRating, int opponentRating, MatchOutcome outcome)
    {
        float actualScore = GetActualScore(outcome);
        float expectedScore = CalculateExpectedScore(playerRating, opponentRating);

        float change = _kFactor * (actualScore - expectedScore);

        // Round to nearest integer
        return (int)Math.Round(change);
    }

    /// <summary>
    /// Calculates new ratings for both players after a match.
    /// </summary>
    /// <param name="rating1">Player 1's current rating.</param>
    /// <param name="rating2">Player 2's current rating.</param>
    /// <param name="outcome">Match outcome from player 1's perspective.</param>
    /// <returns>Tuple of (new rating 1, new rating 2).</returns>
    public (int NewRating1, int NewRating2) CalculateNewRatings(
        int rating1,
        int rating2,
        MatchOutcome outcome)
    {
        int change1 = CalculateRatingChange(rating1, rating2, outcome);
        int change2 = CalculateRatingChange(rating2, rating1, InvertOutcome(outcome));

        int newRating1 = Clamp(rating1 + change1);
        int newRating2 = Clamp(rating2 + change2);

        return (newRating1, newRating2);
    }

    /// <summary>
    /// Updates a match result with ELO changes.
    /// </summary>
    /// <param name="result">Match result to update.</param>
    /// <param name="rating1">Player 1's current rating.</param>
    /// <param name="rating2">Player 2's current rating.</param>
    public void UpdateMatchWithEloChanges(MatchResult result, int rating1, int rating2)
    {
        result.EloChange1 = CalculateRatingChange(rating1, rating2, result.Outcome);
        result.EloChange2 = CalculateRatingChange(rating2, rating1, InvertOutcome(result.Outcome));
    }

    /// <summary>
    /// Gets the actual score for an outcome.
    /// </summary>
    private static float GetActualScore(MatchOutcome outcome)
    {
        return outcome switch
        {
            MatchOutcome.Win => 1.0f,
            MatchOutcome.Loss => 0.0f,
            MatchOutcome.Draw => 0.5f,
            MatchOutcome.Timeout => 0.5f, // Treat timeout as draw
            MatchOutcome.Error => 0.5f,   // Treat error as draw
            _ => 0.5f
        };
    }

    /// <summary>
    /// Inverts an outcome (win becomes loss, etc.)
    /// </summary>
    private static MatchOutcome InvertOutcome(MatchOutcome outcome)
    {
        return outcome switch
        {
            MatchOutcome.Win => MatchOutcome.Loss,
            MatchOutcome.Loss => MatchOutcome.Win,
            _ => outcome
        };
    }

    /// <summary>
    /// Clamps a rating to the valid range.
    /// </summary>
    private int Clamp(int rating)
    {
        return Math.Max(_minRating, Math.Min(_maxRating, rating));
    }

    /// <summary>
    /// Calculates the win probability for a player.
    /// This is a simplified version assuming no draws.
    /// </summary>
    public float CalculateWinProbability(int playerRating, int opponentRating)
    {
        return CalculateExpectedScore(playerRating, opponentRating);
    }

    /// <summary>
    /// Estimates how many games would be needed to reach a target rating.
    /// Assumes all wins against opponents with average rating.
    /// </summary>
    public int EstimateGamesToReach(int currentRating, int targetRating, int averageOpponentRating)
    {
        if (currentRating >= targetRating)
            return 0;

        int games = 0;
        int rating = currentRating;

        while (rating < targetRating && games < 1000)
        {
            int gain = CalculateRatingChange(rating, averageOpponentRating, MatchOutcome.Win);
            rating += Math.Max(1, gain);
            games++;
        }

        return games;
    }

    /// <summary>
    /// Gets a performance rating based on tournament results.
    /// This is the rating at which the expected score equals the actual score.
    /// </summary>
    public int CalculatePerformanceRating(IReadOnlyList<(int OpponentRating, MatchOutcome Outcome)> results)
    {
        if (results.Count == 0)
            return _initialRating;

        float totalScore = results.Sum(r => GetActualScore(r.Outcome));
        float scorePercentage = totalScore / results.Count;

        // Avoid extreme scores
        scorePercentage = Math.Max(0.01f, Math.Min(0.99f, scorePercentage));

        // Average opponent rating
        double avgOpponentRating = results.Average(r => (double)r.OpponentRating);

        // Inverse of expected score formula
        // E = 1 / (1 + 10^(d/400)) => d = 400 * log10((1/E) - 1)
        // where d = opponent_rating - performance_rating
        double d = 400 * Math.Log10((1 / scorePercentage) - 1);

        return (int)Math.Round(avgOpponentRating - d);
    }
}
