using NewFinance.Common;
using NewFinance.Concrete.Accounts;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class PropertySchedule(Property property, DateTime purchaseTime, decimal purchasePrice, DateTime startTime, decimal initialValue, Func<decimal, decimal> getGrowthRate,  Account costPaymentAccount) 
        : Contract(startTime, $"Property Schedule for {property.Name}")
    {
        public const string ChangeTrackerPropertyFees  = "RentalInducedNetIncomeChangeTracker";

        public Property Property { get; } = property;

        public DateTime PurchaseTime { get; } = purchaseTime;

        public decimal PurchasePrice { get; } = purchasePrice;

        public CompoundFlow PropertyValue { get; private set; } = new CompoundFlow(startTime, initialValue, getGrowthRate, TimeSpan.FromDays((double)Constants.DaysPerYear), property, $"Property Value for {property.Name}");

        // Rent minus the fees proportional to rent (agent fees etc.)
        public SteadyFlow? RentalInducedNetIncome { get; set; }

        #region Additional costs

        // Land tax Levy etc.
        public decimal InitialAnnualBaseFeeRate { get; set; }

        public decimal InitialAnnualRentalFeeRate { get; set; }

        public bool IsInvestmentProperty => RentalInducedNetIncome is not null;

        public Inflation FeeInflation { get; set; }

        public (DateTime Time, Action<ContractExecutor, PropertySchedule> Action)? Sale { get; set; }

        #endregion

        protected override (DateTime processedTime, DateTime? bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            var subcontracts = RentalInducedNetIncome is not null ? new Contract[] { PropertyValue, RentalInducedNetIncome } : [PropertyValue];
            var bookedTime = executor.ExecuteContracts(subcontracts, currentTime);

            var levyInflation = FeeInflation.GetRelativeInflationFactor(startTime, currentTime);
            var lastTime = lastProcessedTime ?? startTime;

            var initialAnnualFeeRate = InitialAnnualBaseFeeRate + (IsInvestmentProperty ? InitialAnnualRentalFeeRate : 0);
            var fees =  initialAnnualFeeRate * levyInflation * (currentTime - lastTime).Days / Constants.DaysPerYear;

            executor.ExecuteTransaction(costPaymentAccount, -fees, this, $"Fees for {Property.Name}");
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
