using NewFinance.Common;
using NewFinance.Concrete.Accounts;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class FundSchedule : InvestmentSchedule
    {
        // Define properties and methods for FundSchedule here
        public FundSchedule(Fund fund, DateTime startTime, decimal initialValue, Func<decimal, decimal> getGrowthRate, 
            Func<FundSchedule, TimeSpan, decimal> getYield, Func<decimal, (Account, decimal)>? cash) : base(fund, startTime, initialValue, getGrowthRate)
        {
            YieldContract = new ContextualContract(
                startTime, 
                "Investment Yield",
                this,
                (context, executor, lastProcessedTime, lastBookedTime, currentTime) =>
                {
                    var schedule = (FundSchedule)context;
                    var lastTime = lastProcessedTime ?? schedule.StartTime!.Value;

                    var yield = getYield(schedule, currentTime - lastTime);
                    decimal reinvestment = yield;
                    if (cash is not null)
                    {
                        (var cashAccount, var cashAmount) = cash(yield); 
                        executor.ExecuteTransaction(cashAccount, cashAmount, schedule.YieldContract!, $"Yield for {schedule.Investment.Name}");
                        reinvestment -= cashAmount;

                        if (reinvestment < 0)
                        {
                            // TODO capital gain
                            // tax selling + yield
                        }
                        else if(cashAmount > 0)
                        {
                            // tax for yield
                        }
                    }
                    
                    if (reinvestment > 0)
                    {
                        executor.ExecuteTransaction(schedule.Investment, reinvestment, schedule.YieldContract!, $"Reinvestment for {schedule.Investment.Name}");
                    }

                    return (currentTime, lastBookedTime);
                }
            );

            FeeContract = new ContextualContract(
                startTime, 
                "Investment Fees",
                this,
                (context, executor, lastProcessedTime, lastBookedTime, currentTime) =>
                {
                    var schedule = (PropertySchedule)context;
                    var lastTime = lastProcessedTime ?? schedule.StartTime!.Value;

                    var annualFee = schedule.Investment.Balance * AnnualFeeRateToValue;
                    if (annualFee > AnnualFeeCap)
                    {
                        annualFee = AnnualFeeCap.Value;
                    }
                    var fees = annualFee * (currentTime - lastTime).Days / Constants.DaysPerYear;

                    return (currentTime, lastBookedTime);
                }
            );
        }

        public ContextualContract YieldContract { get; }

        public ContextualContract FeeContract { get; }

        public decimal AnnualFeeRateToValue { get; set; }

        public decimal? AnnualFeeCap { get; set; }

        protected override IEnumerable<Contract> SubContracts
        {
            get
            {
                yield return YieldContract;
                yield return FeeContract;
            }
        }

        public override DateTime? Execute(ContractExecutor executor, DateTime currentTime)
        {
            return base.Execute(executor, currentTime);
        }
    }
}