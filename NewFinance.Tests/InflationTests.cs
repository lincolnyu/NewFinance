namespace NewFinance.Tests;

using NewFinance.Concrete.Contracts;

public class InflationTests
{
    private const decimal Precision = 0.0000000001m;

    [Fact]
    public void GetRelativeInflationFactor_WhenToTimeIsBeforeInflationStart_ReturnsOne()
    {
        var inflationStart = new DateTime(2025, 1, 1);
        var inflation = FlowHelpers.ConstantInflation(inflationStart, 0.10m);

        var factor = inflation.GetRelativeInflationFactor(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31));

        Assert.Equal(1m, factor);
    }

    [Fact]
    public void GetRelativeInflationFactor_ForOneFullYear_CompoundsAnnualRateOnce()
    {
        var inflationStart = new DateTime(2025, 1, 1);
        var inflation = FlowHelpers.ConstantInflation(inflationStart, 0.10m);

        var factor = inflation.GetRelativeInflationFactor(
            inflationStart,
            inflationStart.AddDays((double)Constants.DaysPerYear));

        AssertClose(1.10m, factor);
    }

    [Fact]
    public void GetRelativeInflationFactor_WhenRateChangesDuringPeriod_AppliesEachRateForItsOwnDuration()
    {
        var inflationStart = new DateTime(2025, 1, 1);
        var rateChangeTime = inflationStart.AddDays((double)Constants.DaysPerYear / 2);
        var inflation = new Inflation(
            inflationStart,
            [(0.10m, rateChangeTime), (0.20m, DateTime.MaxValue)]);

        var factor = inflation.GetRelativeInflationFactor(
            inflationStart,
            inflationStart.AddDays((double)Constants.DaysPerYear));

        var expected = (decimal)(Math.Pow(1.10, 0.5) * Math.Pow(1.20, 0.5));
        AssertClose(expected, factor);
    }

    [Fact]
    public void ApplyInflation_WhenFlowStartsBeforeInflationStart_KeepsInitialRateUntilInflationStarts()
    {
        var flowStart = new DateTime(2024, 1, 1);
        var inflationStart = new DateTime(2025, 1, 1);
        var firstReview = inflationStart.AddDays((double)Constants.DaysPerYear);
        var inflation = FlowHelpers.ConstantInflation(inflationStart, 0.10m);

        var descriptor = inflation.ApplyInflation(flowStart, 100m, [firstReview]);

        Assert.Equal(flowStart, descriptor.StartTime);
        Assert.Equal(3, descriptor.Inflows.Count);
        Assert.Equal((100m, inflationStart), descriptor.Inflows[0]);
        Assert.Equal((100m, firstReview), descriptor.Inflows[1]);
        Assert.Equal(DateTime.MaxValue, descriptor.Inflows[2].EndTime);
        AssertClose(110m, descriptor.Inflows[2].DailyRate);
    }

    [Fact]
    public void ApplyInflation_WithFiniteReviewDates_AppendsTerminalInflationBucket()
    {
        var inflationStart = new DateTime(2025, 1, 1);
        var firstReview = inflationStart.AddDays((double)Constants.DaysPerYear);
        var inflation = FlowHelpers.ConstantInflation(inflationStart, 0.10m);

        var descriptor = inflation.ApplyInflation(inflationStart, 100m, [firstReview]);

        Assert.Equal(2, descriptor.Inflows.Count);
        Assert.Equal((100m, firstReview), descriptor.Inflows[0]);
        Assert.Equal(DateTime.MaxValue, descriptor.Inflows[1].EndTime);
        AssertClose(110m, descriptor.Inflows[1].DailyRate);
    }

    [Fact]
    public void ApplyInflation_WhenRateChangesBeforeReview_UsesCombinedRelativeInflationFactor()
    {
        var inflationStart = new DateTime(2025, 1, 1);
        var rateChangeTime = inflationStart.AddDays((double)Constants.DaysPerYear / 2);
        var firstReview = inflationStart.AddDays((double)Constants.DaysPerYear);
        var inflation = new Inflation(
            inflationStart,
            [(0.10m, rateChangeTime), (0.20m, DateTime.MaxValue)]);

        var descriptor = inflation.ApplyInflation(inflationStart, 100m, [firstReview]);

        var expectedRate = 100m * (decimal)(Math.Pow(1.10, 0.5) * Math.Pow(1.20, 0.5));
        Assert.Equal(2, descriptor.Inflows.Count);
        Assert.Equal((100m, firstReview), descriptor.Inflows[0]);
        Assert.Equal(DateTime.MaxValue, descriptor.Inflows[1].EndTime);
        AssertClose(expectedRate, descriptor.Inflows[1].DailyRate);
    }

    private static void AssertClose(decimal expected, decimal actual)
    {
        Assert.InRange(actual, expected - Precision, expected + Precision);
    }
}
