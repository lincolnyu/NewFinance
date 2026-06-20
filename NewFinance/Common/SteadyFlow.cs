using NewFinance.Core;

namespace NewFinance.Common
{
    public class SteadyFlow(SteadyFlowDescriptor descriptor, Account account, string name) : AccountBindingContract(descriptor.StartTime, account, name)
    {
        public int CurrentInflowIndex { get; private set; } = -1;

        public DateTime NextFlowChangeUpdateDate {get; private set;}

        public ChangeTracker InflowTracker {get; private set;} = new ChangeTracker();

        protected TimeSpan? FlowBookingInterval { get; set; }

        public List<(DateTime Time, decimal Amount)> Bursts { get; } = new List<(DateTime, decimal)>();

        public int BurstIndex { get; private set; } = 0;

        public override void Reset(ContractExecutor executor)
        {
            base.Reset(executor);
            InflowTracker.ResetAll();
            BurstIndex = 0;
            CurrentInflowIndex = -1;
        }

        protected override (DateTime processedTime, DateTime? bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            // If there is a burst at the current time, apply the burst first before applying the steady flow logic, and move to the next burst.
            if (currentTime == StartTime)
            {
                BurstIndex = 0;
                CurrentInflowIndex = 0;
                NextFlowChangeUpdateDate = descriptor.Inflows[CurrentInflowIndex].EndTime; // currentTime.NextAnniversayCrossing(descriptor.YearlyFlowChangeUpdateMonth, descriptor.YearlyFlowChangeUpdateDay);
                InflowTracker.ResetAll();
            }
            else
            {
                var dailyRateInBucket = descriptor.Inflows[CurrentInflowIndex].DailyRate;
                var executionTimeSpan = (currentTime - lastProcessedTime)!.Value;

                var inflow = dailyRateInBucket * (decimal)executionTimeSpan.TotalDays; // pro-rate the inflow by the fraction of the time span that has passed in the current bucket
                ApplyInflow(inflow, executionTimeSpan);

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

            var nextBurstTime = BurstIndex < Bursts.Count ? Bursts[BurstIndex].Time : (DateTime?)null;
            if (nextBurstTime == currentTime)
            {
                var burstAmount = Bursts[BurstIndex].Amount;
                Account!.Balance += burstAmount;
                InflowTracker.TrackChange(burstAmount);
                BurstIndex++;

                nextBurstTime = BurstIndex < Bursts.Count ? Bursts[BurstIndex].Time : (DateTime?)null;
                if (nextBurstTime is not null && nextBurstTime < nextBookedTime)
                {
                    nextBookedTime = nextBurstTime.Value;
                }
            }

            return (currentTime, nextBookedTime);
        }

        protected virtual void ApplyInflow(decimal inflow, TimeSpan executionTimeSpan)
        {
            Account!.Balance += inflow;
            InflowTracker.TrackChange(inflow);
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
