using FluentAssertions;
using TzarBot.Training.Tournament;

namespace TzarBot.Tests.Phase6;

/// <summary>
/// Tests for the EloCalculator class.
/// Verifies mathematical correctness of ELO rating calculations.
/// </summary>
public class EloCalculatorTests
{
    private readonly EloCalculator _calculator = new(kFactor: 32, initialRating: 1000);

    #region Expected Score Tests

    [Fact]
    public void CalculateExpectedScore_EqualRatings_ShouldReturn05()
    {
        // When ratings are equal, expected score should be 0.5
        var expected = _calculator.CalculateExpectedScore(1000, 1000);

        expected.Should().BeApproximately(0.5f, 0.001f);
    }

    [Fact]
    public void CalculateExpectedScore_HigherRating_ShouldBeGreaterThan05()
    {
        // Higher rated player should have expected score > 0.5
        var expected = _calculator.CalculateExpectedScore(1400, 1000);

        expected.Should().BeGreaterThan(0.5f);
    }

    [Fact]
    public void CalculateExpectedScore_LowerRating_ShouldBeLessThan05()
    {
        // Lower rated player should have expected score < 0.5
        var expected = _calculator.CalculateExpectedScore(1000, 1400);

        expected.Should().BeLessThan(0.5f);
    }

    [Fact]
    public void CalculateExpectedScore_400PointDifference_ShouldBe091()
    {
        // 400 point difference = expected score of ~0.91 for higher player
        // Formula: E = 1 / (1 + 10^(-400/400)) = 1 / (1 + 0.1) = 0.909...
        var expected = _calculator.CalculateExpectedScore(1400, 1000);

        expected.Should().BeApproximately(0.909f, 0.01f);
    }

    [Fact]
    public void CalculateExpectedScore_SumToOne()
    {
        // Expected scores of both players should sum to 1
        var expected1 = _calculator.CalculateExpectedScore(1200, 1000);
        var expected2 = _calculator.CalculateExpectedScore(1000, 1200);

        (expected1 + expected2).Should().BeApproximately(1f, 0.001f);
    }

    #endregion

    #region Rating Change Tests

    [Fact]
    public void CalculateRatingChange_Win_ShouldBePositive()
    {
        var change = _calculator.CalculateRatingChange(1000, 1000, MatchOutcome.Win);

        change.Should().BePositive();
    }

    [Fact]
    public void CalculateRatingChange_Loss_ShouldBeNegative()
    {
        var change = _calculator.CalculateRatingChange(1000, 1000, MatchOutcome.Loss);

        change.Should().BeNegative();
    }

    [Fact]
    public void CalculateRatingChange_Draw_ShouldBeZeroForEqualRatings()
    {
        var change = _calculator.CalculateRatingChange(1000, 1000, MatchOutcome.Draw);

        change.Should().Be(0);
    }

    [Fact]
    public void CalculateRatingChange_Win_EqualRatings_ShouldBeHalfK()
    {
        // Win against equal opponent: K * (1 - 0.5) = K/2 = 16
        var calculator = new EloCalculator(kFactor: 32);
        var change = calculator.CalculateRatingChange(1000, 1000, MatchOutcome.Win);

        change.Should().Be(16);
    }

    [Fact]
    public void CalculateRatingChange_UpsetWin_ShouldGainMore()
    {
        // Winning against a stronger opponent should give more points
        var changeVsWeaker = _calculator.CalculateRatingChange(1200, 1000, MatchOutcome.Win);
        var changeVsStronger = _calculator.CalculateRatingChange(1000, 1200, MatchOutcome.Win);

        changeVsStronger.Should().BeGreaterThan(changeVsWeaker);
    }

    [Fact]
    public void CalculateRatingChange_ExpectedWin_ShouldGainLess()
    {
        // Winning against a weaker opponent should give fewer points
        var changeVsEqual = _calculator.CalculateRatingChange(1000, 1000, MatchOutcome.Win);
        var changeVsWeak = _calculator.CalculateRatingChange(1400, 1000, MatchOutcome.Win);

        changeVsWeak.Should().BeLessThan(changeVsEqual);
    }

    [Fact]
    public void CalculateRatingChange_UpsetLoss_ShouldLoseMore()
    {
        // Losing against a weaker opponent should cost more points
        var lossVsEqual = Math.Abs(_calculator.CalculateRatingChange(1000, 1000, MatchOutcome.Loss));
        var lossVsWeak = Math.Abs(_calculator.CalculateRatingChange(1400, 1000, MatchOutcome.Loss));

        lossVsWeak.Should().BeGreaterThan(lossVsEqual);
    }

    #endregion

    #region New Ratings Tests

    [Fact]
    public void CalculateNewRatings_ShouldPreserveTotal()
    {
        // Zero-sum: total rating should be preserved
        var (new1, new2) = _calculator.CalculateNewRatings(1100, 900, MatchOutcome.Win);

        (new1 + new2).Should().Be(1100 + 900);
    }

    [Fact]
    public void CalculateNewRatings_Win_ShouldTransferPoints()
    {
        var (winner, loser) = _calculator.CalculateNewRatings(1000, 1000, MatchOutcome.Win);

        winner.Should().BeGreaterThan(1000);
        loser.Should().BeLessThan(1000);
        winner.Should().Be(1000 + (1000 - loser)); // Zero-sum
    }

    [Fact]
    public void CalculateNewRatings_Draw_EqualRatings_ShouldNotChange()
    {
        var (new1, new2) = _calculator.CalculateNewRatings(1000, 1000, MatchOutcome.Draw);

        new1.Should().Be(1000);
        new2.Should().Be(1000);
    }

