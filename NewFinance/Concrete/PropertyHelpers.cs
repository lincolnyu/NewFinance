
using NewFinance.Concrete.Accounts;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public static class PropertyHelpers
    {
        public static Property CreatePropertyWithSchedule(string name, decimal purchasePrice, decimal purchaseAdditionalCost, DateTime inflationStartTime, DateTime purchaseTime, DateTime rentalStartTime, decimal growthRate, decimal priceValueCap,
            decimal initialYearlyRent, decimal totalRentalInducedRate, decimal rentIncreaseRate, decimal? rentCap, decimal initialTotalLevyAndRatesAnnualRate, decimal levyAndRatesInflationRate, Account rentalIncomeAccount)
        {
            var property = new Property(name)
            {
                PurchaseAdditionalCost = purchaseAdditionalCost
            };
            var yearlyNetRent = initialYearlyRent * (1 - totalRentalInducedRate);
            var rentalInflation = FlowHelpers.ConstantInflation(inflationStartTime, rentIncreaseRate);
            var rentalNetInFlowDescriptor = rentalInflation.ApplyInflation(rentalStartTime, yearlyNetRent/Constants.DaysPerYear);
            if (rentCap.HasValue)
            {
                FlowHelpers.FlowCapping(rentalNetInFlowDescriptor, rentCap.Value/Constants.DaysPerYear, true);
            }
            var schedule = new PropertySchedule(property, purchaseTime, purchasePrice, p=>p>=priceValueCap ? 0 : growthRate, rentalNetInFlowDescriptor, rentalIncomeAccount)
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
    }
}