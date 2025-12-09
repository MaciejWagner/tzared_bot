namespace TzarBot.Training.Curriculum;

/// <summary>
/// Predefined curriculum stage definitions for TzarBot training.
///
/// Training progression:
/// 1. Bootstrap: Learn basic interaction with passive environment
/// 2. Basic: Learn economy against easy AI
/// 3. CombatEasy: Learn combat against easy AI
/// 4. CombatNormal: Improve combat against normal AI
/// 5. CombatHard: Master combat against hard AI
/// 6. Tournament: Self-play tournament for final optimization
/// </summary>
public static class StageDefinitions
{
    /// <summary>
    /// Bootstrap stage - Learning basic game interaction.
    /// Passive AI, focus on survival and activity.
    /// </summary>
    public static CurriculumStage Bootstrap => new()
    {
        Name = "Bootstrap",
        Description = "Learn basic interaction with the game environment",
        Order = 0,
        Opponent = OpponentType.PassiveAI,
        Difficulty = AIDifficulty.Passive,
        FitnessMode = FitnessMode.Survival,
        MaxGameDuration = TimeSpan.FromMinutes(10),
        MinGenerationsInStage = 10,
        PromotionCriteria = new PromotionCriteria
        {
            MinWinRate = 0f, // No wins expected against passive
            MinAverageFitness = 20f,
            MinBestFitness = 50f,
            ConsecutiveGenerations = 3
        },
        // Custom weights: reward activity, minimal win focus
        FitnessWeights = new FitnessWeightsOverride
        {
            WinBonus = 10f,
            ActivityWeight = 3f,
            UnitWeight = 2f,
            ResourceWeight = 2f,
            InactivityPenalty = 100f,
            InvalidActionPenalty = 50f
        }
    };

    /// <summary>
    /// Basic stage - Learning economy and resource management.
    /// Easy AI, focus on building and gathering.
    /// </summary>
    public static CurriculumStage Basic => new()
    {
        Name = "Basic",
        Description = "Learn economy and resource management",
        Order = 1,
        Opponent = OpponentType.GameAI,
        Difficulty = AIDifficulty.Easy,
        FitnessMode = FitnessMode.Economy,
        MaxGameDuration = TimeSpan.FromMinutes(15),
        MinGenerationsInStage = 10,
        PromotionCriteria = new PromotionCriteria
        {
            MinWinRate = 0.2f,
            MinAverageFitness = 40f,
            MinBestFitness = 80f,
            ConsecutiveGenerations = 3
        },
        DemotionCriteria = new DemotionCriteria
        {
            MaxWinRate = 0f,
            MaxAverageFitness = 5f,
            ConsecutiveGenerations = 10
        },
        FitnessWeights = new FitnessWeightsOverride
        {
            WinBonus = 50f,
            UnitWeight = 2f,
            BuildingWeight = 3f,
            ResourceWeight = 2f,
            ActivityWeight = 1f
        }
    };

    /// <summary>
    /// CombatEasy stage - Learning basic combat.
    /// Easy AI, focus on combat effectiveness.
    /// </summary>
    public static CurriculumStage CombatEasy => new()
    {
        Name = "CombatEasy",
        Description = "Learn basic combat against easy opponents",
        Order = 2,
        Opponent = OpponentType.GameAI,
        Difficulty = AIDifficulty.Easy,
        FitnessMode = FitnessMode.Combat,
        MaxGameDuration = TimeSpan.FromMinutes(20),
        MinGenerationsInStage = 10,
        PromotionCriteria = new PromotionCriteria
        {
            MinWinRate = 0.6f,
            MinAverageFitness = 60f,
            MinBestFitness = 120f,
            ConsecutiveGenerations = 5
        },
        DemotionCriteria = new DemotionCriteria
        {
            MaxWinRate = 0.1f,
            MaxAverageFitness = 20f,
            ConsecutiveGenerations = 10
        },
        FitnessWeights = new FitnessWeightsOverride
        {
            WinBonus = 100f,
            TimeBonus = 30f,
            CombatWeight = 2f,
            UnitWeight = 1f,
            BuildingWeight = 1f
        }
    };

    /// <summary>
    /// CombatNormal stage - Improving combat skills.
    /// Normal AI, focus on winning.
    /// </summary>
    public static CurriculumStage CombatNormal => new()
    {
        Name = "CombatNormal",
        Description = "Improve combat against normal opponents",
        Order = 3,
        Opponent = OpponentType.GameAI,
        Difficulty = AIDifficulty.Normal,
        FitnessMode = FitnessMode.Victory,
        MaxGameDuration = TimeSpan.FromMinutes(25),
        MinGenerationsInStage = 15,
        PromotionCriteria = new PromotionCriteria
        {
            MinWinRate = 0.5f,
            MinAverageFitness = 80f,
            MinBestFitness = 150f,
            ConsecutiveGenerations = 5
        },
        DemotionCriteria = new DemotionCriteria
        {
            MaxWinRate = 0.15f,
            MaxAverageFitness = 30f,
            ConsecutiveGenerations = 10
        },
        FitnessWeights = new FitnessWeightsOverride
        {
            WinBonus = 150f,
            TimeBonus = 50f,
            CombatWeight = 1.5f
        }
    };

