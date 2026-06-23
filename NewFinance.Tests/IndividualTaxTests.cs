namespace NewFinance.Tests;

using NewFinance.Concrete.Contracts;
using NewFinance.Concrete.Entities;
using NewFinance.Concrete.Rules;
using NewFinance.Core;

public class IndividualTaxTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(18_200, 0)]
    [InlineData(45_000, 4_288)]
    [InlineData(135_000, 31_288)]
    [InlineData(190_000, 51_638)]
    [InlineData(200_000, 56_138)]
    public void CalculateResidentIncomeTax_UsesMarginalResidentRates(decimal taxableIncome, decimal expectedTax)
    {
        var tax = IndividualTax.CalculateResidentIncomeTax(taxableIncome);

        Assert.Equal(expectedTax, tax);
    }

    [Theory]
    [InlineData(26_000, 0)]
    [InlineData(28_000, 77.80)]
    [InlineData(32_500, 527.80)]
    [InlineData(100_000, 2_000)]
    public void CalculateMedicareLevyFY26_AppliesLowIncomePhaseIn(decimal taxableIncome, decimal expectedLevy)
    {
        var levy = new MedicareLevyRules().CalculateFY26(taxableIncome, null);

        Assert.Equal(expectedLevy, levy);
    }

    [Fact]
    public void EstimatePaygWithholding_AnnualisesPeriodIncome()
    {
        var withholding = Employment.EstimatePaygWithholding(10_000m, TimeSpan.FromDays((double)Constants.DaysPerYear / 10), null);

        Assert.Equal(2_278.80m, withholding);
    }

    [Fact]
    public void Execute_CreditsPaygWithholdingAgainstFinalTaxAssessment()
    {
        var taxpayer = new TaxIndividual();
        var cash = new Account("Cash");
        var employment = new Employment(
            FlowHelpers.ConstantFlowDescriptor(new DateTime(2025, 1, 1), 0m),
            taxpayer,
            cash);
        var tax = new IndividualTax(taxpayer, cash);

        var executor = new ContractExecutor();
        var changeTrackers  = new ChangeTrackers();

        taxpayer.TaxableContracts.Add(employment);
        changeTrackers.GetOrCreateTracker(employment, Common.SteadyFlow.ChangeTrackerInflow).TrackChange(100_000m);
        changeTrackers.GetOrCreateTracker(employment, Employment.ChangeTrackerPaygWithheld).TrackChange(25_000m);
        
        executor.Contracts.Add(tax);
        executor.ChangeTrackers = changeTrackers;
        executor.Execute(new DateTime(2025, 6, 30));

        Assert.Equal(2_212m, cash.Balance);
        
        Assert.Equal(-22_788m, changeTrackers.GetOrCreateTracker(tax, IndividualTax.ChangeTrackerTaxPaid).TotalChange);
    }
}
