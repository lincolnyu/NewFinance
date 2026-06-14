using NewFinance.Concrete.Accounts;
using NewFinance.Concrete.Entities;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class IndividualTax(TaxIndividual taxPayer, Account cashPaymentAccount) : Contract(null, $"Individual Tax for {taxPayer.Name}")
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

        private void PerformTaxAccounting(DateTime _)
        {
            Dictionary<Property, decimal> propertyTaxableIncomes = new Dictionary<Property, decimal>();

            foreach (var asset in TaxPayer.Assets)
            {
                if (asset is Property property)
                {
                    var propertySchedule = property.Schedule!;
                    (var _, var share) = property.Ownership.TryGetValue(TaxPayer, out var s) ? (TaxPayer, s) : (null, 0m);

                    var loan = TaxPayer.Liabilities.OfType<Loan>().FirstOrDefault(loan => loan.Contract!.Property == property);

                    var netRentalIncome = propertySchedule.RentalInducedNetIncome.InflowTracker[this].GetTrackedChangeAndReset();

                    var interestPaid = loan?.Contract!.PaidInterestTracker[this].GetTrackedChangeAndReset() * share ?? 0m;

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

            // Final tax workout
            decimal totalTaxableIncome = totalIncome + propertyTaxableIncomes.Values.Sum() - totalDeduction;
            decimal totalTaxPayable = totalTaxableIncome * (decimal)TaxRateFor(totalTaxableIncome);
            cashPaymentAccount.Balance -= totalTaxPayable;
        }

        private double TaxRateFor(decimal taxableIncome)
        {
            if (taxableIncome <= 18200)
            {
                return 0;
            }
            else if (taxableIncome <= 45000)
            {
                return 0.16;
            }
            else if (taxableIncome <= 135000)
            {
                return 0.30;
            }
            else if (taxableIncome <= 180000)
            {
                return 0.37;
            }
            else
            {
                return 0.45;
            }
        }
    }
}
