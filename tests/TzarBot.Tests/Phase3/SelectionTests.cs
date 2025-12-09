using FluentAssertions;
using TzarBot.GeneticAlgorithm.Engine;
using TzarBot.GeneticAlgorithm.Selection;
using TzarBot.NeuralNetwork.Models;

namespace TzarBot.Tests.Phase3;

/// <summary>
/// Tests for selection strategies.
/// </summary>
public class SelectionTests
{
    #region Tournament Selection Tests

    [Fact]
    public void TournamentSelection_ShouldSelectGenome()
    {
        // Arrange
        var selection = new TournamentSelection(3);
        var population = CreatePopulationWithFitness(10);
        var random = new Random(42);

        // Act
        var selected = selection.Select(population, random);

        // Assert
        selected.Should().NotBeNull();
        population.Should().Contain(selected);
    }

    [Fact]
    public void TournamentSelection_ShouldFavorHigherFitness()
    {
        // Arrange
        var selection = new TournamentSelection(3);
        var population = CreatePopulationWithFitness(100);
        var random = new Random(42);

        // Act - select many times and count
        var selectedFitness = new List<float>();
        for (int i = 0; i < 1000; i++)
        {
            var selected = selection.Select(population, random);
            selectedFitness.Add(selected.Fitness);
        }

        // Assert - average selected fitness should be higher than population average
        float avgPopulationFitness = population.Average(g => g.Fitness);
        float avgSelectedFitness = selectedFitness.Average();
        avgSelectedFitness.Should().BeGreaterThan(avgPopulationFitness);
    }

    [Fact]
    public void TournamentSelection_ShouldSelectManyGenomes()
    {
        // Arrange
        var selection = new TournamentSelection(3);
        var population = CreatePopulationWithFitness(50);
        var random = new Random(42);

        // Act
        var selected = selection.SelectMany(population, 10, random).ToList();

        // Assert
        selected.Should().HaveCount(10);
        selected.Should().OnlyContain(g => population.Contains(g));
    }

    [Fact]
    public void TournamentSelection_ShouldSelectPairs()
    {
        // Arrange
        var selection = new TournamentSelection(3);
        var population = CreatePopulationWithFitness(50);
        var random = new Random(42);

        // Act
        var pairs = selection.SelectPairs(population, 10, random).ToList();

        // Assert
        pairs.Should().HaveCount(10);
        foreach (var (parent1, parent2) in pairs)
        {
            parent1.Should().NotBeNull();
            parent2.Should().NotBeNull();
            population.Should().Contain(parent1);
            population.Should().Contain(parent2);
        }
    }

    [Fact]
    public void TournamentSelection_ShouldTryToSelectDifferentParents()
    {
        // Arrange
        var selection = new TournamentSelection(3);
        var population = CreatePopulationWithFitness(10);
        var random = new Random(42);

        // Act
        var pairs = selection.SelectPairs(population, 20, random).ToList();

        // Assert - most pairs should have different parents
        int differentPairs = pairs.Count(p => p.parent1.Id != p.parent2.Id);
        differentPairs.Should().BeGreaterThan(pairs.Count / 2);
    }

