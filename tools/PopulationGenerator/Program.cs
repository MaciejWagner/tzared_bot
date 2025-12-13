using System.Text.Json;
using System.Text.Json.Serialization;
using TzarBot.NeuralNetwork;
using TzarBot.NeuralNetwork.Models;
using TzarBot.NeuralNetwork.Onnx;

namespace PopulationGenerator;

/// <summary>
/// Generates initial population of neural networks for TzarBot training.
/// Creates 20 random networks with Xavier-initialized weights, exports to ONNX and genome files,
/// and generates detailed descriptions of each network.
/// </summary>
class Program
{
    private const int PopulationSize = 20;
    private const int BaseSeed = 20251213; // Date-based seed for reproducibility

    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=== TzarBot Population Generator ===\n");

        // Parse arguments
        string outputDir = args.Length > 0 ? args[0] : "population_gen0";
        int populationSize = args.Length > 1 && int.TryParse(args[1], out var ps) ? ps : PopulationSize;
        int baseSeed = args.Length > 2 && int.TryParse(args[2], out var bs) ? bs : BaseSeed;

        Console.WriteLine($"Configuration:");
        Console.WriteLine($"  Output directory: {outputDir}");
        Console.WriteLine($"  Population size: {populationSize}");
        Console.WriteLine($"  Base seed: {baseSeed}");
        Console.WriteLine();

        try
        {
            var generator = new PopulationGeneratorCore(outputDir, populationSize, baseSeed);
            await generator.GenerateAsync();
            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }
}

/// <summary>
/// Core logic for population generation.
/// </summary>
class PopulationGeneratorCore
{
    private readonly string _outputDir;
    private readonly int _populationSize;
    private readonly int _baseSeed;
    private readonly NetworkConfig _config;
    private readonly OnnxModelExporter _exporter;

    // Different hidden layer configurations for diversity
    private static readonly int[][] HiddenLayerConfigs = new[]
    {
        new[] { 256, 128 },           // Standard 2-layer
        new[] { 512, 256 },           // Larger 2-layer
        new[] { 128, 64 },            // Smaller 2-layer
        new[] { 256, 128, 64 },       // 3-layer pyramid
        new[] { 512, 256, 128 },      // Larger 3-layer
        new[] { 128, 128 },           // Uniform 2-layer
        new[] { 256, 256 },           // Uniform larger
        new[] { 384, 192 },           // Non-standard sizes
        new[] { 256, 128, 64, 32 },   // 4-layer deep
        new[] { 192, 96 },            // Alternative sizes
    };

    public PopulationGeneratorCore(string outputDir, int populationSize, int baseSeed)
    {
        _outputDir = outputDir;
        _populationSize = populationSize;
        _baseSeed = baseSeed;
        _config = NetworkConfig.Default();
        _exporter = new OnnxModelExporter(_config);
    }

    public async Task GenerateAsync()
    {
        // Create output directories
        string genomesDir = Path.Combine(_outputDir, "genomes");
        string onnxDir = Path.Combine(_outputDir, "onnx");
        string reportsDir = Path.Combine(_outputDir, "reports");

        Directory.CreateDirectory(genomesDir);
        Directory.CreateDirectory(onnxDir);
        Directory.CreateDirectory(reportsDir);

        Console.WriteLine($"Creating directories:");
        Console.WriteLine($"  Genomes: {genomesDir}");
        Console.WriteLine($"  ONNX: {onnxDir}");
        Console.WriteLine($"  Reports: {reportsDir}");
        Console.WriteLine();

        var genomes = new List<NetworkGenome>();
        var genomeInfos = new List<GenomeInfo>();

        Console.WriteLine($"Generating {_populationSize} neural networks...\n");

        for (int i = 0; i < _populationSize; i++)
        {
            // Select hidden layer configuration (cycle through configs)
            var hiddenLayers = HiddenLayerConfigs[i % HiddenLayerConfigs.Length];
            int seed = _baseSeed + i;

            Console.Write($"  [{i + 1:D2}/{_populationSize}] ");

            // Create genome
            var genome = NetworkGenome.CreateRandom(hiddenLayers, seed, _config);

            // Validate genome
            if (!genome.IsValid())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"INVALID - seed {seed}");
                Console.ResetColor();
                continue;
            }

            // Generate filenames
            string genomeFile = Path.Combine(genomesDir, $"genome_{i:D2}.bin");
            string onnxFile = Path.Combine(onnxDir, $"network_{i:D2}.onnx");

