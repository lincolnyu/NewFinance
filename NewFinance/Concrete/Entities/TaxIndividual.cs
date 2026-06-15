using NewFinance.Concrete.Contracts;
using NewFinance.Core;

namespace NewFinance.Concrete.Entities
{
    public class TaxIndividual : Entity
    {
        public IndividualTax? Tax { get; set;}

        /// <summary>
        ///  The list of individual contracts that are only relevant for tax calculation and calculate at the end of the financial year (EOFY).
        ///  For example 
        ///   - personal salary
        ///   - personal deductible expenses
        ///   - super contributions
        /// </summary>
        public List<Contract> TaxableContracts { get; } = new List<Contract>();

        public Family? Family { get; set; }
    }
}