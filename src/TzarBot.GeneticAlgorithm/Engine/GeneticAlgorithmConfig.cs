namespace TzarBot.GeneticAlgorithm.Engine;

/// <summary>
/// Configuration for the genetic algorithm engine.
/// Contains all tunable parameters for population evolution.
/// </summary>
public sealed class GeneticAlgorithmConfig
{
    #region Population Settings

    /// <summary>
    /// Size of the population (number of genomes).
    /// Recommended range: 50-200.
    /// </summary>
    public int PopulationSize { get; set; } = 100;

    /// <summary>
    /// Minimum population size allowed.
    /// </summary>
    public const int MinPopulationSize = 10;

    /// <summary>
    /// Maximum population size allowed.
    /// </summary>
    public const int MaxPopulationSize = 1000;

    #endregion

    #region Selection Settings

    /// <summary>
    /// Number of participants in tournament selection.
    /// Higher values increase selection pressure.
    /// </summary>
    public int TournamentSize { get; set; } = 3;

    /// <summary>
    /// Percentage of top performers that pass directly to next generation (0.0-1.0).
    /// Default: 0.05 (top 5%)
    /// </summary>
    public float ElitismRate { get; set; } = 0.05f;

    #endregion

    #region Crossover Settings

    /// <summary>
    /// Probability of crossover occurring between two parents (0.0-1.0).
    /// Default: 0.7 (70%)
    /// </summary>
    public float CrossoverRate { get; set; } = 0.7f;

    /// <summary>
    /// Alpha parameter for arithmetic crossover (weight interpolation).
    /// 0.5 = equal contribution from both parents.
    /// </summary>
    public float ArithmeticCrossoverAlpha { get; set; } = 0.5f;

    /// <summary>
    /// If true, prefer structure from the parent with higher fitness.
    /// </summary>
    public bool InheritFromBetterParent { get; set; } = true;

    #endregion

    #region Mutation Settings

    /// <summary>
    /// Base probability of any mutation occurring to a genome (0.0-1.0).
    /// Individual mutation types have their own rates.
    /// </summary>
    public float MutationRate { get; set; } = 0.3f;

    /// <summary>
    /// Probability of weight mutation per genome.
    /// </summary>
    public float WeightMutationRate { get; set; } = 0.8f;

    /// <summary>
    /// Probability of perturbing each weight during weight mutation.
    /// </summary>
    public float WeightPerturbationRate { get; set; } = 0.1f;

    /// <summary>
    /// Standard deviation for Gaussian weight mutation.
    /// </summary>
    public float WeightMutationStrength { get; set; } = 0.5f;

    /// <summary>
    /// Probability of completely replacing a weight with a new random value.
    /// </summary>
    public float WeightResetRate { get; set; } = 0.01f;

    /// <summary>
    /// Minimum allowed weight value.
    /// </summary>
    public float MinWeight { get; set; } = -10f;

    /// <summary>
    /// Maximum allowed weight value.
    /// </summary>
    public float MaxWeight { get; set; } = 10f;

    /// <summary>
    /// Probability of structure mutation (add/remove layer).
    /// </summary>
    public float StructureMutationRate { get; set; } = 0.05f;

    /// <summary>
    /// Probability of adding a layer (vs removing one) when structure mutation occurs.
    /// </summary>
    public float AddLayerProbability { get; set; } = 0.5f;

    /// <summary>
    /// Probability of mutating neuron count in a layer.
    /// </summary>
    public float NeuronCountMutationRate { get; set; } = 0.1f;

    /// <summary>
    /// Maximum change in neuron count per mutation.
    /// </summary>
    public int MaxNeuronCountDelta { get; set; } = 32;

    /// <summary>
    /// Minimum number of hidden layers a genome can have.
    /// </summary>
    public int MinHiddenLayers { get; set; } = 1;

    /// <summary>
    /// Maximum number of hidden layers a genome can have.
    /// </summary>
    public int MaxHiddenLayers { get; set; } = 5;

    #endregion

    #region Evolution Settings

    /// <summary>
    /// Number of generations to run.
    /// Set to -1 for unlimited (run until stopped).
    /// </summary>
    public int MaxGenerations { get; set; } = 1000;

    /// <summary>
    /// Random seed for reproducibility.
    /// Set to -1 for random initialization.
    /// </summary>
    public int Seed { get; set; } = -1;

    /// <summary>
    /// Maximum degree of parallelism for evaluation.
    /// -1 = use all available processors.
    /// </summary>
    public int MaxParallelism { get; set; } = -1;

    /// <summary>
    /// Interval (in generations) for logging progress.
    /// </summary>
    public int LogInterval { get; set; } = 10;

    /// <summary>
    /// Interval (in generations) for saving checkpoints.
    /// 0 = disabled.
    /// </summary>
    public int CheckpointInterval { get; set; } = 50;

    #endregion

    #region Hidden Layer Initialization

    /// <summary>
    /// Default hidden layer sizes for new random genomes.
    /// </summary>
    public int[] DefaultHiddenLayerSizes { get; set; } = { 256, 128 };

    #endregion

    #region Validation

    /// <summary>
    /// Validates the configuration values are within acceptable ranges.
    /// </summary>
    public bool IsValid()
    {
        if (PopulationSize < MinPopulationSize || PopulationSize > MaxPopulationSize)
            return false;
        if (TournamentSize < 2 || TournamentSize > PopulationSize)
            return false;
        if (ElitismRate < 0f || ElitismRate > 1f)
            return false;
        if (CrossoverRate < 0f || CrossoverRate > 1f)
            return false;
        if (MutationRate < 0f || MutationRate > 1f)
            return false;
        if (WeightMutationStrength <= 0f)
            return false;
        if (MinWeight >= MaxWeight)
            return false;
        if (MinHiddenLayers < 1 || MaxHiddenLayers < MinHiddenLayers)
            return false;

        return true;
    }

    /// <summary>
    /// Creates a default configuration.
    /// </summary>
    public static GeneticAlgorithmConfig Default() => new();

    #endregion

    public override string ToString()
    {
        return $"GAConfig(pop={PopulationSize}, tour={TournamentSize}, elite={ElitismRate:P0}, " +
               $"cross={CrossoverRate:P0}, mut={MutationRate:P0})";
    }
}