            // Save genome (binary MessagePack)
            GenomeSerializer.Save(genome, genomeFile);

            // Export ONNX model
            _exporter.Export(genome, onnxFile);

            // Collect info for report
            var info = new GenomeInfo
            {
                Index = i,
                Id = genome.Id,
                Seed = seed,
                HiddenLayers = string.Join(" -> ", hiddenLayers),
                TotalWeights = genome.Weights.Length,
                GenomeFilePath = genomeFile,
                OnnxFilePath = onnxFile,
                WeightStats = CalculateWeightStats(genome.Weights)
            };

            genomes.Add(genome);
            genomeInfos.Add(info);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"OK - Layers: [{info.HiddenLayers}], Weights: {info.TotalWeights:N0}");
            Console.ResetColor();
        }

        Console.WriteLine();

        // Generate population report
        var populationReport = new PopulationReport
        {
            GeneratedAt = DateTime.UtcNow,
            BaseSeed = _baseSeed,
            PopulationSize = genomes.Count,
            NetworkConfig = new NetworkConfigInfo
            {
                InputShape = $"{_config.InputChannels}x{_config.InputHeight}x{_config.InputWidth}",
                ConvLayers = _config.ConvLayers.Select(c => $"{c.FilterCount}@{c.KernelSize}x{c.KernelSize}s{c.Stride}").ToArray(),
                FlattenedSize = _config.FlattenedConvOutputSize,
                ActionCount = _config.ActionCount
            },
            Genomes = genomeInfos
        };

        // Save JSON report
        string reportFile = Path.Combine(reportsDir, "population_report.json");
        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(reportFile, JsonSerializer.Serialize(populationReport, jsonOptions));
        Console.WriteLine($"Population report saved: {reportFile}");

        // Save detailed markdown report
        string mdReportFile = Path.Combine(reportsDir, "population_report.md");
        await File.WriteAllTextAsync(mdReportFile, GenerateMarkdownReport(populationReport));
        Console.WriteLine($"Markdown report saved: {mdReportFile}");

        // Save population as single binary file (for bulk loading)
        string populationFile = Path.Combine(_outputDir, "population.bin");
        await GenomeSerializer.SavePopulationAsync(genomes, populationFile);
        Console.WriteLine($"Population binary saved: {populationFile}");

        // Generate training protocol
        string protocolFile = Path.Combine(reportsDir, "training_protocol.md");
        await File.WriteAllTextAsync(protocolFile, GenerateTrainingProtocol(populationReport));
        Console.WriteLine($"Training protocol saved: {protocolFile}");

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"SUCCESS: Generated {genomes.Count} neural networks!");
        Console.ResetColor();

        // Summary
        Console.WriteLine("\n=== Summary ===");
        Console.WriteLine($"Total genomes: {genomes.Count}");
        Console.WriteLine($"Total ONNX models: {genomes.Count}");
        Console.WriteLine($"Average weights per network: {genomes.Average(g => g.Weights.Length):N0}");
        Console.WriteLine($"Min weights: {genomes.Min(g => g.Weights.Length):N0}");
        Console.WriteLine($"Max weights: {genomes.Max(g => g.Weights.Length):N0}");
    }

    private WeightStats CalculateWeightStats(float[] weights)
    {
        if (weights.Length == 0)
            return new WeightStats();

        float min = weights.Min();
        float max = weights.Max();
        float mean = weights.Average();
        float variance = weights.Select(w => (w - mean) * (w - mean)).Average();
        float std = (float)Math.Sqrt(variance);

        return new WeightStats
        {
            Min = min,
            Max = max,
            Mean = mean,
            Std = std
        };
    }

    private string GenerateMarkdownReport(PopulationReport report)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("# TzarBot Population Report - Generation 0");
        sb.AppendLine();
        sb.AppendLine($"**Generated:** {report.GeneratedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"**Base Seed:** {report.BaseSeed}");
        sb.AppendLine($"**Population Size:** {report.PopulationSize}");
        sb.AppendLine();

        sb.AppendLine("## Network Architecture");
        sb.AppendLine();
        sb.AppendLine("| Property | Value |");
        sb.AppendLine("|----------|-------|");
        sb.AppendLine($"| Input Shape | {report.NetworkConfig.InputShape} (CHW) |");
        sb.AppendLine($"| Conv Layers | {string.Join(" -> ", report.NetworkConfig.ConvLayers)} |");
        sb.AppendLine($"| Flattened Size | {report.NetworkConfig.FlattenedSize:N0} |");
        sb.AppendLine($"| Action Count | {report.NetworkConfig.ActionCount} |");
        sb.AppendLine($"| Mouse Output | 2 (dx, dy) |");
        sb.AppendLine();

        sb.AppendLine("## Population Overview");
        sb.AppendLine();
        sb.AppendLine("| # | ID | Seed | Hidden Layers | Weights | Mean | Std |");
        sb.AppendLine("|---|----|----- |---------------|---------|------|-----|");

        foreach (var g in report.Genomes)
        {
            sb.AppendLine($"| {g.Index:D2} | {g.Id.ToString()[..8]} | {g.Seed} | {g.HiddenLayers} | {g.TotalWeights:N0} | {g.WeightStats.Mean:F4} | {g.WeightStats.Std:F4} |");
        }

        sb.AppendLine();
        sb.AppendLine("## Individual Network Details");
        sb.AppendLine();

        foreach (var g in report.Genomes)
        {
            sb.AppendLine($"### Network {g.Index:D2}");
            sb.AppendLine();
            sb.AppendLine($"- **ID:** `{g.Id}`");
            sb.AppendLine($"- **Seed:** {g.Seed}");
            sb.AppendLine($"- **Hidden Layers:** {g.HiddenLayers}");
            sb.AppendLine($"- **Total Weights:** {g.TotalWeights:N0}");
            sb.AppendLine($"- **Weight Statistics:**");
            sb.AppendLine($"  - Min: {g.WeightStats.Min:F6}");
            sb.AppendLine($"  - Max: {g.WeightStats.Max:F6}");
            sb.AppendLine($"  - Mean: {g.WeightStats.Mean:F6}");
            sb.AppendLine($"  - Std: {g.WeightStats.Std:F6}");
            sb.AppendLine($"- **Files:**");
            sb.AppendLine($"  - Genome: `{Path.GetFileName(g.GenomeFilePath)}`");
            sb.AppendLine($"  - ONNX: `{Path.GetFileName(g.OnnxFilePath)}`");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string GenerateTrainingProtocol(PopulationReport report)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("# TzarBot Training Protocol - Generation 0");
        sb.AppendLine();
        sb.AppendLine($"**Created:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"**Population Size:** {report.PopulationSize}");
        sb.AppendLine();

        sb.AppendLine("## Protocol Parameters");
        sb.AppendLine();
        sb.AppendLine("| Parameter | Value |");
        sb.AppendLine("|-----------|-------|");
        sb.AppendLine("| Models | 20 |");
        sb.AppendLine("| Trials per Model | 10 |");
        sb.AppendLine("| Total Games | 200 |");
        sb.AppendLine("| Success Criterion | Win game OR score > threshold |");
        sb.AppendLine("| Selection | Top 50% pass to next generation |");
        sb.AppendLine("| Reproduction | Winners produce offspring via crossover + mutation |");
        sb.AppendLine();

        sb.AppendLine("## Evaluation Phases");
        sb.AppendLine();
        sb.AppendLine("### Phase 1: Basic Survival");
        sb.AppendLine("- **Map:** training-0.tzared");
        sb.AppendLine("- **Objective:** Survive as long as possible");
        sb.AppendLine("- **Duration:** 5 minutes per game");
        sb.AppendLine("- **Metrics:**");
        sb.AppendLine("  - Survival time");
        sb.AppendLine("  - Resources gathered");
        sb.AppendLine("  - Buildings constructed");
        sb.AppendLine();

        sb.AppendLine("### Phase 2: Unit Production");
        sb.AppendLine("- **Map:** training-1.tzared");
        sb.AppendLine("- **Objective:** Build and manage units");
        sb.AppendLine("- **Duration:** 10 minutes per game");
        sb.AppendLine("- **Metrics:**");
        sb.AppendLine("  - Units produced");
        sb.AppendLine("  - Actions per minute (APM)");
        sb.AppendLine("  - Resource efficiency");
        sb.AppendLine();

        sb.AppendLine("### Phase 3: Combat");
        sb.AppendLine("- **Map:** training-2.tzared");
        sb.AppendLine("- **Objective:** Defeat AI opponent");
        sb.AppendLine("- **Duration:** 15 minutes per game");
        sb.AppendLine("- **Metrics:**");
        sb.AppendLine("  - Enemy units killed");
        sb.AppendLine("  - Own units lost");
        sb.AppendLine("  - Victory/Defeat outcome");
        sb.AppendLine();

        sb.AppendLine("## Trial Log Template");
        sb.AppendLine();
        sb.AppendLine("Use this template to log each trial:");
        sb.AppendLine();
        sb.AppendLine("```markdown");
        sb.AppendLine("## Trial [TRIAL_NUMBER] - Network [NETWORK_ID]");
        sb.AppendLine();
        sb.AppendLine("| Field | Value |");
        sb.AppendLine("|-------|-------|");
        sb.AppendLine("| Date/Time | [TIMESTAMP] |");
        sb.AppendLine("| Network ID | [ID] |");
        sb.AppendLine("| Phase | [PHASE_NUMBER] |");
        sb.AppendLine("| Map | [MAP_NAME] |");
        sb.AppendLine("| Duration | [SECONDS] |");
        sb.AppendLine("| Outcome | [WIN/LOSS/TIMEOUT] |");
        sb.AppendLine("| Resources Gathered | [NUMBER] |");
        sb.AppendLine("| Units Built | [NUMBER] |");
        sb.AppendLine("| Units Killed | [NUMBER] |");
        sb.AppendLine("| Units Lost | [NUMBER] |");
        sb.AppendLine("| APM | [NUMBER] |");
        sb.AppendLine();
        sb.AppendLine("### Notes");
        sb.AppendLine("[Observations about network behavior]");
        sb.AppendLine("```");
        sb.AppendLine();

        sb.AppendLine("## Generation Summary Template");
        sb.AppendLine();
        sb.AppendLine("```markdown");
        sb.AppendLine("## Generation [N] Summary");
        sb.AppendLine();
        sb.AppendLine("| Network | Trials | Wins | Avg Score | Status |");
        sb.AppendLine("|---------|--------|------|-----------|--------|");
        sb.AppendLine("| Net 00 | 10 | 3 | 45.2 | PASS |");
        sb.AppendLine("| Net 01 | 10 | 1 | 22.5 | FAIL |");
        sb.AppendLine("| ... | ... | ... | ... | ... |");
        sb.AppendLine();
        sb.AppendLine("### Selection Results");
        sb.AppendLine("- Passed: [LIST OF IDs]");
        sb.AppendLine("- Failed: [LIST OF IDs]");
        sb.AppendLine();
        sb.AppendLine("### Next Generation");
        sb.AppendLine("- Parents: [LIST]");
        sb.AppendLine("- Offspring created: [NUMBER]");
        sb.AppendLine("- Mutations applied: [DESCRIPTION]");
        sb.AppendLine("```");
        sb.AppendLine();

        sb.AppendLine("## Network-by-Network Breakdown");
        sb.AppendLine();

        foreach (var g in report.Genomes)
        {
            sb.AppendLine($"### Network {g.Index:D2} (`{g.Id.ToString()[..8]}`)\n");
            sb.AppendLine("| Trial | Phase | Duration | Outcome | Score | Notes |");
            sb.AppendLine("|-------|-------|----------|---------|-------|-------|");
            for (int t = 1; t <= 10; t++)
            {
                sb.AppendLine($"| {t} | - | - | - | - | - |");
            }
            sb.AppendLine($"\n**Summary:** 0/10 wins, Avg Score: 0.0, Status: PENDING\n");
        }

        return sb.ToString();
    }
}

#region Data Classes

class PopulationReport
{
    public DateTime GeneratedAt { get; set; }
    public int BaseSeed { get; set; }
    public int PopulationSize { get; set; }
    public NetworkConfigInfo NetworkConfig { get; set; } = new();
    public List<GenomeInfo> Genomes { get; set; } = new();
}

class NetworkConfigInfo
{
    public string InputShape { get; set; } = "";
    public string[] ConvLayers { get; set; } = Array.Empty<string>();
    public int FlattenedSize { get; set; }
    public int ActionCount { get; set; }
}

class GenomeInfo
{
    public int Index { get; set; }
    public Guid Id { get; set; }
    public int Seed { get; set; }
    public string HiddenLayers { get; set; } = "";
    public int TotalWeights { get; set; }
    public string GenomeFilePath { get; set; } = "";
    public string OnnxFilePath { get; set; } = "";
    public WeightStats WeightStats { get; set; } = new();
}

class WeightStats
{
    public float Min { get; set; }
    public float Max { get; set; }
    public float Mean { get; set; }
    public float Std { get; set; }
}

#endregion
