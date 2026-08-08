using NewFinance.Core;

namespace NewFinance.Common
{
    public class Bursts(DateTime startTime, Account account, string name) : AccountBindingContract(startTime, account, name)
    {
        public ITrackerKey? InflowTrackerKey { get; init; }

        public List<(DateTime Time, decimal Amount)> BurstsList { get; } = new List<(DateTime, decimal)>();

        public int BurstIndex { get; private set; } = 0;

        protected override (DateTime processedTime, DateTime? bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            if (currentTime == StartTime)
            {
                BurstIndex = 0;
            }

            var nextBurstTime = BurstIndex < BurstsList.Count ? BurstsList[BurstIndex].Time : (DateTime?)null;
            if (nextBurstTime == currentTime)
            {
                var burstAmount = BurstsList[BurstIndex].Amount;
                executor.ExecuteTransaction(Account!, burstAmount, this, $"Burst at {currentTime} for {Name}");
                if (InflowTrackerKey is not null)
                {
                    executor.ChangeTrackers?[InflowTrackerKey].TrackChange(burstAmount);
                }
                BurstIndex++;

                nextBurstTime = BurstIndex < BurstsList.Count ? BurstsList[BurstIndex].Time : null;
            }

            return (currentTime, nextBurstTime);
        }
    }
}