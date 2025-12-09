using TzarBot.GeneticAlgorithm.Engine;
using TzarBot.NeuralNetwork.Models;

namespace TzarBot.GeneticAlgorithm.Selection;

/// <summary>
/// Elitism strategy: preserves top performers across generations.
///
/// Elitism ensures the best solutions are never lost, providing:
/// - Monotonic improvement guarantee
/// - Stability in evolution
/// - Faster convergence
///
/// Typical elitism rates: 1-10% of population.
/// </summary>
public sealed class ElitismStrategy
{
    private readonly float _elitismRate;
    private readonly int _minimumElites;

    /// <summary>
    /// Rate of elitism (0.0-1.0).
    /// </summary>
    public float ElitismRate => _elitismRate;

    /// <summary>
    /// Creates elitism strategy with specified rate.
    /// </summary>
    /// <param name="elitismRate">Fraction of population to preserve (0.0-1.0).</param>
    /// <param name="minimumElites">Minimum number of elites (default: 1).</param>
    public ElitismStrategy(float elitismRate = 0.05f, int minimumElites = 1)
    {
        if (elitismRate < 0f || elitismRate > 1f)
            throw new ArgumentOutOfRangeException(nameof(elitismRate), "Rate must be between 0 and 1");
        if (minimumElites < 0)
            throw new ArgumentOutOfRangeException(nameof(minimumElites));

        _elitismRate = elitismRate;
        _minimumElites = minimumElites;
    }

    /// <summary>
    /// Creates elitism strategy from config.
    /// </summary>
    public ElitismStrategy(GeneticAlgorithmConfig config)
        : this(config?.ElitismRate ?? 0.05f, 1)
    {
    }

    /// <summary>
    /// Calculates the number of elites for a given population size.
    /// </summary>
    public int CalculateEliteCount(int populationSize)
    {
        int count = (int)Math.Ceiling(populationSize * _elitismRate);
        return Math.Max(count, _minimumElites);
    }

    /// <summary>
    /// Selects elite genomes from the population (highest fitness).
    /// Returns clones to avoid mutation affecting elites.
    /// </summary>
    /// <param name="population">Population to select from.</param>
    /// <returns>Elite genomes (cloned).</returns>
    public IReadOnlyList<NetworkGenome> SelectElites(IReadOnlyList<NetworkGenome> population)
    {
        if (population == null || population.Count == 0)
            return Array.Empty<NetworkGenome>();

        int eliteCount = CalculateEliteCount(population.Count);
        eliteCount = Math.Min(eliteCount, population.Count);

        return population
            .OrderByDescending(g => g.Fitness)
            .Take(eliteCount)
            .Select(g => g.Clone())
            .ToList();
    }

    /// <summary>
    /// Gets elite genomes without cloning (for read-only access).
    /// </summary>
    public IReadOnlyList<NetworkGenome> GetEliteReferences(IReadOnlyList<NetworkGenome> population)
    {
        if (population == null || population.Count == 0)
            return Array.Empty<NetworkGenome>();

        int eliteCount = CalculateEliteCount(population.Count);
        eliteCount = Math.Min(eliteCount, population.Count);

        return population
            .OrderByDescending(g => g.Fitness)
            .Take(eliteCount)
            .ToList();
    }

    /// <summary>
    /// Gets the best genome from the population.
    /// </summary>
    public NetworkGenome? GetBest(IReadOnlyList<NetworkGenome> population)
    {
        if (population == null || population.Count == 0)
            return null;

        return population.MaxBy(g => g.Fitness);
    }

    /// <summary>
    /// Checks if a genome is in the elite set.
    /// </summary>
    public bool IsElite(NetworkGenome genome, IReadOnlyList<NetworkGenome> population)
    {
        if (genome == null || population == null || population.Count == 0)
            return false;

        int eliteCount = CalculateEliteCount(population.Count);
        float eliteThreshold = GetEliteThreshold(population, eliteCount);

        return genome.Fitness >= eliteThreshold;
    }

    /// <summary>
    /// Gets the minimum fitness required to be in the elite set.
    /// </summary>
    public float GetEliteThreshold(IReadOnlyList<NetworkGenome> population, int? eliteCount = null)
    {
        if (population == null || population.Count == 0)
            return float.PositiveInfinity;

        eliteCount ??= CalculateEliteCount(population.Count);
        int count = Math.Min(eliteCount.Value, population.Count);

        return population
            .OrderByDescending(g => g.Fitness)
            .Skip(count - 1)
            .FirstOrDefault()?.Fitness ?? float.NegativeInfinity;
    }

    /// <summary>
    /// Creates a new population by combining elites with new offspring.
    /// </summary>
    /// <param name="elites">Elite genomes to preserve.</param>
    /// <param name="offspring">New offspring to add.</param>
    /// <param name="targetSize">Target population size.</param>
    /// <returns>Combined population.</returns>
    public List<NetworkGenome> CombineWithOffspring(
        IEnumerable<NetworkGenome> elites,
        IEnumerable<NetworkGenome> offspring,
        int targetSize)
    {
        var result = new List<NetworkGenome>(targetSize);

        // Add elites first
        result.AddRange(elites);

        // Add offspring up to target size
        foreach (var child in offspring)
        {
            if (result.Count >= targetSize)
                break;
            result.Add(child);
        }

        return result;
    }

    /// <summary>
    /// Diversity-aware elitism: selects elites while maintaining structural diversity.
    /// </summary>
    /// <param name="population">Population to select from.</param>
    /// <param name="diversityWeight">Weight for diversity (0=pure fitness, 1=pure diversity).</param>
    /// <returns>Diverse elite genomes.</returns>
    public IReadOnlyList<NetworkGenome> SelectDiverseElites(
        IReadOnlyList<NetworkGenome> population,
        float diversityWeight = 0.2f)
    {
        if (population == null || population.Count == 0)
            return Array.Empty<NetworkGenome>();

        int eliteCount = CalculateEliteCount(population.Count);
        eliteCount = Math.Min(eliteCount, population.Count);

        if (diversityWeight <= 0f || eliteCount <= 1)
        {
            return SelectElites(population);
        }

        // Group by structure (layer sizes)
        var structureGroups = population
            .GroupBy(g => string.Join(",", g.HiddenLayers.Select(l => l.NeuronCount)))
            .ToList();

        var elites = new List<NetworkGenome>();

        // Take best from each structure group proportionally
        int remaining = eliteCount;
        foreach (var group in structureGroups.OrderByDescending(g => g.Max(x => x.Fitness)))
        {
            if (remaining <= 0) break;

            int toTake = Math.Max(1, (int)(eliteCount * diversityWeight / structureGroups.Count));
            toTake = Math.Min(toTake, remaining);

            elites.AddRange(group.OrderByDescending(g => g.Fitness).Take(toTake).Select(g => g.Clone()));
            remaining -= toTake;
        }

        // Fill remaining with best overall
        if (remaining > 0)
        {
            var alreadySelected = elites.Select(e => e.ParentIds.FirstOrDefault()).ToHashSet();
            var additional = population
                .Where(g => !alreadySelected.Contains(g.Id))
                .OrderByDescending(g => g.Fitness)
                .Take(remaining)
                .Select(g => g.Clone());

            elites.AddRange(additional);
        }

        return elites;
    }
}
