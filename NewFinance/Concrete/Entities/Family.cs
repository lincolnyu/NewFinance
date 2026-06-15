using NewFinance.Core;

namespace NewFinance.Concrete.Entities
{
    public class Family : Entity
    {
        public List<TaxIndividual> TaxMembers { get; } = new List<TaxIndividual>();

        public int DependencyCount {get;set;}
    }
}