using NewFinance.Common;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class Employment(SteadyFlowDescriptor descriptor, Account cashAccount) : SteadyFlow(descriptor, cashAccount);
}