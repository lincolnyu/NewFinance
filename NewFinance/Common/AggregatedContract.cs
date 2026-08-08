using NewFinance.Core;

namespace NewFinance.Common
{
    public class AggregatedContract(DateTime? startTime, string name, IEnumerable<Contract>? childContracts = null) : Contract(startTime, name)
    {
        public virtual IEnumerable<Contract> ChildContracts { get; } = childContracts ??[];

        protected override (DateTime processedTime, DateTime? bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            var bookedTime = executor.ExecuteContracts(ChildContracts, currentTime);
            return (currentTime, bookedTime);
        }
    }
}