using NewFinance.Common;
using NewFinance.Concrete.Accounts;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class PropertySchedule(Property property, DateTime purchaseTime, decimal purchasePrice, decimal growthRate, SteadyFlowDescriptor rentalNetInFlowDescriptor, Account rentalIncomeAccount) : Contract(purchaseTime)
    {
        public Property Property { get; } = property;

        public decimal PurchasePrice { get; } = purchasePrice;

        public Account RentalIncomeAccount { get; } = rentalIncomeAccount;

        public SteadyFlowDescriptor RentalFlowDescriptor { get; set; } 

        public decimal GrowthRate { get; } = growthRate;

        public decimal RentProportionalFeesRate { get; set; } = 0.1m;

        public CompoundFlow PropertyValue { get; private set;} = new CompoundFlow(purchaseTime, purchasePrice, growthRate, TimeSpan.FromDays(365.25), property);

        public SteadyFlow RentalNetIncome { get; private set;} = new SteadyFlow(rentalNetInFlowDescriptor, rentalIncomeAccount);

        protected override (DateTime processedTime, DateTime bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            var bookedTime = executor.ExecuteContracts([PropertyValue, RentalNetIncome], currentTime);
            return (currentTime, bookedTime);
        }
    }
}
