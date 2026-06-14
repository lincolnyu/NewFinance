namespace NewFinance.Tests;

using NewFinance.Concrete.Contracts;
using NewFinance.Concrete.Entities;
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
    [InlineData(27_000, 100)]
    [InlineData(32_500, 650)]
    [InlineData(100_000, 2_000)]
    public void CalculateMedicareLevy_AppliesLowIncomePhaseIn(decimal taxableIncome, decimal expectedLevy)
    {
        var levy = IndividualTax.CalculateMedicareLevy(taxableIncome);

        Assert.Equal(expectedLevy, levy);
    }

    [Fact]
    public void EstimatePaygWithholding_AnnualisesPeriodIncome()
    {
        var withholding = Employment.EstimatePaygWithholding(10_000m, TimeSpan.FromDays((double)Constants.DaysPerYear / 10));

        Assert.Equal(2_278.80m, withholding);
    }

    [Fact]
    public void Execute_CreditsPaygWithholdingAgainstFinalTaxAssessment()
    {
        var taxpayer = new TaxIndividual();
        var cash = new Account("Cash");
        var employment = new Employment(
            FlowHelpers.ConstantFlowDescriptor(new DateTime(2025, 1, 1), 0m),
            cash);
        var tax = new IndividualTax(taxpayer, cash);

        taxpayer.TaxableContracts.Add(employment);
        employment.InflowTracker.TrackChange(100_000m);
        employment.PaygWithheldTracker.TrackChange(25_000m);

        tax.Execute(new ContractExecutor(), new DateTime(2025, 6, 30));

        Assert.Equal(2_212m, cash.Balance);
        Assert.Equal(-2_212m, tax.TaxPaid.TotalChange);
    }
}
