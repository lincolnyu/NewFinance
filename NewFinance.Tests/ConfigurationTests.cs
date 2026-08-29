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
        account.Ownership.Add(new OwnershipShare{Entity = per1, Share = 0.5m});
        account.Ownership.Add(new OwnershipShare{Entity = per2, Share = 0.5m});

        var testConfig = new Configuration();
        testConfig.Families.Add(family);
        testConfig.TaxIndividuals.Add(per1);
        testConfig.TaxIndividuals.Add(per2);
        testConfig.Accounts.Add(account);

        testConfig.SaveToFile("test.json");
        var loadedConfig = SerializationHelper.LoadFromFile("test.json");
        File.Delete("test.json");

        Assert.NotNull(loadedConfig);
        Assert.NotNull(loadedConfig.Families);
        Assert.NotNull(loadedConfig.Accounts);

        Assert.Single(loadedConfig.Families);
        Assert.Single(loadedConfig.Accounts);
        Assert.Equal(2, loadedConfig.TaxIndividuals.Count);
        Assert.Equal(2, loadedConfig.Families[0].TaxMembers.Count);

        Assert.Same(loadedConfig.TaxIndividuals[0], loadedConfig.Families[0].TaxMembers[0]);
        Assert.Same(loadedConfig.Families[0], loadedConfig.TaxIndividuals[0].Family);

        Assert.Equal("Person1", loadedConfig.TaxIndividuals[0].Name);
        Assert.Equal("Person2", loadedConfig.TaxIndividuals[1].Name);

        Assert.Equal(2, loadedConfig.Accounts[0].Ownership.Count);
        Assert.Same(loadedConfig.TaxIndividuals[0], loadedConfig.Accounts[0].Ownership[0].Entity);
        Assert.Equal(0.5m, loadedConfig.Accounts[0].Ownership[0].Share);
        Assert.Equal(10000m, loadedConfig.Accounts[0].Balance);
        Assert.Same(loadedConfig.TaxIndividuals[1], loadedConfig.Families[0].TaxMembers[1]);

        return;
    }
}