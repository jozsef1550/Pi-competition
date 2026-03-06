using PiSearch.Core.Services;

namespace PiSearch.Tests;

public sealed class StatisticsServiceTests
{
    [Fact]
    public void ComputeProbability_ReturnsZero_ForZeroDigits()
    {
        Assert.Equal(0.0, StatisticsService.ComputeProbability(0, 5));
    }

    [Fact]
    public void ComputeProbability_ReturnsZero_ForZeroLength()
    {
        Assert.Equal(0.0, StatisticsService.ComputeProbability(1_000_000, 0));
    }

    [Fact]
    public void ComputeProbability_IncreasesWithMoreDigits()
    {
        double p1 = StatisticsService.ComputeProbability(1_000, 5);
        double p2 = StatisticsService.ComputeProbability(1_000_000, 5);
        Assert.True(p2 > p1);
    }

    [Fact]
    public void ComputeProbability_DecreasesWithLongerPattern()
    {
        double pShort = StatisticsService.ComputeProbability(1_000_000, 3);
        double pLong  = StatisticsService.ComputeProbability(1_000_000, 10);
        Assert.True(pLong < pShort);
    }

    [Fact]
    public void ComputeProbability_ApproachesOne_ForSmallPatternAndManyDigits()
    {
        double p = StatisticsService.ComputeProbability(1_000_000, 1);
        Assert.True(p > 0.999);
    }

    [Fact]
    public void ComputeProbability_StaysBelow1()
    {
        double p = StatisticsService.ComputeProbability(long.MaxValue / 2, 1);
        Assert.True(p <= 1.0);
    }

    [Fact]
    public void Sample_ReturnsStatisticsWithCorrectIndex()
    {
        var svc = new StatisticsService();
        svc.Start();
        var snap = svc.Sample(12345, 5);
        Assert.Equal(12345, snap.CurrentIndex);
        Assert.Equal(5, snap.PatternLength);
        svc.Stop();
    }

    [Fact]
    public void Sample_ElapsedTime_IsNonNegative()
    {
        var svc = new StatisticsService();
        svc.Start();
        var snap = svc.Sample(1000, 10);
        Assert.True(snap.ElapsedTime >= TimeSpan.Zero);
        svc.Stop();
    }
}
