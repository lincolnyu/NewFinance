using NewFinance.Core;

namespace NewFinance.Common
{
    public class CompoundFlow(DateTime startTime, decimal initialValue, decimal growthRate, TimeSpan timeStep, Account account) : AccountBindingContract(startTime, account)
    {
        /// <summary>
        ///  The growth rate is the factor by which the cash account will be increased over each time step (negative for decrease). For example, if the growth rate is 0.05 and the time step is 1 month, then the account will be increased by 5% every month.
        /// </summary>
        public decimal GrowthRate { get; } = growthRate;

        public TimeSpan Step { get; } = timeStep;

        public override void Reset(ContractExecutor executor)
        {
            base.Reset(executor);
            Account!.Balance = 0;
        }
        
        protected override (DateTime processedTime, DateTime bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            if (currentTime == StartTime)
            {
                Account!.Balance = initialValue;
                return (currentTime, currentTime.Add(Step));
            }
            else
            {
                DateTime newTime;
                while (true)
                {
                    newTime = lastProcessedTime!.Value.Add(Step);
                    if (newTime > currentTime)
                    {
                        break;
                    }

                    Account!.Balance *= 1 + GrowthRate;
                    
                    lastProcessedTime = newTime;
                }
                return (lastProcessedTime!.Value, newTime);
            }
        }
    }
}