using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TzarBot.GeneticAlgorithm.Operators;
using TzarBot.GeneticAlgorithm.Persistence;
using TzarBot.GeneticAlgorithm.Selection;
using TzarBot.NeuralNetwork.Models;

namespace TzarBot.GeneticAlgorithm.Engine;

/// <summary>
/// Main genetic algorithm engine for evolving neural network populations.
///
/// Evolution loop:
/// 1. Evaluate fitness of all genomes (parallel)
/// 2. Select elites (top performers pass directly to next generation)
/// 3. Select parents using tournament selection
/// 4. Create offspring through crossover
/// 5. Apply mutations to offspring
/// 6. Form new generation from elites + offspring
/// 7. Repeat
/// </summary>
public sealed class GeneticAlgorithmEngine : IGeneticAlgorithm
{
    private readonly GeneticAlgorithmConfig _config;
    private readonly ILogger<GeneticAlgorithmEngine> _logger;
    private readonly Random _random;

    // Operators
    private readonly TournamentSelection _selection;
    private readonly ElitismStrategy _elitism;
    private readonly UniformCrossover _crossover;
    private readonly WeightMutator _weightMutator;
    private readonly StructureMutator _structureMutator;

    // Optional persistence
    private readonly PopulationCheckpoint? _checkpoint;

    // State
    private List<NetworkGenome> _population = new();
    private NetworkGenome? _bestGenome;
    private GenerationStats _currentStats = new();

    /// <inheritdoc />
    public int Generation { get; private set; }

    /// <inheritdoc />
    public IReadOnlyList<NetworkGenome> Population => _population;

    /// <inheritdoc />
    public NetworkGenome? BestGenome => _bestGenome;

    /// <inheritdoc />
    public GenerationStats CurrentStats => _currentStats;

    /// <inheritdoc />
    public GeneticAlgorithmConfig Config => _config;

    /// <inheritdoc />
    public event EventHandler<GenerationStats>? GenerationCompleted;

    /// <inheritdoc />
    public event EventHandler<NetworkGenome>? NewBestGenomeFound;

    /// <summary>
    /// Creates a new GA engine with specified configuration.
    /// </summary>
    /// <param name="config">GA configuration.</param>
    /// <param name="checkpointDirectory">Optional checkpoint directory.</param>
    /// <param name="logger">Optional logger.</param>
    public GeneticAlgorithmEngine(
        GeneticAlgorithmConfig? config = null,
        string? checkpointDirectory = null,
        ILogger<GeneticAlgorithmEngine>? logger = null)
    {
        _config = config ?? GeneticAlgorithmConfig.Default();

        if (!_config.IsValid())
            throw new ArgumentException("Invalid GA configuration", nameof(config));

        _logger = logger ?? NullLogger<GeneticAlgorithmEngine>.Instance;

        // Initialize random
        _random = _config.Seed >= 0
            ? new Random(_config.Seed)
            : new Random();

        // Initialize operators
        _selection = new TournamentSelection(_config);
        _elitism = new ElitismStrategy(_config);
        _crossover = new UniformCrossover(_config);
        _weightMutator = new WeightMutator(_config);
        _structureMutator = new StructureMutator(_config);

        // Initialize checkpoint manager if directory provided
        if (!string.IsNullOrEmpty(checkpointDirectory))
        {
            _checkpoint = new PopulationCheckpoint(checkpointDirectory);
        }

        _logger.LogInformation("GA Engine initialized: {Config}", _config);
    }

    /// <inheritdoc />
    public void InitializePopulation(int? seed = null)
    {
        var random = seed.HasValue ? new Random(seed.Value) : _random;

        _population = new List<NetworkGenome>(_config.PopulationSize);

        for (int i = 0; i < _config.PopulationSize; i++)
        {
            var genome = NetworkGenome.CreateRandom(
                _config.DefaultHiddenLayerSizes,
                random.Next());

            genome.Generation = 0;
            _population.Add(genome);
        }

        Generation = 0;
        _bestGenome = null;

        _logger.LogInformation(
            "Initialized population: {Size} genomes, {Layers} hidden layers, {Weights} weights each",
            _population.Count,
            _config.DefaultHiddenLayerSizes.Length,
            _population[0].Weights.Length);
    }

