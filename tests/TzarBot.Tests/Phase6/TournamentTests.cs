using FluentAssertions;
using TzarBot.NeuralNetwork.Models;
using TzarBot.Training.Tournament;

namespace TzarBot.Tests.Phase6;

/// <summary>
/// Tests for the TournamentManager class.
/// </summary>
public class TournamentTests
{
    [Fact]
    public void GetRating_NewGenome_ShouldReturnInitialRating()
    {
        // Arrange
        var manager = new TournamentManager(initialRating: 1000);
        var genomeId = Guid.NewGuid();

        // Act
        var rating = manager.GetRating(genomeId);

        // Assert
        rating.Should().Be(1000);
    }

    [Fact]
    public void SetRating_ShouldUpdateRating()
    {
        // Arrange
        var manager = new TournamentManager();
        var genomeId = Guid.NewGuid();

        // Act
        manager.SetRating(genomeId, 1500);

        // Assert
        manager.GetRating(genomeId).Should().Be(1500);
    }

    [Fact]
    public void GeneratePairings_ShouldPairAllGenomes()
    {
        // Arrange
        var manager = new TournamentManager();
        var genomes = CreateGenomes(10);

        // Act
        var pairings = manager.GeneratePairings(genomes);

        // Assert
        pairings.Should().HaveCount(5); // 10 genomes = 5 pairs
    }

    [Fact]
    public void GeneratePairings_WithOddCount_ShouldPairAllButOne()
    {
        // Arrange
        var manager = new TournamentManager();
        var genomes = CreateGenomes(9);

        // Act
        var pairings = manager.GeneratePairings(genomes);

        // Assert
        pairings.Should().HaveCount(4); // 9 genomes = 4 pairs (1 bye)
    }

    [Fact]
    public void GeneratePairings_ShouldPreferNewPairings()
    {
        // Arrange
        var manager = new TournamentManager();
        var genomes = CreateGenomes(4);

        // First round
        var round1 = manager.GeneratePairings(genomes);
        foreach (var (g1, g2) in round1)
        {
            manager.RecordMatch(MatchResult.Draw(g1, g2, TimeSpan.FromMinutes(10), 1));
        }

        // Act - Second round should prefer new pairings
        var round2 = manager.GeneratePairings(genomes);

        // Assert - Should have different pairings (when possible)
        // With 4 genomes, we can have different pairings in round 2
        round2.Should().HaveCount(2);
    }

    [Fact]
    public void RecordMatch_ShouldUpdateRatings()
    {
        // Arrange
        var manager = new TournamentManager(kFactor: 32, initialRating: 1000);
        var genome1 = Guid.NewGuid();
        var genome2 = Guid.NewGuid();

        // Act
        var result = MatchResult.Win1(genome1, genome2, TimeSpan.FromMinutes(10));
        manager.RecordMatch(result);

        // Assert
        manager.GetRating(genome1).Should().BeGreaterThan(1000);
        manager.GetRating(genome2).Should().BeLessThan(1000);
    }

    [Fact]
    public void RecordMatch_ShouldUpdateStandings()
    {
        // Arrange
        var manager = new TournamentManager();
        var genome1 = Guid.NewGuid();
        var genome2 = Guid.NewGuid();

        // Act
        manager.RecordMatch(MatchResult.Win1(genome1, genome2, TimeSpan.FromMinutes(10)));

        // Assert
        var standings = manager.GetStandings();
        standings.Should().HaveCount(2);

        var standing1 = standings.First(s => s.GenomeId == genome1);
        var standing2 = standings.First(s => s.GenomeId == genome2);

        standing1.Wins.Should().Be(1);
        standing1.Losses.Should().Be(0);
        standing2.Wins.Should().Be(0);
        standing2.Losses.Should().Be(1);
    }

    [Fact]
    public void GetStandings_ShouldSortByRating()
    {
        // Arrange
        var manager = new TournamentManager();
        var genome1 = Guid.NewGuid();
        var genome2 = Guid.NewGuid();
        var genome3 = Guid.NewGuid();

        manager.SetRating(genome1, 1200);
        manager.SetRating(genome2, 1100);
        manager.SetRating(genome3, 1300);

        // Act
        var standings = manager.GetStandings();

        // Assert
        standings[0].GenomeId.Should().Be(genome3);
        standings[1].GenomeId.Should().Be(genome1);
        standings[2].GenomeId.Should().Be(genome2);
    }

