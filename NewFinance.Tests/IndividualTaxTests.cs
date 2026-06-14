namespace NewFinance.Tests;

using NewFinance.Concrete.Contracts;

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
}
