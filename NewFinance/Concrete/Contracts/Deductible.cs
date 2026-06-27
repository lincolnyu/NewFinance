using NewFinance.Common;
using NewFinance.Concrete.Entities;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class Deductible(BandedFlowDescriptor descriptor, TaxIndividual individual, Account cashAccount, string name) : BandedFlow(descriptor, cashAccount, name)
    {
        TaxIndividual Individual { get; } = individual;
    }
}