    /// <summary>
    /// CombatHard stage - Mastering combat.
    /// Hard AI, focus on efficient wins.
    /// </summary>
    public static CurriculumStage CombatHard => new()
    {
        Name = "CombatHard",
        Description = "Master combat against hard opponents",
        Order = 4,
        Opponent = OpponentType.GameAI,
        Difficulty = AIDifficulty.Hard,
        FitnessMode = FitnessMode.Efficiency,
        MaxGameDuration = TimeSpan.FromMinutes(30),
        MinGenerationsInStage = 20,
        PromotionCriteria = new PromotionCriteria
        {
            MinWinRate = 0.4f,
            MinAverageFitness = 100f,
            MinBestFitness = 200f,
            ConsecutiveGenerations = 10
        },
        DemotionCriteria = new DemotionCriteria
        {
            MaxWinRate = 0.1f,
            MaxAverageFitness = 40f,
            ConsecutiveGenerations = 15
        },
        FitnessWeights = new FitnessWeightsOverride
        {
            WinBonus = 200f,
            TimeBonus = 100f,
            CombatWeight = 1f,
            LossPenalty = 50f
        }
    };

    /// <summary>
    /// Tournament stage - Self-play optimization.
    /// Swiss-system tournament, ELO-based fitness.
    /// </summary>
    public static CurriculumStage Tournament => new()
    {
        Name = "Tournament",
        Description = "Final optimization through self-play tournaments",
        Order = 5,
        Opponent = OpponentType.Tournament,
        Difficulty = AIDifficulty.Hard, // Backup difficulty for reference games
        FitnessMode = FitnessMode.Efficiency,
        MaxGameDuration = TimeSpan.FromMinutes(30),
        MinGenerationsInStage = 0, // No minimum - final stage
        IsFinalStage = true,
        PromotionCriteria = new PromotionCriteria
        {
            MinWinRate = 1f, // Impossible - this is the final stage
            MinAverageFitness = float.MaxValue,
            MinBestFitness = float.MaxValue,
            ConsecutiveGenerations = int.MaxValue
        },
        DemotionCriteria = new DemotionCriteria
        {
            MaxWinRate = 0.05f,
            MaxAverageFitness = 20f,
            ConsecutiveGenerations = 20
        },
        FitnessWeights = new FitnessWeightsOverride
        {
            WinBonus = 200f,
            TimeBonus = 100f
        }
    };

    /// <summary>
    /// Gets all predefined stages in order.
    /// </summary>
    public static IReadOnlyList<CurriculumStage> GetAllStages()
    {
        return new List<CurriculumStage>
        {
            Bootstrap,
            Basic,
            CombatEasy,
            CombatNormal,
            CombatHard,
            Tournament
        };
    }

    /// <summary>
    /// Gets a stage by name.
    /// </summary>
    public static CurriculumStage? GetStage(string name)
    {
        return GetAllStages().FirstOrDefault(s =>
            s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the next stage after the given stage.
    /// </summary>
    public static CurriculumStage? GetNextStage(string currentStageName)
    {
        var stages = GetAllStages();
        var current = stages.FirstOrDefault(s =>
            s.Name.Equals(currentStageName, StringComparison.OrdinalIgnoreCase));

        if (current == null || current.IsFinalStage)
            return null;

        return stages.FirstOrDefault(s => s.Order == current.Order + 1);
    }

    /// <summary>
    /// Gets the previous stage before the given stage.
    /// </summary>
    public static CurriculumStage? GetPreviousStage(string currentStageName)
    {
        var stages = GetAllStages();
        var current = stages.FirstOrDefault(s =>
            s.Name.Equals(currentStageName, StringComparison.OrdinalIgnoreCase));

        if (current == null || current.Order == 0)
            return null;

        return stages.FirstOrDefault(s => s.Order == current.Order - 1);
    }

    /// <summary>
    /// Creates a simplified curriculum with fewer stages for testing.
    /// </summary>
    public static IReadOnlyList<CurriculumStage> GetSimplifiedStages()
    {
        return new List<CurriculumStage>
        {
            new CurriculumStage
            {
                Name = "Bootstrap",
                Description = "Learn basic interaction (simplified)",
                Order = 0,
                Opponent = OpponentType.PassiveAI,
                Difficulty = AIDifficulty.Passive,
                FitnessMode = FitnessMode.Survival,
                MaxGameDuration = TimeSpan.FromMinutes(10),
                MinGenerationsInStage = 5,
                PromotionCriteria = new PromotionCriteria
                {
                    MinWinRate = 0f,
                    MinAverageFitness = 10f,
                    MinBestFitness = 25f,
                    ConsecutiveGenerations = 2
                }
            },
            new CurriculumStage
            {
                Name = "CombatEasy",
                Description = "Combat against easy AI (simplified)",
                Order = 1,
                Opponent = OpponentType.GameAI,
                Difficulty = AIDifficulty.Easy,
                FitnessMode = FitnessMode.Combat,
                MaxGameDuration = TimeSpan.FromMinutes(20),
                MinGenerationsInStage = 5,
                PromotionCriteria = new PromotionCriteria
                {
                    MinWinRate = 0.3f,
                    MinAverageFitness = 30f,
                    MinBestFitness = 60f,
                    ConsecutiveGenerations = 3
                }
            },
            new CurriculumStage
            {
                Name = "Tournament",
                Description = "Self-play tournament (simplified)",
                Order = 2,
                Opponent = OpponentType.Tournament,
                Difficulty = AIDifficulty.Hard,
                FitnessMode = FitnessMode.Efficiency,
                MaxGameDuration = TimeSpan.FromMinutes(30),
                MinGenerationsInStage = 0,
                IsFinalStage = true,
                PromotionCriteria = new PromotionCriteria
                {
                    MinWinRate = 1f,
                    MinAverageFitness = float.MaxValue,
                    MinBestFitness = float.MaxValue,
                    ConsecutiveGenerations = int.MaxValue
                }
            }
        };
    }
}