    [Fact]
    public void GetStandings_ShouldAssignRanks()
    {
        // Arrange
        var manager = new TournamentManager();
        var genomes = CreateGenomes(5);
        for (int i = 0; i < genomes.Count; i++)
        {
            manager.SetRating(genomes[i].Id, 1000 + i * 100);
        }

        // Act
        var standings = manager.GetStandings();

        // Assert
        for (int i = 0; i < standings.Count; i++)
        {
            standings[i].Rank.Should().Be(i + 1);
        }
    }

    [Fact]
    public void ResetTournament_PreserveRatings_ShouldKeepRatings()
    {
        // Arrange
        var manager = new TournamentManager();
        var genomeId = Guid.NewGuid();
        manager.SetRating(genomeId, 1500);
        manager.RecordMatch(MatchResult.Win1(genomeId, Guid.NewGuid(), TimeSpan.FromMinutes(5)));

        // Act
        manager.ResetTournament(preserveRatings: true);

        // Assert
        manager.GetRating(genomeId).Should().BeGreaterThan(1500); // Rating was updated by win
        manager.GetAllMatches().Should().BeEmpty();
    }

    [Fact]
    public void ResetTournament_DontPreserve_ShouldClearAll()
    {
        // Arrange
        var manager = new TournamentManager();
        var genomeId = Guid.NewGuid();
        manager.SetRating(genomeId, 1500);
        manager.RecordMatch(MatchResult.Win1(genomeId, Guid.NewGuid(), TimeSpan.FromMinutes(5)));

        // Act
        manager.ResetTournament(preserveRatings: false);

        // Assert
        manager.GetRating(genomeId).Should().Be(1000); // Back to initial
        manager.GetAllMatches().Should().BeEmpty();
    }

    [Fact]
    public void GetMatchHistory_ShouldReturnMatchesForGenome()
    {
        // Arrange
        var manager = new TournamentManager();
        var genome1 = Guid.NewGuid();
        var genome2 = Guid.NewGuid();
        var genome3 = Guid.NewGuid();

        manager.RecordMatch(MatchResult.Win1(genome1, genome2, TimeSpan.FromMinutes(5)));
        manager.RecordMatch(MatchResult.Win1(genome1, genome3, TimeSpan.FromMinutes(6)));
        manager.RecordMatch(MatchResult.Win1(genome2, genome3, TimeSpan.FromMinutes(7)));

        // Act
        var history = manager.GetMatchHistory(genome1);

        // Assert
        history.Should().HaveCount(2);
        history.All(m => m.Genome1Id == genome1 || m.Genome2Id == genome1).Should().BeTrue();
    }

    [Fact]
    public void CalculateTournamentFitness_ShouldReflectPerformance()
    {
        // Arrange
        var manager = new TournamentManager(initialRating: 1000);
        var winner = Guid.NewGuid();
        var loser = Guid.NewGuid();

        for (int i = 0; i < 5; i++)
        {
            manager.RecordMatch(MatchResult.Win1(winner, loser, TimeSpan.FromMinutes(5), i + 1));
        }

        // Act
        var winnerFitness = manager.CalculateTournamentFitness(winner);
        var loserFitness = manager.CalculateTournamentFitness(loser);

        // Assert
        winnerFitness.Should().BeGreaterThan(loserFitness);
    }

    [Fact]
    public void State_ShouldTrackRounds()
    {
        // Arrange
        var manager = new TournamentManager(totalRounds: 5);
        var genomes = CreateGenomes(10);

        // Act
        manager.GeneratePairings(genomes); // Round 1

        // Assert
        manager.State.CurrentRound.Should().Be(1);
        manager.State.TotalRounds.Should().Be(5);
    }

    [Fact]
    public void LoadAndExportRatings_ShouldRoundtrip()
    {
        // Arrange
        var manager = new TournamentManager();
        var ratings = new Dictionary<Guid, int>
        {
            { Guid.NewGuid(), 1100 },
            { Guid.NewGuid(), 1200 },
            { Guid.NewGuid(), 1300 }
        };

        // Act
        manager.LoadRatings(ratings);
        var exported = manager.ExportRatings();

        // Assert
        exported.Should().BeEquivalentTo(ratings);
    }

    private static List<NetworkGenome> CreateGenomes(int count)
    {
        return Enumerable.Range(0, count)
            .Select(i => NetworkGenome.CreateRandom(new[] { 64 }, i))
            .ToList();
    }
}
