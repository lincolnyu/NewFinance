using NewFinance.Concrete.Contracts;

namespace NewFinance.Tests;
public class InflationFactoryTests
{
    [Fact]
    public void StartsBeforeInflationStart_InitialRateAppliesUntilInflationStart_ThenFirstInflationApplies()
    {
        var flowStart = new DateTime(2026, 1, 1);
        var inflationStart = new DateTime(2026, 1, 11);
        var next = new DateTime(2026, 1, 21);

        var descriptor = new Inflation(inflationStart, [(0.10m*Constants.daysPerYear/(decimal)(next - inflationStart).TotalDays, next)]).ApplyInflationPreciseMatching(
            flowStart,
            100m);

        Assert.Equal(flowStart, descriptor.StartTime);
        Assert.Equal(3, descriptor.Inflows.Count);

        Assert.Equal(100m, descriptor.Inflows[0].Rate);
        Assert.Equal(inflationStart, descriptor.Inflows[0].EndTime);

        Assert.Equal(110m, descriptor.Inflows[1].Rate, 4);
        Assert.Equal(next, descriptor.Inflows[1].EndTime);

        Assert.Equal(110m, descriptor.Inflows[2].Rate, 4);
        Assert.Equal(DateTime.MaxValue, descriptor.Inflows[2].EndTime);
    }

    [Fact]
    public void StartsAtInflationStart_DoesNotApplyInflationImmediately_InitialRateAppliesUntilFirstFutureHit()
    {
        var flowStart = new DateTime(2026, 1, 1);
        var inflationStart = flowStart;
        var next = new DateTime(2026, 1, 11);
        var following = new DateTime(2026, 1, 21);

        var descriptor = new Inflation(inflationStart, [(0.10m*Constants.daysPerYear/(decimal)(next-inflationStart).TotalDays, next), (0.20m*Constants.daysPerYear/(decimal)(following-next).TotalDays, following)]).ApplyInflationPreciseMatching(
            flowStart,
            100m);

        Assert.Equal(3, descriptor.Inflows.Count);

        Assert.Equal(100m, descriptor.Inflows[0].Rate);
        Assert.Equal(next, descriptor.Inflows[0].EndTime);

        Assert.Equal(120m, descriptor.Inflows[1].Rate, 4);
        Assert.Equal(following, descriptor.Inflows[1].EndTime);

        Assert.Equal(120m, descriptor.Inflows[2].Rate, 4);
        Assert.Equal(DateTime.MaxValue, descriptor.Inflows[2].EndTime);
    }

    [Fact]
    public void MultipleInflationRates_AdvancesCurrentTimeBetweenBuckets()
    {
        var flowStart = new DateTime(2026, 1, 1);
        var inflationStart = new DateTime(2026, 1, 11);
        var firstEnd = new DateTime(2026, 1, 21);
        var secondEnd = new DateTime(2026, 1, 31);

        var descriptor = new Inflation(inflationStart, [(0.10m*Constants.daysPerYear/(decimal)(firstEnd-inflationStart).TotalDays, firstEnd), (0.20m*Constants.daysPerYear/(decimal)(secondEnd-firstEnd).TotalDays, secondEnd)]).ApplyInflationPreciseMatching(
            flowStart,
            100m);

        Assert.Equal(4, descriptor.Inflows.Count);

        Assert.Equal(100m, descriptor.Inflows[0].Rate);
        Assert.Equal(inflationStart, descriptor.Inflows[0].EndTime);

        Assert.Equal(110m, descriptor.Inflows[1].Rate, 4);
        Assert.Equal(firstEnd, descriptor.Inflows[1].EndTime);

        Assert.Equal(132m, descriptor.Inflows[2].Rate, 4);
        Assert.Equal(secondEnd, descriptor.Inflows[2].EndTime);

        Assert.Equal(132m, descriptor.Inflows[3].Rate, 4);
        Assert.Equal(DateTime.MaxValue, descriptor.Inflows[3].EndTime);
    }

    [Fact]
    public void InflationPointAtFlowStart_IsIgnoredUntilNextFutureHit()
    {
        var flowStart = new DateTime(2026, 1, 11);
        var inflationStart = new DateTime(2026, 1, 1);
        var pointAtFlowStart = flowStart;
        var next = new DateTime(2026, 1, 21);

        var descriptor = new Inflation(inflationStart, [(0.10m*Constants.daysPerYear/(decimal)(pointAtFlowStart-inflationStart).TotalDays, pointAtFlowStart), (0.20m*Constants.daysPerYear/(decimal)(next-pointAtFlowStart).TotalDays, next)]).ApplyInflationPreciseMatching(
            flowStart,
            100m);

        Assert.Equal(2, descriptor.Inflows.Count);

        Assert.Equal(100m, descriptor.Inflows[0].Rate);
        Assert.Equal(next, descriptor.Inflows[0].EndTime);

        Assert.Equal(100m, descriptor.Inflows[1].Rate);
        Assert.Equal(DateTime.MaxValue, descriptor.Inflows[1].EndTime);
    }