    [Fact]
    public void CalculateNewRatings_Draw_UnequalRatings_ShouldConverge()
    {
        // Draw against stronger opponent = gain, draw against weaker = loss
        var (higher, lower) = _calculator.CalculateNewRatings(1200, 1000, MatchOutcome.Draw);

        higher.Should().BeLessThan(1200); // Higher rated loses points
        lower.Should().BeGreaterThan(1000); // Lower rated gains points
    }

    [Fact]
    public void CalculateNewRatings_ShouldRespectMinMax()
    {
        var calculator = new EloCalculator(kFactor: 32, initialRating: 1000, minRating: 100, maxRating: 3000);

        // Extreme ratings should be clamped
        var (new1, _) = calculator.CalculateNewRatings(150, 1500, MatchOutcome.Loss);

        new1.Should().BeGreaterThanOrEqualTo(100);
    }

    #endregion

    #region Match Result Update Tests

    [Fact]
    public void UpdateMatchWithEloChanges_ShouldSetChanges()
    {
        // Arrange
        var result = MatchResult.Win1(Guid.NewGuid(), Guid.NewGuid(), TimeSpan.FromMinutes(10));

        // Act
        _calculator.UpdateMatchWithEloChanges(result, 1000, 1000);

        // Assert
        result.EloChange1.Should().Be(16); // K/2 for win
        result.EloChange2.Should().Be(-16); // -K/2 for loss
    }

    [Fact]
    public void UpdateMatchWithEloChanges_Draw_ShouldHaveZeroChanges()
    {
        var result = MatchResult.Draw(Guid.NewGuid(), Guid.NewGuid(), TimeSpan.FromMinutes(10));

        _calculator.UpdateMatchWithEloChanges(result, 1000, 1000);

        result.EloChange1.Should().Be(0);
        result.EloChange2.Should().Be(0);
    }

    #endregion

    #region Win Probability Tests

    [Fact]
    public void CalculateWinProbability_EqualRatings_ShouldBe50Percent()
    {
        var prob = _calculator.CalculateWinProbability(1000, 1000);

        prob.Should().BeApproximately(0.5f, 0.001f);
    }

    [Fact]
    public void CalculateWinProbability_ShouldMatchExpectedScore()
    {
        var expected = _calculator.CalculateExpectedScore(1200, 1000);
        var probability = _calculator.CalculateWinProbability(1200, 1000);

        probability.Should().Be(expected);
    }

    #endregion

    #region Performance Rating Tests

    [Fact]
    public void CalculatePerformanceRating_AllWins_ShouldBeHigh()
    {
        var results = new List<(int OpponentRating, MatchOutcome Outcome)>
        {
            (1000, MatchOutcome.Win),
            (1100, MatchOutcome.Win),
            (1200, MatchOutcome.Win)
        };

        var performance = _calculator.CalculatePerformanceRating(results);

        performance.Should().BeGreaterThan(1200); // Better than all opponents
    }

    [Fact]
    public void CalculatePerformanceRating_AllLosses_ShouldBeLow()
    {
        var results = new List<(int OpponentRating, MatchOutcome Outcome)>
        {
            (1000, MatchOutcome.Loss),
            (1100, MatchOutcome.Loss),
            (1200, MatchOutcome.Loss)
        };

        var performance = _calculator.CalculatePerformanceRating(results);

        performance.Should().BeLessThan(1000); // Worse than all opponents
    }

    [Fact]
    public void CalculatePerformanceRating_50PercentScore_ShouldEqualAverage()
    {
        var results = new List<(int OpponentRating, MatchOutcome Outcome)>
        {
            (1000, MatchOutcome.Win),
            (1000, MatchOutcome.Loss),
            (1200, MatchOutcome.Win),
            (1200, MatchOutcome.Loss)
        };

        var performance = _calculator.CalculatePerformanceRating(results);

        // Should be close to average opponent rating (1100)
        performance.Should().BeInRange(1050, 1150);
    }

    [Fact]
    public void CalculatePerformanceRating_EmptyResults_ShouldReturnInitial()
    {
        var results = new List<(int OpponentRating, MatchOutcome Outcome)>();

        var performance = _calculator.CalculatePerformanceRating(results);

        performance.Should().Be(_calculator.InitialRating);
    }

    #endregion

    #region K-Factor Tests

    [Theory]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(64)]
    public void KFactor_ShouldAffectRatingVolatility(int kFactor)
    {
        var calculator = new EloCalculator(kFactor: kFactor);

        var change = calculator.CalculateRatingChange(1000, 1000, MatchOutcome.Win);

        change.Should().Be(kFactor / 2);
    }

    #endregion

    #region Constructor Validation Tests

    [Fact]
    public void Constructor_InvalidKFactor_ShouldThrow()
    {
        var act = () => new EloCalculator(kFactor: 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_InitialRatingOutOfRange_ShouldThrow()
    {
        var act = () => new EloCalculator(initialRating: 50, minRating: 100, maxRating: 3000);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_MinGreaterThanMax_ShouldThrow()
    {
        var act = () => new EloCalculator(minRating: 2000, maxRating: 1000);

        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Games To Reach Tests

    [Fact]
    public void EstimateGamesToReach_AlreadyAtTarget_ShouldReturnZero()
    {
        var games = _calculator.EstimateGamesToReach(1500, 1500, 1000);

        games.Should().Be(0);
    }

    [Fact]
    public void EstimateGamesToReach_AboveTarget_ShouldReturnZero()
    {
        var games = _calculator.EstimateGamesToReach(1600, 1500, 1000);

        games.Should().Be(0);
    }

    [Fact]
    public void EstimateGamesToReach_ShouldReturnPositiveCount()
    {
        var games = _calculator.EstimateGamesToReach(1000, 1200, 1000);

        games.Should().BePositive();
    }

    #endregion
}
