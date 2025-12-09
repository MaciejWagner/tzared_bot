using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TzarBot.GeneticAlgorithm.Engine;
using TzarBot.GeneticAlgorithm.Fitness;
using TzarBot.NeuralNetwork;
using TzarBot.NeuralNetwork.Models;
using TzarBot.NeuralNetwork.Onnx;
using TzarBot.Orchestrator.Communication;
using TzarBot.Orchestrator.Service;
using TzarBot.Training.Checkpoint;
using TzarBot.Training.Curriculum;

namespace TzarBot.Training.Core;

/// <summary>
/// Main training pipeline that coordinates all components.
///
/// Architecture:
/// - Uses IGeneticAlgorithm for population evolution
/// - Uses OrchestratorService for parallel genome evaluation on VMs
/// - Uses ICurriculumManager for staged difficulty progression
/// - Uses ICheckpointManager for state persistence
///
/// Training loop:
/// 1. Initialize population (random or from checkpoint)
/// 2. For each generation:
///    a. Evaluate all genomes (via orchestrator)
///    b. Calculate fitness (using curriculum-specific weights)
///    c. Check for stage transitions
///    d. Evolve population (selection, crossover, mutation)
///    e. Save checkpoint if needed
/// 3. Export best genome on completion
/// </summary>
public sealed class TrainingPipeline : ITrainingPipeline
{
    private readonly ILogger<TrainingPipeline> _logger;
    private readonly TrainingConfig _config;
    private readonly IGeneticAlgorithm _ga;
    private readonly OrchestratorService? _orchestrator;
    private readonly ICurriculumManager _curriculum;
    private readonly ICheckpointManager _checkpoint;
    private readonly IFitnessCalculator _fitnessCalculator;

    private TrainingState _state = new();
    private int _seed;
    private CancellationTokenSource? _pauseCts;
    private TaskCompletionSource? _pauseTcs;
    private bool _disposed;

    // Metrics tracking
    private int _generationGamesPlayed;
    private int _generationWins;
    private readonly Stopwatch _trainingStopwatch = new();

    /// <inheritdoc />
    public TrainingState State => _state;

    /// <inheritdoc />
    public TrainingConfig Config => _config;

    /// <inheritdoc />
    public bool IsRunning => _state.Status == TrainingStatus.Running;

    /// <inheritdoc />
    public event EventHandler<GenerationCompletedEventArgs>? GenerationCompleted;

    /// <inheritdoc />
    public event EventHandler<NewBestGenomeEventArgs>? NewBestGenomeFound;

    /// <inheritdoc />
    public event EventHandler<StageChangedEventArgs>? StageChanged;

    /// <inheritdoc />
    public event EventHandler<TrainingStatusChangedEventArgs>? StatusChanged;

    /// <inheritdoc />
    public event EventHandler<TrainingErrorEventArgs>? ErrorOccurred;

    /// <summary>
    /// Creates a training pipeline with all dependencies.
    /// </summary>
    public TrainingPipeline(
        ILogger<TrainingPipeline> logger,
        IOptions<TrainingConfig> config,
        IGeneticAlgorithm ga,
        ICurriculumManager curriculum,
        ICheckpointManager checkpoint,
        OrchestratorService? orchestrator = null)
    {
        _logger = logger;
        _config = config.Value;
        _ga = ga;
        _orchestrator = orchestrator;
        _curriculum = curriculum;
        _checkpoint = checkpoint;

        // Create fitness calculator - will be updated per stage
        _fitnessCalculator = new GameFitnessCalculator(curriculum.GetCurrentFitnessWeights());

        // Wire up curriculum events
        if (_curriculum is CurriculumManager cm)
        {
            cm.StageChanged += OnCurriculumStageChanged;
        }
    }