    [Fact]
    public void TournamentSelection_ShouldThrowForEmptyPopulation()
    {
        // Arrange
        var selection = new TournamentSelection(3);
        var population = new List<NetworkGenome>();
        var random = new Random(42);

        // Act & Assert
        Action act = () => selection.Select(population, random);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TournamentSelection_ShouldHandleSmallPopulation()
    {
        // Arrange
        var selection = new TournamentSelection(10); // Tournament larger than population
        var population = CreatePopulationWithFitness(3);
        var random = new Random(42);

        // Act
        var selected = selection.Select(population, random);

        // Assert
        selected.Should().NotBeNull();
    }

    [Fact]
    public void TournamentSelection_SelectDeterministic_ShouldReturnBestFromCandidates()
    {
        // Arrange
        var selection = new TournamentSelection(3);
        var population = CreatePopulationWithFitness(10);
        int[] candidates = { 2, 5, 7 };

        // Find expected best
        float maxFitness = candidates.Max(i => population[i].Fitness);
        var expected = population.First(g => g.Fitness == maxFitness);

        // Act
        var selected = selection.SelectDeterministic(population, candidates);

        // Assert
        selected.Fitness.Should().Be(maxFitness);
    }

    #endregion

    #region Elitism Tests

    [Fact]
    public void ElitismStrategy_ShouldSelectTopPerformers()
    {
        // Arrange
        var elitism = new ElitismStrategy(0.1f); // 10%
        var population = CreatePopulationWithFitness(100);

        // Act
        var elites = elitism.SelectElites(population);

        // Assert
        elites.Should().HaveCount(10); // 10% of 100
        elites.Should().OnlyContain(e =>
            population.OrderByDescending(g => g.Fitness).Take(10).Any(g => g.Fitness == e.Fitness));
    }

    [Fact]
    public void ElitismStrategy_ShouldReturnClones()
    {
        // Arrange
        var elitism = new ElitismStrategy(0.1f);
        var population = CreatePopulationWithFitness(10);

        // Act
        var elites = elitism.SelectElites(population);

        // Assert - elites should have different IDs than originals
        foreach (var elite in elites)
        {
            population.Should().NotContain(g => g.Id == elite.Id);
        }
    }

    [Fact]
    public void ElitismStrategy_GetBest_ShouldReturnHighestFitness()
    {
        // Arrange
        var elitism = new ElitismStrategy(0.1f);
        var population = CreatePopulationWithFitness(50);
        var expectedBest = population.MaxBy(g => g.Fitness);

        // Act
        var best = elitism.GetBest(population);

        // Assert
        best.Should().NotBeNull();
        best!.Fitness.Should().Be(expectedBest!.Fitness);
    }

    [Fact]
    public void ElitismStrategy_CalculateEliteCount_ShouldRespectMinimum()
    {
        // Arrange
        var elitism = new ElitismStrategy(0.01f, minimumElites: 5); // 1% but at least 5

        // Act
        int count = elitism.CalculateEliteCount(100);

        // Assert
        count.Should().BeGreaterThanOrEqualTo(5);
    }

    [Fact]
    public void ElitismStrategy_IsElite_ShouldReturnTrue_ForTopPerformers()
    {
        // Arrange
        var elitism = new ElitismStrategy(0.1f);
        var population = CreatePopulationWithFitness(100);
        var best = population.OrderByDescending(g => g.Fitness).First();

        // Act
        bool isElite = elitism.IsElite(best, population);

        // Assert
        isElite.Should().BeTrue();
    }

    [Fact]
    public void ElitismStrategy_IsElite_ShouldReturnFalse_ForLowPerformers()
    {
        // Arrange
        var elitism = new ElitismStrategy(0.1f);
        var population = CreatePopulationWithFitness(100);
        var worst = population.OrderBy(g => g.Fitness).First();

        // Act
        bool isElite = elitism.IsElite(worst, population);

        // Assert
        isElite.Should().BeFalse();
    }

    [Fact]
    public void ElitismStrategy_CombineWithOffspring_ShouldRespectTargetSize()
    {
        // Arrange
        var elitism = new ElitismStrategy(0.1f);
        var elites = CreatePopulationWithFitness(10);
        var offspring = CreatePopulationWithFitness(100);

        // Act
        var combined = elitism.CombineWithOffspring(elites, offspring, 50);

        // Assert
        combined.Should().HaveCount(50);
    }

    [Fact]
    public void ElitismStrategy_CombineWithOffspring_ShouldIncludeAllElites()
    {
        // Arrange
        var elitism = new ElitismStrategy(0.1f);
        var elites = CreatePopulationWithFitness(5);
        var offspring = CreatePopulationWithFitness(100);

        // Act
        var combined = elitism.CombineWithOffspring(elites, offspring, 50);

        // Assert
        foreach (var elite in elites)
        {
            combined.Should().Contain(elite);
        }
    }

    [Fact]
    public void ElitismStrategy_GetEliteThreshold_ShouldReturnCorrectValue()
    {
        // Arrange
        var elitism = new ElitismStrategy(0.1f);
        var population = CreatePopulationWithFitness(100);
        var sorted = population.OrderByDescending(g => g.Fitness).ToList();
        float expectedThreshold = sorted[9].Fitness; // 10th best

        // Act
        float threshold = elitism.GetEliteThreshold(population);

        // Assert
        threshold.Should().Be(expectedThreshold);
    }

    [Fact]
    public void ElitismStrategy_SelectDiverseElites_ShouldMaintainDiversity()
    {
        // Arrange
        var elitism = new ElitismStrategy(0.2f); // 20%
        var population = CreateDiversePopulation(50); // Different structures

        // Act
        var elites = elitism.SelectDiverseElites(population, 0.5f);

        // Assert
        elites.Should().HaveCountGreaterThan(0);
        // Should have some structural diversity
        var structures = elites.Select(g => g.HiddenLayers.Count).Distinct().Count();
        structures.Should().BeGreaterThan(1);
    }

    #endregion

    private static List<NetworkGenome> CreatePopulationWithFitness(int count)
    {
        var population = new List<NetworkGenome>(count);
        var random = new Random(42);

        for (int i = 0; i < count; i++)
        {
            var genome = NetworkGenome.CreateRandom(new[] { 128, 64 }, random.Next());
            genome.Fitness = (float)random.NextDouble() * 100f;
            population.Add(genome);
        }

        return population;
    }

    private static List<NetworkGenome> CreateDiversePopulation(int count)
    {
        var population = new List<NetworkGenome>(count);
        var random = new Random(42);
        var structures = new[] { new[] { 128 }, new[] { 256, 128 }, new[] { 512, 256, 128 } };

        for (int i = 0; i < count; i++)
        {
            var structure = structures[i % structures.Length];
            var genome = NetworkGenome.CreateRandom(structure, random.Next());
            genome.Fitness = (float)random.NextDouble() * 100f;
            population.Add(genome);
        }

        return population;
    }
}
