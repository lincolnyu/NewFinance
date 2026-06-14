using NewFinance.Core;

namespace NewFinance.Common
{
    public class SteadyFlow(SteadyFlowDescriptor descriptor, Account account, string name) : AccountBindingContract(descriptor.StartTime, account, name)
    {
        public int CurrentInflowIndex { get; private set; } = -1;

        public DateTime NextFlowChangeUpdateDate {get; private set;}

        public ChangeTracker InflowTracker {get; private set;} = new ChangeTracker();

        public override void Reset(ContractExecutor executor)
        {
            base.Reset(executor);
            InflowTracker.ResetAll();
        }

        protected override (DateTime processedTime, DateTime bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            if (currentTime == StartTime)
            {
                CurrentInflowIndex = 0;
                NextFlowChangeUpdateDate = descriptor.Inflows[CurrentInflowIndex].EndTime; // currentTime.NextAnniversayCrossing(descriptor.YearlyFlowChangeUpdateMonth, descriptor.YearlyFlowChangeUpdateDay);
                InflowTracker.ResetAll();
                            
                return (currentTime, NextFlowChangeUpdateDate);
            }
            else
            {
                // The yearly inflow is the amount by which the cash account will be increased over a year (negative for decrease).
                var rateInBucket = descriptor.Inflows[CurrentInflowIndex].Rate;
                var executionTimeSpan = (currentTime - lastProcessedTime)!.Value;

                var inflow = rateInBucket * (decimal)executionTimeSpan.TotalDays; // pro-rate the inflow by the fraction of the time span that has passed in the current bucket
                Account!.Balance += inflow;
                InflowTracker.TrackChange(inflow);

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
                return (currentTime, NextFlowChangeUpdateDate);
            }
       }
    }
}