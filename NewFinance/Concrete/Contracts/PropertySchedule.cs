using NewFinance.Common;
using NewFinance.Concrete.Accounts;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class PropertySchedule(Property property, DateTime purchaseTime, decimal purchasePrice, Func<decimal, decimal> getGrowthRate, SteadyFlowDescriptor rentalNetInFlowDescriptor, Account rentalIncomeAccount) : Contract(purchaseTime, $"Property Schedule for {property.Name}")
    {
        public CompoundFlow PropertyValue { get; private set;} = new CompoundFlow(purchaseTime, purchasePrice, getGrowthRate, TimeSpan.FromDays((double)Constants.DaysPerYear), property, $"Property Value for {property.Name}");

        // Rent minus the fees proportional to rent (agent fees etc.)
        public SteadyFlow RentalInducedNetIncome { get; private set;} = new SteadyFlow(rentalNetInFlowDescriptor, rentalIncomeAccount, $"Rental Net Income for {property.Name}");

        #region Additional costs
        public ChangeTracker ExtraFeesTracker {get; private set;} = new ChangeTracker();

        // Land tax Levy etc.
        public decimal InitialTotalLevyAndRatesAnnualRate { get; set; }

        public Inflation LevyAndRatesInflation { get; set; }

        #endregion

        protected override (DateTime processedTime, DateTime bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            var bookedTime = executor.ExecuteContracts([PropertyValue, RentalInducedNetIncome], currentTime);

            var levyInflation = LevyAndRatesInflation.GetRelativeInflationFactor(purchaseTime, currentTime);
            var lastTime = lastProcessedTime ?? purchaseTime;
            var govFees = InitialTotalLevyAndRatesAnnualRate * levyInflation * (currentTime - lastTime).Days / Constants.DaysPerYear;
            rentalIncomeAccount.Balance -= govFees;
            ExtraFeesTracker.TrackIncrease(-govFees);

            // TODO Additional costs such as repair, adhoc...

            return (currentTime, bookedTime);
        }
    }
}
