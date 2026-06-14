using NewFinance.Common;
using NewFinance.Concrete.Accounts;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class PropertySchedule(Property property, DateTime purchaseTime, decimal purchasePrice, decimal growthRate, SteadyFlowDescriptor rentalNetInFlowDescriptor, Account rentalIncomeAccount) : Contract(purchaseTime, $"Property Schedule for {property.Name}")
    {
        public Property Property { get; } = property;

        public decimal PurchasePrice { get; } = purchasePrice;

        public SteadyFlowDescriptor RentalFlowDescriptor { get; set; } 

        public decimal GrowthRate { get; } = growthRate;

        public decimal RentProportionalFeesRate { get; set; } = 0.1m;

        public CompoundFlow PropertyValue { get; private set;} = new CompoundFlow(purchaseTime, purchasePrice, growthRate, TimeSpan.FromDays(365.25), property, $"Property Value for {property.Name}");

        // Rent minus the fees proportional to rent (agent fees etc.)
        public SteadyFlow RentalInducedNetIncome { get; private set;} = new SteadyFlow(rentalNetInFlowDescriptor, rentalIncomeAccount, $"Rental Net Income for {property.Name}");

        public ChangeTracker ExtraFeesTracker {get; private set;} = new ChangeTracker();

        // Land tax Levy etc.
        public decimal InitialTotalGovernmentFeesAnnualRate { get; set; }

        public Inflation GovernmentFeesInflation { get; set; }

        protected override (DateTime processedTime, DateTime bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            var bookedTime = executor.ExecuteContracts([PropertyValue, RentalInducedNetIncome], currentTime);

            var govFeesInflation = GovernmentFeesInflation.GetRelativeInflationFactor(purchaseTime, currentTime);
            var govFees = InitialTotalGovernmentFeesAnnualRate * govFeesInflation * (currentTime - lastProcessedTime!.Value).Days / Constants.daysPerYear;
            rentalIncomeAccount.Balance -= govFees;
            ExtraFeesTracker.TrackIncrease(-govFees);

            // TODO Additional fees...

            return (currentTime, bookedTime);
        }
    }
}
