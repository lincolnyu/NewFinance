using NewFinance.Concrete.Accounts;
using NewFinance.Concrete.Entities;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class IndividualTax(TaxIndividual taxPayer, Account cashPaymentAccount) : Contract(null, $"Individual Tax for {taxPayer.Name}")
    {
        public TaxIndividual TaxPayer { get; } = taxPayer;

        public ChangeTracker TaxPaid { get; } = new ChangeTracker();

        private decimal _rentalLossPool = 0m;

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
            decimal totalPropertyGain = 0m;
            foreach (var asset in TaxPayer.Assets)
            {
                if (asset is Property property)
                {
                    var propertySchedule = property.Schedule!;

                    if (!propertySchedule.IsInvestmentProperty)
                    {
                        continue;
                    }

                    (var _, var share) = property.Ownership.TryGetValue(TaxPayer, out var s) ? (TaxPayer, s) : (null, 0m);

                    var loan = TaxPayer.Liabilities.OfType<Loan>().FirstOrDefault(loan => loan.Contract!.Property == property);

                    var netRentalIncome = propertySchedule.RentalInducedNetIncome?.InflowTracker[this].GetTrackedChangeAndReset() ?? 0m;

                    var interestPaid = loan?.Contract!.PaidInterestTracker[this].GetTrackedChangeAndReset() * share ?? 0m;

                    var netRentalTaxable = netRentalIncome - interestPaid;

                    var allowNegativeGearing = IsNegativeGearingAllowed(property, currentTime);
                    if (!allowNegativeGearing && netRentalTaxable < 0)
                    {
                        _rentalLossPool += -netRentalTaxable;
                        netRentalTaxable = 0;
                    }

                    decimal? netCapitalGain = null;
                    if (property.SalesProceeds is not null) // just sold
                    {
                        var saleProceeds =  property.SalesProceeds!.TotalChange;
                        var capitalGain = saleProceeds - property.PurchaseAdditionalCost;
                        netCapitalGain = capitalGain * share;
                    }

                    totalPropertyGain += netRentalTaxable + (netCapitalGain ?? 0);
                }
            }

            if (totalPropertyGain > 0 && _rentalLossPool > 0)
            {
                var lossOffset = Math.Min(_rentalLossPool, totalPropertyGain);
                _rentalLossPool -= lossOffset;
                totalPropertyGain -= lossOffset;
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
            decimal totalTaxableIncome = totalIncome + totalPropertyGain - totalDeduction;
            decimal totalTaxPayable = totalTaxableIncome * (decimal)TaxRateFor(totalTaxableIncome);
            cashPaymentAccount.Balance -= totalTaxPayable;
            TaxPaid.TrackChange(totalTaxPayable);
        }

        private bool IsNegativeGearingAllowed(Property property, DateTime currentTime)
        {
            //### Labor's Grandfathering Rules
            //| Purchase Date | Type of Property | Negative Gearing Allowed? |
            //|---------------|------------------|---------------------------|
            //| Before 7:30pm 12 May 2026 | Any | Fully grandfathered (unchanged) |
            //| After 12 May 2026 | **New build** | Yes (full negative gearing continues) |
            //| After 12 May 2026 | **Established house** | Only until 30 June 2027. After that → restricted |

            if (property.Schedule!.PurchaseTime < new DateTime(2026, 5, 12, 19, 30, 0) || property.IsPurchasedAsNewBuild)
            {
                return true;
            }
            else if (currentTime <= new DateTime(2027, 6, 30))
            {
                return true;
            }
            else
            {
                return false;
            }
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
