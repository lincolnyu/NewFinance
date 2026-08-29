using NewFinance.Concrete.Entities;
using NewFinance.Core;

namespace NewFinance.Configuration
{
    public class Configuration
    {
        public List<TaxIndividual> TaxIndividuals { get; set; } = [];

        public List<Family> Families { get; set; } = [];
        
        public List<Account> Accounts { get; set; } = [];

        public List<Contract> ExistingContracts { get; set; } = [];

        public List<(Contract, bool)> OptionalContracts { get; set; } = [];
    }
}