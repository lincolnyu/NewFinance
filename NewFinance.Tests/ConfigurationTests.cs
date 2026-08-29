namespace NewFinance.Tests;

using NewFinance.Concrete;
using NewFinance.Concrete.Entities;
using NewFinance.Configuration;
using NewFinance.Core;

public class ConfigurationTests
{
    [Fact]
    public void SerializeAndDeserialize()
    {
        var per1 = new TaxIndividual{Name="Person1"};
        var per2 = new TaxIndividual{Name="Person2"};
        var family = new Family{Name="MyFamily"};
        family.AddTaxMember(per1);
        family.AddTaxMember(per2);

        var account = new Account("bank", 10000);
        account.Ownership.Add((per1, 0.5m));
        account.Ownership.Add((per2, 0.5m));

        var testConfig = new Configuration();
        testConfig.Families.Add(family);
        testConfig.TaxIndividuals.Add(per1);
        testConfig.TaxIndividuals.Add(per2);
        testConfig.Accounts.Add(account);

        testConfig.SaveToFile("test.json");
        var testLoadingConfig = SerializationHelper.LoadFromFile("test.json");

        Assert.NotNull(testLoadingConfig);
        Assert.NotNull(testLoadingConfig.Families);
        Assert.NotNull(testLoadingConfig.Accounts);
        Assert.Equal(2, testLoadingConfig.TaxIndividuals.Count);
        Assert.Single(testLoadingConfig.Families);
        Assert.Single(testLoadingConfig.Accounts);

        return;
    }
}