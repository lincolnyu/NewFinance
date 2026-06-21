using NewFinance.Concrete.Accounts;
using NewFinance.Concrete.Entities;
using NewFinance.Concrete.Rules;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class IndividualTax(TaxIndividual taxPayer, Account cashPaymentAccount) : Contract(null, $"Individual Tax for {taxPayer.Name}")
    {
        public const string ChangeTrackerTaxPaid = "TaxPaid";

        public TaxIndividual TaxPayer { get; } = taxPayer;

        private decimal _rentalLossPool = 0m;

        protected override (DateTime processedTime, DateTime? bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            if (currentTime.IsEOFY())
            {
                PerformTaxAccounting(executor, currentTime);
            }

            var nextEOFY = currentTime.NextEOFY();

            executor.ReEnsureNextForcedTime(nextEOFY);

            return (currentTime, nextEOFY);
        }

        private void PerformTaxAccounting(ContractExecutor executor, DateTime currentTime)
        {
            decimal totalPropertyGain = 0m;
            foreach (var asset in TaxPayer.Assets)
            {
                if (asset is Property property)
                {
                    var propertySchedule = property.Schedule!;

                    // This is also to clear trackers.
                    (var _, var share) = property.Ownership.TryGetValue(TaxPayer, out var s) ? (TaxPayer, s) : (null, 0m);

                    var loan = TaxPayer.Liabilities.OfType<Loan>().FirstOrDefault(loan => loan.Contract!.Property == property);
                    
                    var netRentalIncome = executor.ChangeTrackers?[propertySchedule.RentalInducedNetIncome, Common.SteadyFlow.ChangeTrackerInflow][this].GetTrackedChangeAndReset() * share ?? 0m;

                    var interestPaid = (-executor.ChangeTrackers?[loan?.Contract!, LoanContract.ChangeTrackerPaidInterest][this].GetTrackedChangeAndReset() ?? 0m) * share;

                    var fees = (-executor.ChangeTrackers?[propertySchedule, PropertySchedule.ChangeTrackerPropertyFees][this].GetTrackedChangeAndReset()?? 0m) * share;

                    var netRentalTaxable = netRentalIncome - interestPaid - fees;

                    if (propertySchedule.IsInvestmentProperty)
                    {
                        var allowNegativeGearing = IsNegativeGearingAllowed(property, currentTime);
                        if (!allowNegativeGearing && netRentalTaxable < 0)
                        {
                            _rentalLossPool += -netRentalTaxable;
                            netRentalTaxable = 0;
                        }

                        decimal? netCapitalGain = null;

                        if (executor.ChangeTrackers?.TryGetTracker(property, PropertyHelpers.ChangeTrackerSalesProceedsForTax, out var salesProceedsTracker) == true) // just sold
                        {
                            // Do not reset here as it may be used by another tax individual if co-owned.
                            decimal salesProceeds = salesProceedsTracker![this].TrackedChange;
                            var capitalGain = salesProceeds - property.PurchaseAdditionalCost - property.Schedule!.PurchasePrice;
                            netCapitalGain = capitalGain * share;
                        }
                        totalPropertyGain += netRentalTaxable + (netCapitalGain ?? 0);
                    }
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
            decimal totalPaygWithheld = 0;
            foreach (var contract in TaxPayer.TaxableContracts)
            {
                if (contract is Employment employment)
                {
                    totalIncome += executor.ChangeTrackers?[employment, Common.SteadyFlow.ChangeTrackerInflow][this].GetTrackedChangeAndReset() ?? 0m;
                    totalPaygWithheld += executor.ChangeTrackers?[employment, Employment.ChangeTrackerPaygWithheld][this].GetTrackedChangeAndReset() ?? 0m;
                }
                else if (contract is Deductible expense)
                {
                    totalDeduction += -executor.ChangeTrackers?[expense, Common.SteadyFlow.ChangeTrackerInflow][this].GetTrackedChangeAndReset() ?? 0m;
                }
            }

            // Final tax workout
            decimal totalTaxableIncome = Math.Max(0, totalIncome + totalPropertyGain - totalDeduction);
            decimal residentialIncome = CalculateResidentIncomeTax(totalTaxableIncome);
            decimal medicareLevy = new MedicareLevyRules().Calculate(totalTaxableIncome, TaxPayer);
            decimal totalTaxPayable = residentialIncome +  medicareLevy;
            decimal taxAssessmentBalance = totalTaxPayable - totalPaygWithheld;
            executor.ExecuteTransaction(cashPaymentAccount, -taxAssessmentBalance, this, $"Tax assessment for {Name}");
            executor.ChangeTrackers?[this, ChangeTrackerTaxPaid].TrackChange(-totalTaxPayable);
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

        internal static decimal CalculateResidentIncomeTax(decimal taxableIncome)
        {
            taxableIncome = Math.Max(0, taxableIncome);

            if (taxableIncome <= 18_200m)
            {
                return 0m;
            }

            if (taxableIncome <= 45_000m)
            {
                return (taxableIncome - 18_200m) * 0.16m;
            }

            if (taxableIncome <= 135_000m)
            {
                return 4_288m + (taxableIncome - 45_000m) * 0.30m;
            }

            if (taxableIncome <= 190_000m)
            {
                return 31_288m + (taxableIncome - 135_000m) * 0.37m;
            }

            return 51_638m + (taxableIncome - 190_000m) * 0.45m;
        }
    }
}