    /// <inheritdoc />
    public void LoadPopulation(IEnumerable<NetworkGenome> population)
    {
        _population = population.ToList();

        if (_population.Count == 0)
            throw new ArgumentException("Population cannot be empty", nameof(population));

        // Find best genome and latest generation
        _bestGenome = _population.MaxBy(g => g.Fitness);
        Generation = _population.Max(g => g.Generation);

        _logger.LogInformation(
            "Loaded population: {Size} genomes, generation {Generation}, best fitness {Fitness:F3}",
            _population.Count,
            Generation,
            _bestGenome?.Fitness ?? 0);
    }

    /// <inheritdoc />
    public async Task<GenerationStats> RunGenerationAsync(
        FitnessEvaluator evaluator,
        CancellationToken cancellationToken = default)
    {
        if (_population.Count == 0)
            throw new InvalidOperationException("Population not initialized. Call InitializePopulation first.");

        var stopwatch = Stopwatch.StartNew();
        float previousBestFitness = _bestGenome?.Fitness ?? float.NegativeInfinity;

        // 1. Evaluate fitness
        var evalStopwatch = Stopwatch.StartNew();
        await EvaluateFitnessAsync(evaluator, cancellationToken);
        var evalTime = evalStopwatch.Elapsed;

        // 2. Evolution
        var evoStopwatch = Stopwatch.StartNew();

        // Select elites
        var elites = _elitism.SelectElites(_population);

        // Create offspring
        int offspringCount = _config.PopulationSize - elites.Count;
        var offspring = CreateOffspring(offspringCount);

        // Form new generation
        _population = _elitism.CombineWithOffspring(elites, offspring, _config.PopulationSize);

        // Increment generation for new offspring
        foreach (var genome in _population)
        {
            if (genome.Generation <= Generation)
            {
                genome.Generation = Generation + 1;
            }
        }

        Generation++;
        var evoTime = evoStopwatch.Elapsed;

        // 3. Update best genome
        var currentBest = _population.MaxBy(g => g.Fitness);
        if (currentBest != null && (
            _bestGenome == null || currentBest.Fitness > _bestGenome.Fitness))
        {
            _bestGenome = currentBest.Clone();
            NewBestGenomeFound?.Invoke(this, _bestGenome);

            _logger.LogInformation(
                "New best genome found: Gen {Generation}, Fitness {Fitness:F3}",
                Generation, _bestGenome.Fitness);
        }

        // 4. Calculate statistics
        var fitnessValues = _population.Select(g => g.Fitness).ToArray();
        float avgFitness = fitnessValues.Average();
        float stdDev = CalculateStdDev(fitnessValues, avgFitness);

        _currentStats = new GenerationStats
        {
            Generation = Generation,
            BestFitness = currentBest?.Fitness ?? 0,
            AverageFitness = avgFitness,
            WorstFitness = fitnessValues.Min(),
            FitnessStdDev = stdDev,
            EliteCount = elites.Count,
            CrossoverCount = offspringCount / 2,
            MutationCount = offspringCount,
            EvaluationTime = evalTime,
            EvolutionTime = evoTime,
            BestGenomeId = currentBest?.Id ?? Guid.Empty,
            Improvement = (currentBest?.Fitness ?? 0) - previousBestFitness,
            Timestamp = DateTime.UtcNow
        };

        // 5. Checkpoint if configured
        if (_checkpoint != null && _config.CheckpointInterval > 0 &&
            Generation % _config.CheckpointInterval == 0)
        {
            await SaveCheckpointAsync(cancellationToken);
        }

        // 6. Log progress
        if (_config.LogInterval > 0 && Generation % _config.LogInterval == 0)
        {
            _logger.LogInformation(
                "Gen {Generation}: Best={Best:F3}, Avg={Avg:F3}, StdDev={StdDev:F3}, Time={Time}ms",
                Generation,
                _currentStats.BestFitness,
                _currentStats.AverageFitness,
                _currentStats.FitnessStdDev,
                stopwatch.ElapsedMilliseconds);
        }

        GenerationCompleted?.Invoke(this, _currentStats);

        return _currentStats;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<GenerationStats> RunAsync(
        FitnessEvaluator evaluator,
        int maxGenerations = -1,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_population.Count == 0)
        {
            InitializePopulation();
        }

        int targetGen = maxGenerations < 0
            ? int.MaxValue
            : Generation + maxGenerations;

        while (Generation < targetGen && !cancellationToken.IsCancellationRequested)
        {
            var stats = await RunGenerationAsync(evaluator, cancellationToken);
            yield return stats;
        }
    }

