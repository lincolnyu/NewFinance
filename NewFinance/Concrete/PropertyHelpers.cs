
using NewFinance.Common;
using NewFinance.Concrete.Accounts;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public static class PropertyHelpers
    {
        public static Property CreatePropertyWithSchedule(string name, DateTime purchaseTime, decimal purchasePrice, decimal purchaseAdditionalCost, DateTime initialTime, decimal initialValue, decimal growthRate, decimal priceValueCap, DateTime inflationStartTime, DateTime? rentalStartTime, 
            decimal initialYearlyRent, decimal totalRentalInducedRate, decimal rentIncreaseRate, decimal? rentCap, decimal initialTotalLevyAndRatesAnnualRate, decimal levyAndRatesInflationRate, Account rentalIncomeAccount)
        {
            var property = new Property(name)
            {
                PurchaseAdditionalCost = purchaseAdditionalCost
            };
            var yearlyNetRent = initialYearlyRent * (1 - totalRentalInducedRate);

            SteadyFlowDescriptor? rentalNetInFlowDescriptor = null;
            if (rentalStartTime.HasValue)
            {
                var rentalInflation = FlowHelpers.ConstantInflation(inflationStartTime, rentIncreaseRate);
                rentalNetInFlowDescriptor = rentalInflation.ApplyInflation(rentalStartTime.Value, yearlyNetRent/Constants.DaysPerYear);
                if (rentCap.HasValue)
                {
                    FlowHelpers.FlowCapping(rentalNetInFlowDescriptor.Value, rentCap.Value/Constants.DaysPerYear, true);
                }
            }
            var schedule = new PropertySchedule(property, purchaseTime, purchasePrice, initialTime, initialValue, p=>p>=priceValueCap ? 0 : growthRate, rentalNetInFlowDescriptor, rentalIncomeAccount)
            {
                InitialTotalLevyAndRatesAnnualRate = initialTotalLevyAndRatesAnnualRate,
                LevyAndRatesInflation = FlowHelpers.ConstantInflation(inflationStartTime, levyAndRatesInflationRate)
            };
            property.Schedule = schedule;
            return property;
        }

        public static Loan CreateLoan(Property property, decimal loanAmount, Account cashAccount, decimal offsetRatio, decimal? principalRepaymentTotalYears, decimal annualInterestRate, bool alreadySettled)
        {
            var loan = new Loan($"Loan for {property.Name}");

            var loanContract = new LoanContract(loan, property, loanAmount, alreadySettled)
            {
                CashAccount = cashAccount,
                OffsetRatio = offsetRatio,
                AnnualPrincipalPayment = principalRepaymentTotalYears.HasValue? loanAmount / principalRepaymentTotalYears.Value : 0,
                AnnualInterestRate = annualInterestRate
            };
            loan.Contract = loanContract;

            return loan;
        }

        public static decimal BasicCapitalGain(Property property, decimal saleCost)
        {
            var salePrice = property.Balance;
            var saleProceeds = salePrice - saleCost;
            var captialGain = saleProceeds - property.Schedule!.PurchasePrice - property.PurchaseAdditionalCost;
            return captialGain;
        }

        public static void SellPropety(Property property, decimal saleCost, Loan? loan, DateTime saleTime, Account cashAccount)
        {
            property.Schedule!.Sale =(saleTime , () =>
            {
                var salePrice = property.Balance;
                var salesProceeds = salePrice - saleCost;

                property.Balance = 0;

                if (loan != null)
                {
                    var loanRepayment = loan.Balance;
                    loan.Balance = 0;
                    salesProceeds += loanRepayment;
                }

                property.SalesProceeds = new ChangeTracker();
                property.SalesProceeds.TrackChange(salesProceeds);

                cashAccount.Balance += salesProceeds;
            });
        }
    }
}