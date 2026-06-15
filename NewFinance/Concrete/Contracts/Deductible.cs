using NewFinance.Common;
using NewFinance.Concrete.Entities;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class Deductible(SteadyFlowDescriptor descriptor, TaxIndividual individual, Account cashAccount, string name) : SteadyFlow(descriptor, cashAccount, name)
    {
        TaxIndividual Individual { get; } = individual;
    }
}