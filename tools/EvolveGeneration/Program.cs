using System.Text.Json;
using TzarBot.GeneticAlgorithm.Engine;
using TzarBot.GeneticAlgorithm.Operators;
using TzarBot.NeuralNetwork;
using TzarBot.NeuralNetwork.Models;
using TzarBot.NeuralNetwork.Onnx;

namespace EvolveGeneration;

/// <summary>
/// Evolves next generation from top performers of previous generation.
/// Usage: EvolveGeneration.exe <source_gen_dir> <results_json> <output_dir> [options]
///
/// Example:
///   EvolveGeneration.exe training/generation_0 training/generation_0/results/batch_summary.json training/generation_1
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=== TzarBot Generation Evolver ===\n");

        if (args.Length < 3)
        {
            PrintUsage();
            return 1;
        }

        string sourceDir = args[0];
        string resultsFile = args[1];
        string outputDir = args[2];

        // Parse options
        int topCount = 10;
        int populationSize = 20;
        double mutationRate = 0.15;
        double mutationSigma = 0.1; // Mutation strength
        int eliteCount = -1; // -1 means use eliteRatio
        double eliteRatio = 0.3; // Default: 30% elites
        double randomRatio = 0.1; // Default: 10% random new networks
        int mutatedCopies = 0; // Number of mutated copies of best network (legacy)
        int mutatedPerElite = 0; // Number of mutated copies per elite network
        double mutatedCopiesSigma = 0.25; // Sigma for mutated copies
        int seed = (int)DateTime.Now.Ticks;

        for (int i = 3; i < args.Length; i++)
        {
            if (args[i] == "--top" && i + 1 < args.Length)
                topCount = int.Parse(args[++i]);
            else if (args[i] == "--population" && i + 1 < args.Length)
                populationSize = int.Parse(args[++i]);
            else if (args[i] == "--mutation" && i + 1 < args.Length)
                mutationRate = double.Parse(args[++i]);
            else if (args[i] == "--mutation-sigma" && i + 1 < args.Length)
                mutationSigma = double.Parse(args[++i]);
            else if (args[i] == "--elite" && i + 1 < args.Length)
                eliteCount = int.Parse(args[++i]);
            else if (args[i] == "--elite-ratio" && i + 1 < args.Length)
                eliteRatio = double.Parse(args[++i]);
            else if (args[i] == "--random-ratio" && i + 1 < args.Length)
                randomRatio = double.Parse(args[++i]);
            else if (args[i] == "--mutated-copies" && i + 1 < args.Length)
                mutatedCopies = int.Parse(args[++i]);
            else if (args[i] == "--mutated-per-elite" && i + 1 < args.Length)
                mutatedPerElite = int.Parse(args[++i]);
            else if (args[i] == "--mutated-sigma" && i + 1 < args.Length)
                mutatedCopiesSigma = double.Parse(args[++i]);
            else if (args[i] == "--seed" && i + 1 < args.Length)
                seed = int.Parse(args[++i]);
        }

        // Calculate elite count from ratio if not explicitly set
        if (eliteCount < 0)
        {
            eliteCount = (int)(populationSize * eliteRatio);
        }
        int randomCount = (int)(populationSize * randomRatio);

        // Total mutations = legacy mutatedCopies (of best) + mutatedPerElite * eliteCount
        int totalMutations = mutatedCopies + (mutatedPerElite * eliteCount);
        int crossoverCount = populationSize - eliteCount - randomCount - totalMutations;
        if (crossoverCount < 0) crossoverCount = 0;

        Console.WriteLine($"Configuration:");
        Console.WriteLine($"  Source: {sourceDir}");
        Console.WriteLine($"  Results: {resultsFile}");
        Console.WriteLine($"  Output: {outputDir}");
        Console.WriteLine($"  Top performers: {topCount}");
        Console.WriteLine($"  New population: {populationSize}");
        Console.WriteLine($"  Mutation rate: {mutationRate:P0}");
        Console.WriteLine($"  Mutation sigma: {mutationSigma}");
        Console.WriteLine($"  Elite count: {eliteCount}");
        if (mutatedCopies > 0)
            Console.WriteLine($"  Mutated copies (best): {mutatedCopies} (sigma={mutatedCopiesSigma})");
        if (mutatedPerElite > 0)
            Console.WriteLine($"  Mutated per elite: {mutatedPerElite} x {eliteCount} = {mutatedPerElite * eliteCount} (sigma={mutatedCopiesSigma})");
        Console.WriteLine($"  Crossovers: {crossoverCount}");
        Console.WriteLine($"  Random count: {randomCount}");
        Console.WriteLine($"  Seed: {seed}");
        Console.WriteLine();

        try
        {
            await EvolveAsync(sourceDir, resultsFile, outputDir,
                topCount, populationSize, mutationRate, mutationSigma, eliteCount, randomCount,
                crossoverCount, mutatedCopies, mutatedPerElite, mutatedCopiesSigma, seed);
            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Console.ResetColor();
            return 1;
        }
    }

    static void PrintUsage()
    {
        Console.WriteLine("Usage: EvolveGeneration.exe <source_gen_dir> <results_json> <output_dir> [options]");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  source_gen_dir  - Directory with previous generation (genomes/, onnx/)");
        Console.WriteLine("  results_json    - JSON file with training results (batch_summary.json)");
        Console.WriteLine("  output_dir      - Directory for new generation");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --top N            - Number of top performers to select (default: 10)");
        Console.WriteLine("  --population N     - New population size (default: 20)");
        Console.WriteLine("  --mutation R       - Mutation rate 0.0-1.0 (default: 0.15)");
        Console.WriteLine("  --mutation-sigma S - Mutation strength (default: 0.1)");
        Console.WriteLine("  --elite N          - Number of elites (overrides --elite-ratio)");
        Console.WriteLine("  --elite-ratio R    - Ratio of elites 0.0-1.0 (default: 0.3 = 30%)");
        Console.WriteLine("  --random-ratio R     - Ratio of random new networks 0.0-1.0 (default: 0.1 = 10%)");
        Console.WriteLine("  --mutated-copies N   - Create N mutated copies of best network only");
        Console.WriteLine("  --mutated-per-elite N - Create N mutated copies per EACH elite network");
        Console.WriteLine("  --mutated-sigma S    - Sigma for mutated copies (default: 0.25)");
        Console.WriteLine("  --seed N             - Random seed (default: current time)");
        Console.WriteLine();
        Console.WriteLine("Population split: elites + mutations + crossovers + random = population");
        Console.WriteLine("  mutations = mutated-copies + (mutated-per-elite * elite)");
        Console.WriteLine("  crossovers = population - elites - mutations - random");
        Console.WriteLine();
        Console.WriteLine("Example (population 50):");
        Console.WriteLine(@"  EvolveGeneration.exe training\gen_12 training\gen_12\results\summary.json training\gen_13 --population 50 --elite 10 --mutated-per-elite 2 --random-ratio 0.08");
    }

    static async Task EvolveAsync(
        string sourceDir,
        string resultsFile,
        string outputDir,
        int topCount,
        int populationSize,
        double mutationRate,
        double mutationSigma,
        int eliteCount,
        int randomCount,
        int crossoverCount,
        int mutatedCopies,
        int mutatedPerElite,
        double mutatedCopiesSigma,
        int seed)
    {
        var random = new Random(seed);

        // 1. Load results and rank networks
        Console.WriteLine("Loading training results...");
        var results = await LoadResultsAsync(resultsFile);

        // Sort by Fitness score (calculated in training script)
        // Fitness = (victories * 100) + (timeouts * 30) + (totalActions / 10)
        // This rewards survival over raw action count
        var ranked = results
            .OrderByDescending(r => r.Fitness)
            .ThenByDescending(r => r.Actions)
            .Take(topCount)
            .ToList();

        Console.WriteLine($"\nTop {topCount} performers (by Fitness):");
        for (int i = 0; i < ranked.Count; i++)
        {
            var r = ranked[i];
            Console.WriteLine($"  {i + 1}. Network {r.NetworkId:D2}: Fitness={r.Fitness:F1}, {r.Actions} actions, {r.APS:F2} APS");
        }

        // 2. Load genomes for top performers
        Console.WriteLine("\nLoading genomes...");
        var genomes = new List<(NetworkGenome genome, TrialResult result)>();

        foreach (var result in ranked)
        {
            string genomePath = Path.Combine(sourceDir, "genomes", $"genome_{result.NetworkId:D2}.bin");
            if (!File.Exists(genomePath))
            {
                Console.WriteLine($"  WARNING: Genome not found: {genomePath}");
                continue;
            }

            var genome = GenomeSerializer.Load(genomePath);
            genome.Fitness = (float)result.Actions; // Set fitness from results
            genomes.Add((genome, result));
            Console.WriteLine($"  Loaded genome_{result.NetworkId:D2}: {genome.HiddenLayers.Count} layers, {genome.Weights.Length} weights");
        }

        if (genomes.Count < 2)
        {
            throw new InvalidOperationException("Need at least 2 genomes to evolve");
        }

        // 3. Create GA operators
        var config = new GeneticAlgorithmConfig
        {
            PopulationSize = populationSize,
            ElitismRate = (float)eliteCount / populationSize,
            MutationRate = (float)mutationRate,
            CrossoverRate = 0.8f,
            WeightMutationStrength = (float)mutationSigma,
            MinWeight = -3f,
            MaxWeight = 3f,
            Seed = seed
        };

        var crossover = new UniformCrossover(config);
        var mutator = new WeightMutator(config);

        // 4. Create new population
        Console.WriteLine($"\nCreating generation 1 ({populationSize} networks)...");
        var newPopulation = new List<NetworkGenome>();

        // Add elites (unchanged)
        for (int i = 0; i < Math.Min(eliteCount, genomes.Count); i++)
        {
            var elite = genomes[i].genome.Clone();
            elite.Generation = 1;
            elite.Id = Guid.NewGuid(); // New ID for tracking
            elite.ParentIds = new[] { genomes[i].genome.Id };
            elite.Fitness = 0; // Reset fitness for new evaluation
            newPopulation.Add(elite);
            Console.WriteLine($"  Elite {i}: From network_{genomes[i].result.NetworkId:D2} (unchanged)");
        }

        // Create mutated copies of best network only (legacy --mutated-copies)
        if (mutatedCopies > 0 && genomes.Count > 0)
        {
            var bestGenome = genomes[0].genome;
            var strongMutatorConfig = new GeneticAlgorithmConfig
            {
                WeightMutationStrength = (float)mutatedCopiesSigma,
                MinWeight = -3f,
                MaxWeight = 3f,
                MutationRate = 1.0f // Always mutate
            };
            var strongMutator = new WeightMutator(strongMutatorConfig);

            Console.WriteLine($"  Creating {mutatedCopies} mutated copies of best network (sigma={mutatedCopiesSigma})...");
            for (int i = 0; i < mutatedCopies; i++)
            {
                var mutated = bestGenome.Clone();
                mutated.Generation = 1;
                mutated.Id = Guid.NewGuid();
                mutated.ParentIds = new[] { bestGenome.Id };
                strongMutator.Mutate(mutated, random);
                mutated.Fitness = 0;
                newPopulation.Add(mutated);
                Console.WriteLine($"  MutatedCopy {i}: From network_{genomes[0].result.NetworkId:D2} (sigma={mutatedCopiesSigma})");
            }
        }

        // Create mutated copies for EACH elite (--mutated-per-elite)
        if (mutatedPerElite > 0 && genomes.Count > 0)
        {
            var strongMutatorConfig = new GeneticAlgorithmConfig
            {
                WeightMutationStrength = (float)mutatedCopiesSigma,
                MinWeight = -3f,
                MaxWeight = 3f,
                MutationRate = 1.0f // Always mutate
            };
            var strongMutator = new WeightMutator(strongMutatorConfig);

            int elitesToMutate = Math.Min(eliteCount, genomes.Count);
            Console.WriteLine($"  Creating {mutatedPerElite} mutated copies per elite x {elitesToMutate} elites = {mutatedPerElite * elitesToMutate} total (sigma={mutatedCopiesSigma})...");

            for (int e = 0; e < elitesToMutate; e++)
            {
                var eliteGenome = genomes[e].genome;
                var eliteNetworkId = genomes[e].result.NetworkId;

                for (int m = 0; m < mutatedPerElite; m++)
                {
                    var mutated = eliteGenome.Clone();
                    mutated.Generation = 1;
                    mutated.Id = Guid.NewGuid();
                    mutated.ParentIds = new[] { eliteGenome.Id };
                    strongMutator.Mutate(mutated, random);
                    mutated.Fitness = 0;
                    newPopulation.Add(mutated);
                    Console.WriteLine($"  MutatedElite {e}.{m}: From network_{eliteNetworkId:D2} (sigma={mutatedCopiesSigma})");
                }
            }
        }

        // Create crossovers (from top performers / elite pool)
        Console.WriteLine($"  Creating {crossoverCount} crossovers (top{topCount} x top{topCount})...");
        for (int i = 0; i < crossoverCount; i++)
        {
            var parent1 = SelectParent(genomes, random);
            var parent2 = SelectParent(genomes, random);

            // Ensure different parents
            int attempts = 0;
            while (parent2.genome.Id == parent1.genome.Id && attempts < 10)
            {
                parent2 = SelectParent(genomes, random);
                attempts++;
            }

            NetworkGenome child;
            if (CanCrossover(parent1.genome, parent2.genome))
            {
                child = crossover.CrossoverSingle(parent1.genome, parent2.genome, random);
                Console.WriteLine($"  Crossover {i}: network_{parent1.result.NetworkId:D2} x network_{parent2.result.NetworkId:D2}");
            }
            else
            {
                child = (parent1.result.Actions >= parent2.result.Actions
                    ? parent1.genome : parent2.genome).Clone();
                child.ParentIds = new[] { child.Id };
                child.Id = Guid.NewGuid();
                Console.WriteLine($"  Crossover {i}: Cloned network_{(parent1.result.Actions >= parent2.result.Actions ? parent1 : parent2).result.NetworkId:D2} (incompatible structures)");
            }

            if (random.NextDouble() < mutationRate)
            {
                mutator.Mutate(child, random);
            }

            child.Generation = 1;
            child.Fitness = 0;
            newPopulation.Add(child);
        }

        // Create random new networks with random architecture
        // Favor larger architectures for complex future tasks
        Console.WriteLine($"  Creating {randomCount} random networks...");
        var randomArchitectures = new int[][]
        {
            new[] { 256, 128 },               // 2 layers medium
            new[] { 512, 256 },               // 2 layers large
            new[] { 256, 128, 64 },           // 3 layers medium
            new[] { 512, 256, 128 },          // 3 layers large
            new[] { 512, 256, 128, 64 },      // 4 layers (default)
            new[] { 1024, 512, 256, 128 },    // 4 layers large
            new[] { 512, 512, 256, 128 },     // 4 layers wide
            new[] { 1024, 512, 256 },         // 3 layers very large
        };

        for (int i = 0; i < randomCount; i++)
        {
            var arch = randomArchitectures[random.Next(randomArchitectures.Length)];
            var randomGenome = NetworkGenome.CreateRandom(arch, random.Next());
            randomGenome.Generation = 1;
            randomGenome.Fitness = 0;
            randomGenome.ParentIds = Array.Empty<Guid>(); // No parents
            newPopulation.Add(randomGenome);
            Console.WriteLine($"  Random {i}: {string.Join("-", arch)} architecture");
        }

        // 5. Create output directories
        string genomesDir = Path.Combine(outputDir, "genomes");
        string onnxDir = Path.Combine(outputDir, "onnx");
        string reportsDir = Path.Combine(outputDir, "reports");

        Directory.CreateDirectory(genomesDir);
        Directory.CreateDirectory(onnxDir);
        Directory.CreateDirectory(reportsDir);

        // 6. Save new population
        Console.WriteLine("\nSaving generation 1...");
        var networkConfig = NetworkConfig.Default();
        var exporter = new OnnxModelExporter(networkConfig);

        for (int i = 0; i < newPopulation.Count; i++)
        {
            var genome = newPopulation[i];

            string genomeFile = Path.Combine(genomesDir, $"genome_{i:D2}.bin");
            string onnxFile = Path.Combine(onnxDir, $"network_{i:D2}.onnx");

            GenomeSerializer.Save(genome, genomeFile);
            exporter.Export(genome, onnxFile);

            Console.WriteLine($"  {i:D2}: {genome.HiddenLayers.Count} layers, {genome.Weights.Length} weights -> {Path.GetFileName(onnxFile)}");
        }

        // Save population.bin
        string populationFile = Path.Combine(outputDir, "population.bin");
        await GenomeSerializer.SavePopulationAsync(newPopulation, populationFile);

        // 7. Generate evolution report
        var report = new EvolutionReport
        {
            GeneratedAt = DateTime.UtcNow,
            SourceGeneration = 0,
            TargetGeneration = 1,
            TopPerformers = ranked.Select(r => new PerformerInfo
            {
                NetworkId = r.NetworkId,
                Actions = r.Actions,
                APS = r.APS
            }).ToList(),
            EliteCount = eliteCount,
            CrossoverCount = crossoverCount,
            RandomCount = randomCount,
            MutationRate = mutationRate,
            PopulationSize = populationSize,
            Seed = seed
        };

        string reportFile = Path.Combine(reportsDir, "evolution_report.json");
        await File.WriteAllTextAsync(reportFile,
            JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"SUCCESS: Generation 1 created with {newPopulation.Count} networks!");
        Console.WriteLine($"  Output: {outputDir}");
        Console.WriteLine($"  Report: {reportFile}");
        Console.ResetColor();
    }

    static (NetworkGenome genome, TrialResult result) SelectParent(
        List<(NetworkGenome genome, TrialResult result)> genomes,
        Random random)
    {
        // Tournament selection (size 3)
        int tournamentSize = Math.Min(3, genomes.Count);
        var candidates = new List<(NetworkGenome genome, TrialResult result)>();

        for (int i = 0; i < tournamentSize; i++)
        {
            int idx = random.Next(genomes.Count);
            candidates.Add(genomes[idx]);
        }

        return candidates.OrderByDescending(c => c.result.Actions).First();
    }

    static bool CanCrossover(NetworkGenome g1, NetworkGenome g2)
    {
        // Can crossover if same hidden layer structure
        if (g1.HiddenLayers.Count != g2.HiddenLayers.Count)
            return false;

        for (int i = 0; i < g1.HiddenLayers.Count; i++)
        {
            if (g1.HiddenLayers[i].NeuronCount != g2.HiddenLayers[i].NeuronCount)
                return false;
        }

        return g1.Weights.Length == g2.Weights.Length;
    }

    static async Task<List<TrialResult>> LoadResultsAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var results = JsonSerializer.Deserialize<List<TrialResult>>(json, options)
            ?? throw new InvalidOperationException("Failed to parse results");

        // Normalize from new format if needed
        foreach (var r in results)
        {
            r.NormalizeFromNewFormat();
        }

        return results;
    }
}

