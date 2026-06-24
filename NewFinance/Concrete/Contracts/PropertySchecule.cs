using NewFinance.Concrete.Accounts;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class PropertySchedule(Property property, DateTime purchaseTime, decimal purchasePrice, DateTime startTime, decimal initialValue, Func<decimal, decimal> getGrowthRate,  Account costPaymentAccount) 
        : InvestmentSchedule(property, purchaseTime, purchasePrice, startTime, initialValue, getGrowthRate, costPaymentAccount)
    {
        // Land tax Levy etc.
        public decimal InitialAnnualBaseFeeRate { get; set; }

        public decimal InitialAnnualRentalFeeRate { get; set; }

        public bool IsInvestmentProperty => YieldInducedStream is not null;

        public override decimal InitialFeeRate        
        {
            get
            {
                return InitialAnnualBaseFeeRate + (IsInvestmentProperty ? InitialAnnualRentalFeeRate : 0);
            }
        }
    }
}