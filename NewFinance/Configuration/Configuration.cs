using NewFinance.Concrete.Entities;
using NewFinance.Core;

namespace NewFinance.Configuration
{
    public class Configuration
    {
        public List<TaxIndividual> TaxIndividuals { get; } = [];

        public List<Family> Families { get; } = [];
        
        public List<Account> Accounts {get;} = [];

        public List<Contract> ExistingContracts {get;} = [];

        public List<(Contract, bool)> OptionalContracts {get;} = [];
    }
}