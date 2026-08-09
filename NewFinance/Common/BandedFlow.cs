using NewFinance.Core;

namespace NewFinance.Common
{
    public class BandedFlow(BandedFlowDescriptor descriptor, Account account, string name) : AccountBindingContract(descriptor.StartTime, account, name)
    {
        public ITrackerKey? InflowTrackerKey { get; set; }

        public int CurrentInflowIndex { get; private set; } = -1;

        public DateTime NextFlowChangeUpdateDate {get; private set;}

        protected TimeSpan? FlowBookingInterval { get; set; }

        protected override (DateTime processedTime, DateTime? bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            // If there is a burst at the current time, apply the burst first before applying the steady flow logic, and move to the next burst.
            if (currentTime == StartTime)
            {
                CurrentInflowIndex = 0;
                NextFlowChangeUpdateDate = descriptor.Inflows[CurrentInflowIndex].EndTime; // currentTime.NextAnniversayCrossing(descriptor.YearlyFlowChangeUpdateMonth, descriptor.YearlyFlowChangeUpdateDay);
            }
            else
            {
                var dailyRateInBucket = descriptor.Inflows[CurrentInflowIndex].DailyRate;
                var executionTimeSpan = (currentTime - lastProcessedTime)!.Value;

                var inflow = dailyRateInBucket * (decimal)executionTimeSpan.TotalDays; // pro-rate the inflow by the fraction of the time span that has passed in the current bucket
                ApplyInflow(executor, inflow, executionTimeSpan);

                if (currentTime == NextFlowChangeUpdateDate)
                {
                    CurrentInflowIndex++;
                    if (CurrentInflowIndex < descriptor.Inflows.Count)
                    {
                        NextFlowChangeUpdateDate = descriptor.Inflows[CurrentInflowIndex].EndTime;
                    }
                    else
                    {
                        // No more flow change, set the next update date to a far future date.
                        NextFlowChangeUpdateDate = DateTime.MaxValue;
                        CurrentInflowIndex = descriptor.Inflows.Count - 1; // keep it at the last bucket
                    }
                }
                else
                {
                    System.Diagnostics.Debug.Assert(currentTime < NextFlowChangeUpdateDate);
                    // Keep booking the next raise update time until the current time reaches it.
                }
            }

            var nextBookedTime = GetNextBookedTime(currentTime);

            return (currentTime, nextBookedTime);
        }

        protected virtual void ApplyInflow(ContractExecutor executor, decimal inflow, TimeSpan executionTimeSpan)
        {
            executor.ExecuteTransaction(Account!, inflow, this, $"Inflow for {Name}");
            if (InflowTrackerKey is not null)
            {
                executor.ChangeTrackers?[InflowTrackerKey].TrackChange(inflow);
            }
        }

        private DateTime GetNextBookedTime(DateTime currentTime)
        {
            if (FlowBookingInterval is not { } interval)
            {
                return NextFlowChangeUpdateDate;
            }

            var nextFrequencyUpdateDate = currentTime.Add(interval);
            return nextFrequencyUpdateDate < NextFlowChangeUpdateDate ? nextFrequencyUpdateDate : NextFlowChangeUpdateDate;
        }
    }
}
