using MessagePack;

namespace TzarBot.Training.Tournament;

/// <summary>
/// Represents the result of a match between two genomes.
/// </summary>
[MessagePackObject]
public sealed class MatchResult
{
    /// <summary>
    /// Unique identifier for this match.
    /// </summary>
    [Key(0)]
    public Guid MatchId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID of the first genome (player 1).
    /// </summary>
    [Key(1)]
    public Guid Genome1Id { get; set; }

    /// <summary>
    /// ID of the second genome (player 2).
    /// </summary>
    [Key(2)]
    public Guid Genome2Id { get; set; }

    /// <summary>
    /// ID of the winning genome (null = draw).
    /// </summary>
    [Key(3)]
    public Guid? WinnerId { get; set; }

    /// <summary>
    /// Outcome from genome 1's perspective.
    /// </summary>
    [Key(4)]
    public MatchOutcome Outcome { get; set; }

    /// <summary>
    /// Game duration.
    /// </summary>
    [Key(5)]
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// When the match was played.
    /// </summary>
    [Key(6)]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Tournament round number (for Swiss-system).
    /// </summary>
    [Key(7)]
    public int Round { get; set; }

    /// <summary>
    /// Score for genome 1 (game-specific metric).
    /// </summary>
    [Key(8)]
    public float Score1 { get; set; }

    /// <summary>
    /// Score for genome 2 (game-specific metric).
    /// </summary>
    [Key(9)]
    public float Score2 { get; set; }

    /// <summary>
    /// ELO rating change for genome 1.
    /// </summary>
    [Key(10)]
    public int EloChange1 { get; set; }

    /// <summary>
    /// ELO rating change for genome 2.
    /// </summary>
    [Key(11)]
    public int EloChange2 { get; set; }

    /// <summary>
    /// Whether the match was completed normally.
    /// </summary>
    [Key(12)]
    public bool WasCompleted { get; set; } = true;

    /// <summary>
    /// Error message if match failed.
    /// </summary>
    [Key(13)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creates a result for a win by genome 1.
    /// </summary>
    public static MatchResult Win1(Guid genome1Id, Guid genome2Id, TimeSpan duration, int round = 0)
    {
        return new MatchResult
        {
            Genome1Id = genome1Id,
            Genome2Id = genome2Id,
            WinnerId = genome1Id,
            Outcome = MatchOutcome.Win,
            Duration = duration,
            Round = round
        };
    }

    /// <summary>
    /// Creates a result for a win by genome 2.
    /// </summary>
    public static MatchResult Win2(Guid genome1Id, Guid genome2Id, TimeSpan duration, int round = 0)
    {
        return new MatchResult
        {
            Genome1Id = genome1Id,
            Genome2Id = genome2Id,
            WinnerId = genome2Id,
            Outcome = MatchOutcome.Loss,
            Duration = duration,
            Round = round
        };
    }

    /// <summary>
    /// Creates a result for a draw.
    /// </summary>
    public static MatchResult Draw(Guid genome1Id, Guid genome2Id, TimeSpan duration, int round = 0)
    {
        return new MatchResult
        {
            Genome1Id = genome1Id,
            Genome2Id = genome2Id,
            WinnerId = null,
            Outcome = MatchOutcome.Draw,
            Duration = duration,
            Round = round
        };
    }

    /// <summary>
    /// Creates a result for a timeout (treated as draw).
    /// </summary>
    public static MatchResult Timeout(Guid genome1Id, Guid genome2Id, TimeSpan duration, int round = 0)
    {
        return new MatchResult
        {
            Genome1Id = genome1Id,
            Genome2Id = genome2Id,
            WinnerId = null,
            Outcome = MatchOutcome.Timeout,
            Duration = duration,
            Round = round,
            WasCompleted = false
        };
    }

    /// <summary>
    /// Gets the outcome from the perspective of a specific genome.
    /// </summary>
    public MatchOutcome GetOutcomeFor(Guid genomeId)
    {
        if (genomeId == Genome1Id)
            return Outcome;

        if (genomeId == Genome2Id)
        {
            return Outcome switch
            {
                MatchOutcome.Win => MatchOutcome.Loss,
                MatchOutcome.Loss => MatchOutcome.Win,
                _ => Outcome // Draw, Timeout, Error stay the same
            };
        }

        throw new ArgumentException($"Genome {genomeId} was not in this match");
    }

    /// <summary>
    /// Gets the ELO change for a specific genome.
    /// </summary>
    public int GetEloChangeFor(Guid genomeId)
    {
        if (genomeId == Genome1Id)
            return EloChange1;
        if (genomeId == Genome2Id)
            return EloChange2;

        throw new ArgumentException($"Genome {genomeId} was not in this match");
    }

    public override string ToString()
    {
        var outcomeStr = Outcome switch
        {
            MatchOutcome.Win => $"{Genome1Id:N8} wins",
            MatchOutcome.Loss => $"{Genome2Id:N8} wins",
            MatchOutcome.Draw => "Draw",
            MatchOutcome.Timeout => "Timeout",
            MatchOutcome.Error => "Error",
            _ => "Unknown"
        };

        return $"Match[R{Round}] {Genome1Id:N8} vs {Genome2Id:N8}: {outcomeStr} ({Duration.TotalSeconds:F0}s)";
    }
}

/// <summary>
/// Outcome of a match from one player's perspective.
/// </summary>
public enum MatchOutcome
{
    /// <summary>
    /// Player won the match.
    /// </summary>
    Win,

    /// <summary>
    /// Player lost the match.
    /// </summary>
    Loss,

    /// <summary>
    /// Match ended in a draw.
    /// </summary>
    Draw,

    /// <summary>
    /// Match timed out.
    /// </summary>
    Timeout,

    /// <summary>
    /// Match failed due to error.
    /// </summary>
    Error
}
