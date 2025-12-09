using System.Collections.Concurrent;
using TzarBot.Dashboard.Models;

namespace TzarBot.Dashboard.Services;

/// <summary>
/// Mock implementation of ITrainingStateService for development and testing.
/// Generates fake training data to test the dashboard without actual training.
/// </summary>
public sealed class MockTrainingService : ITrainingStateService, IHostedService, IDisposable
{
    private readonly ILogger<MockTrainingService> _logger;
    private readonly IConfiguration _configuration;
    private readonly Random _random = new(42);
    private readonly ConcurrentQueue<DashboardGenerationStats> _generationHistory = new();
    private readonly ConcurrentQueue<ActivityLogEntry> _activityLog = new();
    private readonly List<GenomeSummary> _population = new();
    private readonly object _lock = new();

    private Timer? _generationTimer;
    private CancellationTokenSource? _cts;

    private int _currentGeneration;
    private string _currentStage = "Stage 1: Basic Training";
    private float _bestFitness;
    private float _averageFitness;
    private int _totalGamesPlayed;
    private int _totalWins;

    private readonly string[] _stages = new[]
    {
        "Stage 1: Basic Training",
        "Stage 2: Resource Management",
        "Stage 3: Combat Basics",
        "Stage 4: Advanced Strategy",
        "Stage 5: Full Game"
    };

    public MockTrainingService(ILogger<MockTrainingService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        // Initialize population
        InitializePopulation(50);
    }

    public TrainingStatus Status { get; private set; } = TrainingStatus.Stopped;
    public int CurrentGeneration => _currentGeneration;
    public string CurrentStage => _currentStage;
    public float BestFitness => _bestFitness;
    public float AverageFitness => _averageFitness;
    public int PopulationSize => _population.Count;
    public int TotalGamesPlayed => _totalGamesPlayed;
    public float WinRate => _totalGamesPlayed > 0 ? (float)_totalWins / _totalGamesPlayed : 0f;
    public DateTime? TrainingStartedAt { get; private set; }

    public IReadOnlyList<DashboardGenerationStats> GenerationHistory
    {
        get
        {
            lock (_lock)
            {
                return _generationHistory.ToArray();
            }
        }
    }

    public IReadOnlyList<GenomeSummary> CurrentPopulation
    {
        get
        {
            lock (_lock)
            {
                return _population.OrderByDescending(g => g.Fitness).ToList();
            }
        }
    }

    public IReadOnlyList<ActivityLogEntry> RecentActivity
    {
        get
        {
            lock (_lock)
            {
                return _activityLog.TakeLast(50).Reverse().ToArray();
            }
        }
    }

    public event Action<DashboardGenerationStats>? OnGenerationComplete;
    public event Action<TrainingStatus>? OnStatusChanged;
    public event Action<string>? OnStageAdvanced;
    public event Action<GenomeSummary>? OnNewBestGenome;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MockTrainingService starting...");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Auto-start training simulation
        _ = Task.Run(() => StartSimulationAsync(), cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("MockTrainingService stopping...");
        _cts?.Cancel();
        _generationTimer?.Dispose();
        Status = TrainingStatus.Stopped;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _cts?.Dispose();
        _generationTimer?.Dispose();
    }

    public Task PauseAsync()
    {
        if (Status == TrainingStatus.Running)
        {
            Status = TrainingStatus.Paused;
            _generationTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            AddActivity(ActivityLevel.Info, "Training paused by user");
            OnStatusChanged?.Invoke(Status);
            _logger.LogInformation("Training paused");
        }
        return Task.CompletedTask;
    }

    public Task ResumeAsync()
    {
        if (Status == TrainingStatus.Paused)
        {
            Status = TrainingStatus.Running;
            var interval = _configuration.GetValue<int>("Training:MockGenerationIntervalMs", 3000);
            _generationTimer?.Change(0, interval);
            AddActivity(ActivityLevel.Info, "Training resumed by user");
            OnStatusChanged?.Invoke(Status);
            _logger.LogInformation("Training resumed");
        }
        return Task.CompletedTask;
    }

    public Task SaveCheckpointAsync()
    {
        AddActivity(ActivityLevel.Success, $"Checkpoint saved at generation {_currentGeneration}");
        _logger.LogInformation("Checkpoint saved at generation {Generation}", _currentGeneration);
        return Task.CompletedTask;
    }

    private async Task StartSimulationAsync()
    {
        // Small delay before starting
        await Task.Delay(1000);

        Status = TrainingStatus.Running;
        TrainingStartedAt = DateTime.UtcNow;
        OnStatusChanged?.Invoke(Status);
        AddActivity(ActivityLevel.Success, "Training simulation started");

        var interval = _configuration.GetValue<int>("Training:MockGenerationIntervalMs", 3000);
        _generationTimer = new Timer(
            _ => SimulateGeneration(),
            null,
            0,
            interval);
    }

