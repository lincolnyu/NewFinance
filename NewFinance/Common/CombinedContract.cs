using NewFinance.Core;

namespace NewFinance.Common
{
    public abstract class CombinedContract(DateTime? startTime, string name) : Contract(startTime, name)
    {
        public abstract IEnumerable<Contract> ChildContracts { get; }

        protected override (DateTime processedTime, DateTime? bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            var bookedTime = executor.ExecuteContracts(ChildContracts, currentTime);
            return (currentTime, bookedTime);
        }
    }
}