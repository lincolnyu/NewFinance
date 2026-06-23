using NewFinance.Common;
using NewFinance.Concrete.Accounts;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public abstract class InvestmentSchedule(Investment investment, DateTime purchaseTime, decimal purchasePrice, DateTime startTime, decimal initialValue, Func<decimal, decimal> getGrowthRate,  Account costPaymentAccount) 
        : Contract(startTime, $"Schedule for {investment.Name}")
    {
        public const string ChangeTrackerPropertyFees  = "RentalInducedNetIncomeChangeTracker";

        public Investment Investment { get; } = investment;

        public DateTime PurchaseTime { get; } = purchaseTime;

        public decimal PurchasePrice { get; } = purchasePrice;

        public CompoundFlow Value { get; private set; } = new CompoundFlow(startTime, initialValue, getGrowthRate, TimeSpan.FromDays((double)Constants.DaysPerYear), investment, $"Property Value for {investment.Name}");

        // Rent minus the fees proportional to rent (agent fees etc.)
        public SteadyFlow? YieldInducedStream { get; set; }


        #region Additional costs

        public abstract decimal InitialFeeRate { get; }

        public Inflation FeeInflation { get; set; }

        public (DateTime Time, Action<ContractExecutor, InvestmentSchedule> Action)? Sale { get; set; }

        #endregion

        protected override (DateTime processedTime, DateTime? bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            var subcontracts = YieldInducedStream is not null ? new Contract[] { Value, YieldInducedStream } : [Value];
            var bookedTime = executor.ExecuteContracts(subcontracts, currentTime);

            var feeInflation = FeeInflation.GetRelativeInflationFactor(startTime, currentTime);
            var lastTime = lastProcessedTime ?? startTime;

            var fees =  InitialFeeRate * feeInflation * (currentTime - lastTime).Days / Constants.DaysPerYear;

            executor.ExecuteTransaction(costPaymentAccount, -fees, this, $"Fees for {Investment.Name}");
            // TODO Additional costs such as repair, adhoc...

            executor.ChangeTrackers?.GetOrCreateTracker(this, ChangeTrackerPropertyFees).TrackChange(-fees);

            if (currentTime == Sale?.Time)
            {
                Sale?.Action(executor, this);
            }

            return (currentTime, bookedTime);
        }
    }
}