    /// <summary>
    /// Evaluates fitness for all genomes in the population.
    /// </summary>
    private async Task EvaluateFitnessAsync(
        FitnessEvaluator evaluator,
        CancellationToken cancellationToken)
    {
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = _config.MaxParallelism < 0
                ? Environment.ProcessorCount
                : _config.MaxParallelism,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(_population, options, async (genome, ct) =>
        {
            genome.Fitness = await evaluator(genome, ct);
        });
    }

    /// <summary>
    /// Creates offspring through selection, crossover, and mutation.
    /// </summary>
    private List<NetworkGenome> CreateOffspring(int count)
    {
        var offspring = new List<NetworkGenome>(count);

        // Select pairs and create offspring
        int pairCount = (count + 1) / 2;
        var pairs = _selection.SelectPairs(_population, pairCount, _random);

        foreach (var (parent1, parent2) in pairs)
        {
            if (offspring.Count >= count)
                break;

            // Decide if crossover occurs
            if (_random.NextDouble() < _config.CrossoverRate)
            {
                var child = _crossover.CrossoverSingle(parent1, parent2, _random);
                ApplyMutations(child);
                offspring.Add(child);

                if (offspring.Count < count)
                {
                    var child2 = _crossover.CrossoverSingle(parent2, parent1, _random);
                    ApplyMutations(child2);
                    offspring.Add(child2);
                }
            }
            else
            {
                // No crossover - just clone and mutate
                var child1 = parent1.Clone();
                child1.ParentIds = new[] { parent1.Id };
                ApplyMutations(child1);
                offspring.Add(child1);

                if (offspring.Count < count)
                {
                    var child2 = parent2.Clone();
                    child2.ParentIds = new[] { parent2.Id };
                    ApplyMutations(child2);
                    offspring.Add(child2);
                }
            }
        }

        return offspring;
    }

    /// <summary>
    /// Applies mutations to a genome.
    /// </summary>
    private void ApplyMutations(NetworkGenome genome)
    {
        if (_random.NextDouble() > _config.MutationRate)
            return;

        // Weight mutation (most common)
        _weightMutator.Mutate(genome, _random);

        // Structure mutation (rare)
        _structureMutator.Mutate(genome, _random);

        // Ensure weights are valid
        _weightMutator.ClampWeights(genome.Weights);
    }

    /// <summary>
    /// Saves a checkpoint of the current population.
    /// </summary>
    public async Task SaveCheckpointAsync(CancellationToken cancellationToken = default)
    {
        if (_checkpoint == null)
        {
            _logger.LogWarning("Checkpoint save requested but no checkpoint directory configured");
            return;
        }

        var path = await _checkpoint.SaveAsync(_population, Generation, _currentStats, cancellationToken);
        _logger.LogInformation("Saved checkpoint: {Path}", path);
    }

    /// <summary>
    /// Loads the latest checkpoint.
    /// </summary>
    public async Task<bool> LoadLatestCheckpointAsync(CancellationToken cancellationToken = default)
    {
        if (_checkpoint == null)
        {
            _logger.LogWarning("Checkpoint load requested but no checkpoint directory configured");
            return false;
        }

        var data = await _checkpoint.LoadLatestAsync(cancellationToken);
        if (data == null)
        {
            _logger.LogInformation("No checkpoint found");
            return false;
        }

        LoadPopulation(data.Genomes);
        Generation = data.Generation;

        _logger.LogInformation(
            "Loaded checkpoint: Generation {Generation}, {Count} genomes",
            Generation, _population.Count);

        return true;
    }

    /// <summary>
    /// Gets the current population diversity (unique structures).
    /// </summary>
    public float GetDiversity()
    {
        if (_population.Count == 0)
            return 0f;

        var structures = _population
            .Select(g => string.Join(",", g.HiddenLayers.Select(l => l.NeuronCount)))
            .Distinct()
            .Count();

        return (float)structures / _population.Count;
    }

    /// <summary>
    /// Calculates standard deviation.
    /// </summary>
    private static float CalculateStdDev(float[] values, float mean)
    {
        if (values.Length <= 1)
            return 0f;

        double sumSquares = values.Sum(v => Math.Pow(v - mean, 2));
        return (float)Math.Sqrt(sumSquares / (values.Length - 1));
    }
}
