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
    public abstract class InvestmentSchedule(Investment investment, DateTime startTime, decimal initialValue, Func<decimal, decimal> getGrowthRate) 
        : Contract(startTime, $"Schedule for {investment.Name}")
    {
        public const string ChangeTrackerPropertyFees  = "RentalInducedNetIncomeChangeTracker";

        public Investment Investment { get; } = investment;

        /// <summary>
        ///  The current value of the investment, which is a compound flow that grows over time based on the growth rate function provided.
        /// </summary>
        public CompoundFlow Value { get; private set; } = new CompoundFlow(startTime, initialValue, getGrowthRate, TimeSpan.FromDays((double)Constants.DaysPerYear), investment, $"Property Value for {investment.Name}");

        protected abstract IEnumerable<Contract> SubContracts { get; }

        protected override (DateTime processedTime, DateTime? bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            var bookedTime = executor.ExecuteContracts(SubContracts.Prepend(Value), currentTime);
            return (currentTime, bookedTime);
        }
    }
}
