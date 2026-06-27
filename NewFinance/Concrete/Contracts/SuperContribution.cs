using NewFinance.Common;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class SuperContribution(BandedFlowDescriptor descriptor, Account cashAccount) : BandedFlow(descriptor, cashAccount, "Super Contribution")
    {
    }
}