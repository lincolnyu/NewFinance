using NewFinance.Concrete.Accounts;
using NewFinance.Concrete.Entities;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class IndividualTax(TaxIndividual taxPayer, Account cashPaymentAccount) : Contract(null)
    {
        public TaxIndividual TaxPayer { get; } = taxPayer;

        public override void Reset(ContractExecutor executor)
        {
            base.Reset(executor);
        }

        protected override (DateTime processedTime, DateTime bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            if (currentTime.IsEOFY())
            {
                PerformTaxAccounting(currentTime);
            }

            var nextEOFY = currentTime.NextEOFY();

            executor.ReEnsureNextForcedTime(nextEOFY);

            return (currentTime, nextEOFY);
        }

        private void PerformTaxAccounting(DateTime currentTime)
        {
            Dictionary<Property, decimal> propertyTaxableIncomes = new Dictionary<Property, decimal>();

            foreach (var asset in TaxPayer.Assets)
            {
                if (asset is Property property)
                {
                    var propertySchedule = property.Schedule!;
                    (var _, var share) = property.Ownership.First(o => o.Owner == TaxPayer);

                    var loan = TaxPayer.Liabilities.OfType<Loan>().First(loan => loan.Contract.Property == property);

                    var netRentalIncome = propertySchedule.RentalNetIncome.InflowTracker[this].GetTrackedChangeAndReset();

                    var interestPaid = loan.Contract.YearToDateInterestPaid * share;

                    var taxableIncome = netRentalIncome - interestPaid;

                    propertyTaxableIncomes[property] = taxableIncome;
                }
            }
           
            decimal totalIncome = 0;
            decimal totalDeduction = 0;
            foreach (var contract in TaxPayer.TaxableContracts)
            {
                if (contract is Employment employment)
                {
                    totalIncome += employment.InflowTracker[this].GetTrackedChangeAndReset();
                }
                else if (contract is Deductible expense)
                {
                    totalDeduction += -expense.InflowTracker[this].GetTrackedChangeAndReset();
                }
            }

            decimal totalTaxPaiable = 0;
            cashPaymentAccount.Balance -= totalTaxPaiable;
        }
    }
}
