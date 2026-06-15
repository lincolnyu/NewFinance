using NewFinance.Common;
using NewFinance.Concrete.Accounts;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class PropertySchedule(Property property, DateTime purchaseTime, decimal purchasePrice, DateTime initialTime, decimal initialValue, Func<decimal, decimal> getGrowthRate, SteadyFlowDescriptor? rentalNetInFlowDescriptor, Account cashOrRentalIncomeAccount) : Contract(initialTime, $"Property Schedule for {property.Name}")
    {
        public DateTime PurchaseTime { get; } = purchaseTime;

        public decimal PurchasePrice { get; } = purchasePrice;

        public CompoundFlow PropertyValue { get; private set; } = new CompoundFlow(initialTime, initialValue, getGrowthRate, TimeSpan.FromDays((double)Constants.DaysPerYear), property, $"Property Value for {property.Name}");

        public bool IsInvestmentProperty => RentalInducedNetIncome != null;

        // Rent minus the fees proportional to rent (agent fees etc.)
        public SteadyFlow? RentalInducedNetIncome { get; private set;} = rentalNetInFlowDescriptor.HasValue ? new SteadyFlow(rentalNetInFlowDescriptor.Value, cashOrRentalIncomeAccount, $"Rental Net Income for {property.Name}") : null;

        #region Additional costs
        public ChangeTracker ExtraFeesTracker {get; private set;} = new ChangeTracker();

        // Land tax Levy etc.
        public decimal InitialTotalLevyAndRatesAnnualRate { get; set; }

        public Inflation LevyAndRatesInflation { get; set; }

        public (DateTime Time, Action Action)? Sale {get; set;}

        #endregion

        protected override (DateTime processedTime, DateTime bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            var subcontracts = rentalNetInFlowDescriptor.HasValue ? new Contract[] { PropertyValue, RentalInducedNetIncome! } : [PropertyValue];
            var bookedTime = executor.ExecuteContracts(subcontracts, currentTime);

            var levyInflation = LevyAndRatesInflation.GetRelativeInflationFactor(initialTime, currentTime);
            var lastTime = lastProcessedTime ?? initialTime;
            var govFees = InitialTotalLevyAndRatesAnnualRate * levyInflation * (currentTime - lastTime).Days / Constants.DaysPerYear;
            cashOrRentalIncomeAccount.Balance -= govFees;
            // TODO Additional costs such as repair, adhoc...
            ExtraFeesTracker.TrackChange(govFees);

            if (currentTime == Sale?.Time)
            {
                Sale?.Action();
            }

            return (currentTime, bookedTime);
        }
    }
}
