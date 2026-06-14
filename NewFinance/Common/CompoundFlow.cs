using NewFinance.Core;

namespace NewFinance.Common
{
    public class CompoundFlow(DateTime startTime, decimal initialValue, Func<decimal, decimal> getGrowthRate, TimeSpan timeStep, Account account, string name) : AccountBindingContract(startTime, account, name)
    {
        /// <summary>
        ///  The growth rate is the factor by which the cash account will be increased one yearly. For example, if the growth rate is 0.05, the cash account will be increased by 5% every year.
        /// </summary>
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

                    var growthRate = getGrowthRate(Account!.Balance);
                    var growthEachTimeStep = growthRate * Step.Days / Constants.DaysPerYear;
                    Account!.Balance *= 1 + growthEachTimeStep;
                    
                    lastProcessedTime = newTime;
                }
                return (lastProcessedTime!.Value, newTime);
            }
        }
    }
}