    private void SimulateGeneration()
    {
        if (Status != TrainingStatus.Running) return;

        try
        {
            lock (_lock)
            {
                _currentGeneration++;

                // Simulate fitness improvement with diminishing returns
                var baseImprovement = 0.5f / (1 + _currentGeneration * 0.05f);
                var noise = (float)(_random.NextDouble() * 0.2 - 0.1);

                // Update population fitness
                foreach (var genome in _population)
                {
                    UpdateGenomeFitness(genome);
                }

                var sortedPopulation = _population.OrderByDescending(g => g.Fitness).ToList();
                var previousBest = _bestFitness;
                _bestFitness = sortedPopulation.First().Fitness;
                _averageFitness = sortedPopulation.Average(g => g.Fitness);

                // Simulate games
                var gamesThisGen = _population.Count * 3;
                var winsThisGen = (int)(gamesThisGen * (0.3f + _averageFitness * 0.005f));
                _totalGamesPlayed += gamesThisGen;
                _totalWins += winsThisGen;

                // Check for stage advancement
                CheckStageAdvancement();

                var stats = new DashboardGenerationStats
                {
                    Generation = _currentGeneration,
                    Stage = _currentStage,
                    BestFitness = _bestFitness,
                    AverageFitness = _averageFitness,
                    WorstFitness = sortedPopulation.Last().Fitness,
                    FitnessStdDev = CalculateStdDev(sortedPopulation.Select(g => g.Fitness)),
                    EliteCount = 5,
                    WinRate = (float)winsThisGen / gamesThisGen,
                    GamesPlayed = gamesThisGen,
                    Duration = TimeSpan.FromSeconds(_random.Next(10, 60)),
                    Timestamp = DateTime.UtcNow,
                    BestGenomeId = sortedPopulation.First().Id,
                    Improvement = _bestFitness - previousBest
                };

                _generationHistory.Enqueue(stats);

                // Keep history limited
                while (_generationHistory.Count > 500)
                {
                    _generationHistory.TryDequeue(out _);
                }

                // Log activity
                AddActivity(
                    ActivityLevel.Info,
                    $"Generation {_currentGeneration} completed: Best={_bestFitness:F2}, Avg={_averageFitness:F2}",
                    _currentGeneration);

                // Check for new best
                if (_bestFitness > previousBest + 0.5f)
                {
                    var bestGenome = sortedPopulation.First();
                    AddActivity(
                        ActivityLevel.Success,
                        $"New best genome found: {bestGenome.ShortId} with fitness {bestGenome.Fitness:F2}",
                        _currentGeneration,
                        bestGenome.Id);
                    OnNewBestGenome?.Invoke(bestGenome);
                }

                OnGenerationComplete?.Invoke(stats);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in generation simulation");
            AddActivity(ActivityLevel.Error, $"Error in generation: {ex.Message}");
        }
    }

    private void InitializePopulation(int size)
    {
        _population.Clear();

        for (int i = 0; i < size; i++)
        {
            _population.Add(CreateRandomGenome());
        }

        _bestFitness = _population.Max(g => g.Fitness);
        _averageFitness = _population.Average(g => g.Fitness);
    }

    private GenomeSummary CreateRandomGenome()
    {
        var fitness = (float)(_random.NextDouble() * 10);
        var gamesPlayed = _random.Next(5, 20);
        var wins = (int)(gamesPlayed * (0.2 + _random.NextDouble() * 0.3));

        return new GenomeSummary
        {
            Id = Guid.NewGuid(),
            Generation = _currentGeneration,
            Fitness = fitness,
            EloRating = 1000 + (int)(fitness * 50) + _random.Next(-100, 100),
            GamesPlayed = gamesPlayed,
            Wins = wins,
            HiddenLayerCount = _random.Next(2, 5),
            ParameterCount = _random.Next(50000, 200000)
        };
    }

    private void UpdateGenomeFitness(GenomeSummary genome)
    {
        // Simulate fitness change
        var change = (float)(_random.NextDouble() * 0.5 - 0.1);
        var index = _population.IndexOf(genome);
        if (index >= 0)
        {
            var newFitness = Math.Max(0, genome.Fitness + change);
            var newGames = genome.GamesPlayed + _random.Next(1, 4);
            var newWins = genome.Wins + (_random.NextDouble() > 0.5 ? 1 : 0);

            _population[index] = genome with
            {
                Fitness = newFitness,
                GamesPlayed = newGames,
                Wins = (int)newWins,
                EloRating = 1000 + (int)(newFitness * 50)
            };
        }
    }

    private void CheckStageAdvancement()
    {
        var stageIndex = Array.IndexOf(_stages, _currentStage);

        // Advance stage every ~50 generations or when fitness threshold met
        var shouldAdvance =
            (_currentGeneration % 50 == 0 && stageIndex < _stages.Length - 1) ||
            (_averageFitness > (stageIndex + 1) * 20 && stageIndex < _stages.Length - 1);

        if (shouldAdvance)
        {
            var newStage = _stages[stageIndex + 1];
            AddActivity(
                ActivityLevel.Success,
                $"Advanced to {newStage}",
                _currentGeneration);
            _currentStage = newStage;
            OnStageAdvanced?.Invoke(_currentStage);
        }
    }

    private void AddActivity(ActivityLevel level, string message, int? generation = null, Guid? genomeId = null)
    {
        var entry = new ActivityLogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Message = message,
            Generation = generation ?? _currentGeneration,
            GenomeId = genomeId
        };

        _activityLog.Enqueue(entry);

        // Keep log limited
        while (_activityLog.Count > 100)
        {
            _activityLog.TryDequeue(out _);
        }
    }

    private static float CalculateStdDev(IEnumerable<float> values)
    {
        var list = values.ToList();
        if (list.Count == 0) return 0;

        var avg = list.Average();
        var sumOfSquares = list.Sum(v => (v - avg) * (v - avg));
        return (float)Math.Sqrt(sumOfSquares / list.Count);
    }
}