    [Fact]
    public void StartsAfterInflationStartBeforeFirstRateHit_InitialRateAppliesUntilFirstHit_ThenFirstInflationApplies()
    {
        var flowStart = new DateTime(2026, 1, 5);
        var inflationStart = new DateTime(2026, 1, 1);
        var firstHit = new DateTime(2026, 1, 11);
        var secondHit = new DateTime(2026, 1, 21);

        var descriptor = new Inflation(inflationStart, [(0.10m*Constants.daysPerYear/(decimal)(firstHit-inflationStart).TotalDays, firstHit), (0.20m*Constants.daysPerYear/(decimal)(secondHit-firstHit).TotalDays, secondHit)]).ApplyInflationPreciseMatching(
            flowStart,
            100m);

        Assert.Equal(flowStart, descriptor.StartTime);
        Assert.Equal(3, descriptor.Inflows.Count);

        Assert.Equal(100m, descriptor.Inflows[0].Rate);
        Assert.Equal(firstHit, descriptor.Inflows[0].EndTime);

        Assert.Equal(120m, descriptor.Inflows[1].Rate, 4);
        Assert.Equal(secondHit, descriptor.Inflows[1].EndTime);

        Assert.Equal(120m, descriptor.Inflows[2].Rate, 4);
        Assert.Equal(DateTime.MaxValue, descriptor.Inflows[2].EndTime);
    }

    [Fact]
    public void StartsExactlyAtInflationStart_DoesNotApplyFirstInflationUntilFirstFutureRateHit()
    {
        var flowStart = new DateTime(2026, 1, 1);
        var inflationStart = flowStart;
        var firstHit = new DateTime(2026, 1, 11);
        var secondHit = new DateTime(2026, 1, 21);

        var descriptor = new Inflation(inflationStart, [(0.10m*Constants.daysPerYear/(decimal)(firstHit-inflationStart).TotalDays, firstHit), (0.20m*Constants.daysPerYear/(decimal)(secondHit-firstHit).TotalDays, secondHit)]).ApplyInflationPreciseMatching(
            flowStart,
            100m);

        Assert.Equal(flowStart, descriptor.StartTime);
        Assert.Equal(3, descriptor.Inflows.Count);

        // Because flowStartTime is exactly on inflationStartTime, the flow starts with initialRate.
        Assert.Equal(100m, descriptor.Inflows[0].Rate);
        Assert.Equal(firstHit, descriptor.Inflows[0].EndTime);

        // After firstHit is reached, the next interval uses the next listed inflation value.
        Assert.Equal(120m, descriptor.Inflows[1].Rate, 4);
        Assert.Equal(secondHit, descriptor.Inflows[1].EndTime);

        Assert.Equal(120m, descriptor.Inflows[2].Rate, 4);
        Assert.Equal(DateTime.MaxValue, descriptor.Inflows[2].EndTime);
    }

    [Fact]
    public void NoFutureInflationHits_ReturnsOneOpenEndedBucketAtInitialRate()
    {
        var flowStart = new DateTime(2026, 1, 11);
        var inflationStart = new DateTime(2026, 1, 1);

        var descriptor = new Inflation(inflationStart, []).ApplyInflationPreciseMatching(
            flowStart,
            100m);

        Assert.Equal(flowStart, descriptor.StartTime);
        Assert.Single(descriptor.Inflows);

        Assert.Equal(100m, descriptor.Inflows[0].Rate);
        Assert.Equal(DateTime.MaxValue, descriptor.Inflows[0].EndTime);
    }

    [Fact]
    public void OnlyPastInflationHits_ReturnsOneOpenEndedBucketAtInitialRate()
    {
        var flowStart = new DateTime(2026, 1, 21);
        var inflationStart = new DateTime(2026, 1, 1);

        var descriptor = new Inflation(inflationStart, [(0.10m*Constants.daysPerYear/(decimal)(new DateTime(2026, 1, 11)-inflationStart).TotalDays, new DateTime(2026, 1, 11)), (0.20m*Constants.daysPerYear/(decimal)(flowStart-new DateTime(2026, 1, 11)).TotalDays, flowStart)]).ApplyInflationPreciseMatching(
            flowStart,
            100m);

        Assert.Equal(flowStart, descriptor.StartTime);
        Assert.Single(descriptor.Inflows);

        Assert.Equal(100m, descriptor.Inflows[0].Rate);
        Assert.Equal(DateTime.MaxValue, descriptor.Inflows[0].EndTime);
    }
}

