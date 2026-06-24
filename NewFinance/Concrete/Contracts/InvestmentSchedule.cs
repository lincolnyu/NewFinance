using NewFinance.Common;
using NewFinance.Concrete.Accounts;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    /// <summary>
    /// Schedule for investment. It tracks the value of the property, the yield and the fees.
    /// </summary>
    /// <param name="investment">The investment associated with this schedule.</param>
    /// <param name="purchaseTime">The time when the investment was purchased. For property it is the time when the contract is exchanged NOT the settlement date.</param>
    /// <param name="purchasePrice">The price at which the investment was purchased.</param>
    /// <param name="startTime">
    ///     The start time of this contract in simulation, which is the start of its value tracking. Normally no later than yield start time which is set up separately. 
    ///     It usually is the max of purchaseTime and simulation start time.
    /// </param>
    /// <param name="initialValue">The initial value of the investment (at the `startTime`).</param>
    /// <param name="getGrowthRate">A function to get the growth rate of the investment.`</param>
    /// <param name="costPaymentAccount">Account to pay for the fees.</param>
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
