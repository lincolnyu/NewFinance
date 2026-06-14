using NewFinance.Common;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class Deductible(SteadyFlowDescriptor descriptor, Account cashAccount) : SteadyFlow(descriptor, cashAccount, "Deductible");
}