class TrialResult
{
    // Old format
    public int NetworkId { get; set; }
    public string Outcome { get; set; } = "";
    public double Duration { get; set; }
    public double Actions { get; set; }
    public double APS { get; set; }
    public double InferenceMs { get; set; }
    public double Fitness { get; set; }

    // New format from train_generation_staggered.ps1
    public string Network { get; set; } = "";
    public int? V { get; set; }
    public int? D { get; set; }
    public int? T { get; set; }
    public double AvgDur { get; set; }
    public double AvgAct { get; set; }

    public void NormalizeFromNewFormat()
    {
        if (!string.IsNullOrEmpty(Network) && Network.StartsWith("network_"))
        {
            NetworkId = int.Parse(Network.Replace("network_", ""));
            Actions = AvgAct;
            Duration = AvgDur;
            // Fitness already set
        }
    }
}

class EvolutionReport
{
    public DateTime GeneratedAt { get; set; }
    public int SourceGeneration { get; set; }
    public int TargetGeneration { get; set; }
    public List<PerformerInfo> TopPerformers { get; set; } = new();
    public int EliteCount { get; set; }
    public int CrossoverCount { get; set; }
    public int RandomCount { get; set; }
    public double MutationRate { get; set; }
    public int PopulationSize { get; set; }
    public int Seed { get; set; }
}

class PerformerInfo
{
    public int NetworkId { get; set; }
    public double Actions { get; set; }
    public double APS { get; set; }
}
