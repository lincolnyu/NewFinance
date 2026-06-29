
using NewFinance.Core;

namespace NewFinance.Common
{
    public class ContextualContract(DateTime startTime, string name, object context, Func<object, ContractExecutor, DateTime?, DateTime?, DateTime, (DateTime, DateTime?)> executeFunc) : Contract(startTime, name)
    {
        protected override (DateTime processedTime, DateTime? bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            return executeFunc(context, executor, lastProcessedTime, lastBookedTime, currentTime);
        }
    }
}