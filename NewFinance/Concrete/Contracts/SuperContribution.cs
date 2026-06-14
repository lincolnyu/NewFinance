using NewFinance.Common;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class SuperContribution(SteadyFlowDescriptor descriptor, Account cashAccount) : SteadyFlow(descriptor, cashAccount, "Super Contribution")
    {
    }
}