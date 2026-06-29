using NewFinance.Common;
using NewFinance.Concrete.Accounts;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class PropertySchedule
        : InvestmentSchedule
    {
        public PropertySchedule(Property property, DateTime purchaseTime, decimal purchasePrice, DateTime startTime, decimal initialValue, Func<decimal, decimal> getGrowthRate,  Account costPaymentAccount)  
            : base(property, startTime, initialValue, getGrowthRate)
        {
            PurchaseTime = purchaseTime;
            PurchasePrice = purchasePrice;
            CostPaymentAccount = costPaymentAccount;

            FeeContract = new ContextualContract(
                startTime, 
                "Property Fees",
                this,
                (context, executor, lastProcessedTime, lastBookedTime, currentTime) =>
                {
                    var schedule = (PropertySchedule)context;
                    var lastTime = lastProcessedTime ?? schedule.StartTime!.Value;

                    var feeInflation = schedule.FeeInflation.GetRelativeInflationFactor(schedule.StartTime!.Value, currentTime);
                    var fees =  schedule.InitialFeeRate * feeInflation * (currentTime - lastTime).Days / Constants.DaysPerYear;

                    executor.ExecuteTransaction(schedule.CostPaymentAccount, -fees, schedule, $"Fees for {schedule.Investment.Name}");
                    
                    // TODO Additional costs such as repair, adhoc...
                    // TODO some of the government fees such as land tax could be proportional to the property value.

                    executor.ChangeTrackers?.GetOrCreateTracker(schedule, ChangeTrackerPropertyFees).TrackChange(-fees);

                    if (currentTime == schedule.Sale?.Time)
                    {
                        schedule.Sale?.Action(executor, schedule);
                    }

                    return (currentTime, lastBookedTime);
                }
            );
        }

        public Account CostPaymentAccount { get; }

        public DateTime PurchaseTime { get; }

        public decimal PurchasePrice { get; }

        /// <summary>
        ///  The base fees even if the property is not rented out.
        /// </summary>
        public decimal InitialAnnualBaseFeeRate { get; set; }

        /// <summary>
        ///  The fees that incurr when the property is rented out. For example, property management fee, rental agent fee, etc that are not proportional to the rental income but are proportional to the inflation.
        /// </summary>
        public decimal InitialAnnualRentalFeeRate { get; set; }

        /// <summary>
        ///  The rental income minus the fees that are proportional to the rental income (e.g. property management fee, rental agent fee, etc.)
        /// </summary>
        public BandedFlow? RentInducedStream { get; set; }

        public bool IsInvestmentProperty => RentInducedStream is not null;

        public Inflation FeeInflation { get; set; }

        public decimal InitialFeeRate => InitialAnnualBaseFeeRate + (IsInvestmentProperty ? InitialAnnualRentalFeeRate : 0);

        public (DateTime Time, Action<ContractExecutor, InvestmentSchedule> Action)? Sale { get; set; }

        protected override IEnumerable<Contract> SubContracts
        {
            get
            {
                if (IsInvestmentProperty)
                {
                    yield return RentInducedStream!;
                }
                yield return FeeContract;
            }    
        }

        private ContextualContract FeeContract {get; }
    }
}