    /// <summary>
    /// Creates a training pipeline for testing (minimal dependencies).
    /// </summary>
    public TrainingPipeline(
        TrainingConfig config,
        IGeneticAlgorithm ga,
        ICurriculumManager curriculum,
        ICheckpointManager checkpoint,
        ILogger<TrainingPipeline>? logger = null)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<TrainingPipeline>.Instance;
        _config = config;
        _ga = ga;
        _orchestrator = null;
        _curriculum = curriculum;
        _checkpoint = checkpoint;
        _fitnessCalculator = new GameFitnessCalculator(curriculum.GetCurrentFitnessWeights());
    }

    /// <inheritdoc />
    public Task InitializeAsync(int seed = -1, CancellationToken cancellationToken = default)
    {
        _seed = seed == -1 ? Environment.TickCount : seed;

        SetStatus(TrainingStatus.Initializing, "Initializing population");

        _logger.LogInformation("Initializing training pipeline with seed {Seed}", _seed);

        // Initialize GA population
        _ga.InitializePopulation(_seed);

        // Initialize state
        _state = new TrainingState
        {
            StartTime = DateTime.UtcNow,
            Population = _ga.Population.ToList(),
            CurrentStageName = _config.InitialStage,
            Status = TrainingStatus.NotStarted
        };

        // Set curriculum stage
        _curriculum.SetStage(_config.InitialStage);

        _logger.LogInformation("Training initialized: {PopSize} genomes, stage={Stage}",
            _state.Population.Count, _state.CurrentStageName);

        SetStatus(TrainingStatus.NotStarted, "Initialization complete");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task InitializeFromCheckpointAsync(
        string checkpointPath,
        CancellationToken cancellationToken = default)
    {
        SetStatus(TrainingStatus.Initializing, "Loading checkpoint");

        _logger.LogInformation("Loading checkpoint: {Path}", checkpointPath);

        var checkpoint = await _checkpoint.LoadAsync(checkpointPath, cancellationToken);

        _seed = checkpoint.Seed;
        _state = checkpoint.State;

        // Load population into GA
        _ga.LoadPopulation(_state.Population);

        // Restore curriculum stage
        _curriculum.SetStage(_state.CurrentStageName);

        _logger.LogInformation(
            "Checkpoint loaded: Gen={Gen}, Stage={Stage}, Pop={Pop}, BestFit={Fit:F2}",
            _state.CurrentGeneration, _state.CurrentStageName,
            _state.Population.Count, _state.BestFitness);

        SetStatus(TrainingStatus.NotStarted, "Checkpoint loaded");
    }

    /// <inheritdoc />
    public async Task<TrainingState> RunAsync(CancellationToken cancellationToken = default)
    {
        SetStatus(TrainingStatus.Running, "Training started");
        _trainingStopwatch.Start();

        try
        {
            int maxGen = _config.MaxGenerations;

            while (!cancellationToken.IsCancellationRequested)
            {
                // Check max generations
                if (maxGen > 0 && _state.CurrentGeneration >= maxGen)
                {
                    _logger.LogInformation("Max generations ({Max}) reached", maxGen);
                    break;
                }

                // Check max training time
                if (_trainingStopwatch.Elapsed >= _config.MaxTrainingTime)
                {
                    _logger.LogInformation("Max training time ({Time}) reached", _config.MaxTrainingTime);
                    break;
                }

                // Check for pause
                await CheckPauseAsync(cancellationToken);

                // Run one generation
                await RunGenerationAsync(cancellationToken);

                // Update elapsed time
                _state.ElapsedTime = _trainingStopwatch.Elapsed;
                _state.LastUpdateTime = DateTime.UtcNow;
            }

            SetStatus(
                cancellationToken.IsCancellationRequested ? TrainingStatus.Cancelled : TrainingStatus.Completed,
                "Training finished");
        }
        catch (OperationCanceledException)
        {
            SetStatus(TrainingStatus.Cancelled, "Training cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Training failed");
            SetStatus(TrainingStatus.Error, ex.Message);
            RaiseError(ex, "RunAsync", true);
        }
        finally
        {
            _trainingStopwatch.Stop();
        }

        return _state;
    }

    /// <inheritdoc />
    public async Task<GenerationStats> RunGenerationAsync(CancellationToken cancellationToken = default)
    {
        var genStopwatch = Stopwatch.StartNew();
        _generationGamesPlayed = 0;
        _generationWins = 0;

        var generation = _state.CurrentGeneration + 1;
        _logger.LogInformation("Generation {Gen} starting...", generation);

        // Create evaluator that uses orchestrator or mock
        FitnessEvaluator evaluator = CreateEvaluator(cancellationToken);

        // Run GA generation
        var gaStats = await _ga.RunGenerationAsync(evaluator, cancellationToken);

        genStopwatch.Stop();

        // Convert to training stats
        var stats = GenerationStats.FromGAStats(
            gaStats,
            _state.CurrentStageName,
            _generationGamesPlayed,
            _generationWins);

        // Update state
        _state.RecordGeneration(stats);
        _state.Population = _ga.Population.ToList();
        _state.TotalGamesPlayed += _generationGamesPlayed;
        _state.TotalWins += _generationWins;

        // Check for new best
        if (_ga.BestGenome != null && _state.UpdateBest(_ga.BestGenome))
        {
            _logger.LogInformation("New best genome found! Fitness={Fit:F2}", _state.BestFitness);

            RaiseNewBestFound(_ga.BestGenome, _state.BestFitness, generation,
                gaStats.Improvement);

            // Save best genome
            if (_config.SaveBestGenome)
            {
                await _checkpoint.SaveBestGenomeAsync(
                    _ga.BestGenome, generation, _state.CurrentStageName,
                    cancellationToken: cancellationToken);
            }
        }

        // Update curriculum metrics and check for stage transition
        _curriculum.UpdateMetrics(stats);
        CheckStageTransition();

        // Checkpoint if needed
        if (_config.CheckpointInterval > 0 && generation % _config.CheckpointInterval == 0)
        {
            await SaveCheckpointAsync(cancellationToken);
        }

        // Raise completion event
        RaiseGenerationCompleted(stats);

        _logger.LogInformation(
            "Generation {Gen} complete: BestFit={Best:F2}, AvgFit={Avg:F2}, WinRate={WR:P0}, Time={Time:F1}s",
            generation, stats.BestFitness, stats.AverageFitness, stats.WinRate,
            genStopwatch.Elapsed.TotalSeconds);

        return stats;
    }

    /// <inheritdoc />
    public Task PauseAsync()
    {
        if (_state.Status != TrainingStatus.Running)
            return Task.CompletedTask;

        _pauseCts = new CancellationTokenSource();
        _pauseTcs = new TaskCompletionSource();

        SetStatus(TrainingStatus.Paused, "Training paused by user");
        _logger.LogInformation("Training paused at generation {Gen}", _state.CurrentGeneration);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ResumeAsync()
    {
        if (_state.Status != TrainingStatus.Paused)
            return Task.CompletedTask;

        _pauseTcs?.TrySetResult();
        _pauseCts?.Dispose();
        _pauseCts = null;
        _pauseTcs = null;

        SetStatus(TrainingStatus.Running, "Training resumed");
        _logger.LogInformation("Training resumed at generation {Gen}", _state.CurrentGeneration);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping training...");

        // Save final checkpoint
        await SaveCheckpointAsync(cancellationToken);

        // Export best genome
        if (_state.BestGenome != null)
        {
            var path = Path.Combine(_config.BestGenomeDirectory, "final_best.onnx");
            await ExportBestGenomeAsync(path, cancellationToken);
        }

        SetStatus(TrainingStatus.Completed, "Training stopped");
    }

    /// <inheritdoc />
    public async Task<string> SaveCheckpointAsync(CancellationToken cancellationToken = default)
    {
        return await _checkpoint.SaveCheckpointAsync(
            _state, _config, _seed,
            new Dictionary<string, string>
            {
                ["stage"] = _state.CurrentStageName,
                ["elapsed"] = _state.ElapsedTime.ToString()
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task ExportBestGenomeAsync(string outputPath, CancellationToken cancellationToken = default)
    {
        if (_state.BestGenome == null)
            throw new InvalidOperationException("No best genome to export");

        _logger.LogInformation("Exporting best genome to {Path}", outputPath);

        // Ensure directory exists
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // Export genome to ONNX
        var exporter = new OnnxModelExporter();
        await exporter.ExportAsync(_state.BestGenome, outputPath, cancellationToken);

        _logger.LogInformation("Best genome exported: {Path}", outputPath);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;

        _pauseCts?.Dispose();

        // Unsubscribe from events
        if (_curriculum is CurriculumManager cm)
        {
            cm.StageChanged -= OnCurriculumStageChanged;
        }

        await Task.CompletedTask;
    }

    #region Private Methods

    private FitnessEvaluator CreateEvaluator(CancellationToken cancellationToken)
    {
        if (_orchestrator != null)
        {
            // Real evaluation via orchestrator
            return async (genome, ct) =>
            {
                var results = new List<GameResult>();

                for (int i = 0; i < _config.GamesPerGenome; i++)
                {
                    var genomeData = GenomeSerializer.Serialize(genome);
                    var evalResult = await _orchestrator.EvaluateGenomeAsync(
                        genome.Id.ToString(),
                        genomeData,
                        _config.GameTimeout,
                        ct);

                    _generationGamesPlayed++;

                    if (evalResult.Success)
                    {
                        var gameResult = ConvertToGameResult(evalResult);
                        results.Add(gameResult);

                        if (gameResult.Won)
                            _generationWins++;
                    }
                }

                // Update genome stats
                genome.GamesPlayed = results.Count;
                genome.Wins = results.Count(r => r.Won);

                return results.Count > 0
                    ? _fitnessCalculator.CalculateAverage(results)
                    : 0f;
            };
        }
        else
        {
            // Mock evaluation for testing
            var random = new Random(_seed + _state.CurrentGeneration);

            return (genome, ct) =>
            {
                _generationGamesPlayed += _config.GamesPerGenome;

                // Mock fitness based on network complexity and randomness
                float baseFitness = genome.HiddenLayers.Sum(l => l.NeuronCount) / 1000f;
                float randomFactor = (float)random.NextDouble() * 50f;

                // Simulate some wins based on fitness
                float winProb = Math.Min(0.9f, baseFitness + 0.1f);
                int wins = (int)(_config.GamesPerGenome * winProb);
                _generationWins += wins;

                genome.GamesPlayed = _config.GamesPerGenome;
                genome.Wins = wins;

                return Task.FromResult(baseFitness + randomFactor);
            };
        }
    }

    private static GameResult ConvertToGameResult(EvaluationResult evalResult)
    {
        var metrics = evalResult.Metrics;
        return new GameResult
        {
            Won = evalResult.Outcome == GameOutcome.Win,
            DurationSeconds = metrics?.GameDurationSeconds ?? (float)evalResult.EvaluationDuration.TotalSeconds,
            UnitsBuilt = metrics?.UnitsCreated ?? 0,
            UnitsKilled = metrics?.EnemyUnitsDestroyed ?? 0,
            BuildingsBuilt = metrics?.BuildingsConstructed ?? 0,
            ResourcesGathered = metrics?.ResourcesGathered ?? 0,
            ValidActions = (int)(metrics?.ActionsPerMinute ?? 0 * (metrics?.GameDurationSeconds ?? 0) / 60)
        };
    }

    private void CheckStageTransition()
    {
        var (transitionType, reason) = _curriculum.EvaluateTransition();

        switch (transitionType)
        {
            case StageTransitionType.Promotion:
                if (_curriculum.TryPromote(reason))
                {
                    _state.RecordStageTransition(
                        _state.CurrentStageName,
                        _curriculum.CurrentStage.Name,
                        reason);
                }
                break;

            case StageTransitionType.Demotion:
                if (_config.AllowDemotion && _curriculum.TryDemote(reason))
                {
                    _state.RecordStageTransition(
                        _state.CurrentStageName,
                        _curriculum.CurrentStage.Name,
                        reason);
                }
                break;
        }
    }

    private async Task CheckPauseAsync(CancellationToken cancellationToken)
    {
        if (_pauseTcs != null)
        {
            await Task.WhenAny(
                _pauseTcs.Task,
                Task.Delay(Timeout.Infinite, cancellationToken));
        }
    }

    private void SetStatus(TrainingStatus newStatus, string? reason = null)
    {
        var oldStatus = _state.Status;
        _state.Status = newStatus;

        StatusChanged?.Invoke(this, new TrainingStatusChangedEventArgs
        {
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Reason = reason
        });
    }

    private void RaiseGenerationCompleted(GenerationStats stats)
    {
        GenerationCompleted?.Invoke(this, new GenerationCompletedEventArgs
        {
            Stats = stats,
            Summary = _state.GetSummary()
        });
    }

    private void RaiseNewBestFound(NetworkGenome genome, float fitness, int generation, float improvement)
    {
        NewBestGenomeFound?.Invoke(this, new NewBestGenomeEventArgs
        {
            Genome = genome,
            Fitness = fitness,
            Generation = generation,
            Improvement = improvement
        });
    }

    private void RaiseError(Exception ex, string context, bool isFatal)
    {
        ErrorOccurred?.Invoke(this, new TrainingErrorEventArgs
        {
            Exception = ex,
            Context = context,
            IsFatal = isFatal
        });
    }

    private void OnCurriculumStageChanged(object? sender, StageTransitionRecord e)
    {
        StageChanged?.Invoke(this, new StageChangedEventArgs
        {
            FromStage = e.FromStage,
            ToStage = e.ToStage,
            Generation = e.Generation,
            Reason = e.Reason,
            IsPromotion = e.Type == StageTransitionType.Promotion
        });
    }

    #endregion
}
