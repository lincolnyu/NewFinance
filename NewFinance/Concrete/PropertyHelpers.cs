
using NewFinance.Common;
using NewFinance.Concrete.Accounts;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public static class PropertyHelpers
    {
        public static Property CreatePropertyWithSchedule(string name, DateTime purchaseTime, decimal purchasePrice, decimal purchaseAdditionalCost, DateTime initialTime, decimal initialValue, decimal growthRate, 
            decimal priceValueCap,  decimal initialBaseAnnualFeeRate, decimal initialRentalFeeRate, decimal levyAndRatesInflationRate, Account cashAccount)
        {
            var property = new Property(name)
            {
                PurchaseAdditionalCost = purchaseAdditionalCost
            };

            var schedule = new PropertySchedule(property, purchaseTime, purchasePrice, initialTime, initialValue, p=>p>=priceValueCap ? 0 : growthRate, cashAccount)
            {
                InitialAnnualBaseFeeRate = initialBaseAnnualFeeRate,
                InitialAnnualRentalFeeRate = initialRentalFeeRate,
                FeeInflation = FlowHelpers.ConstantInflation(purchaseTime, levyAndRatesInflationRate)
            };
            property.Schedule = schedule;
            return property;
        }

        public static SteadyFlow CreatePropertyRentalStream(string name, DateTime startTime, decimal initialYearlyRent, decimal totalRentalInducedRate, decimal rentIncreaseRate, decimal? yearlyRentCap, Account cashAccount)
        {
            var yearlyNetRent = initialYearlyRent * (1 - totalRentalInducedRate);
            var rentalInflation = FlowHelpers.ConstantInflation(startTime, rentIncreaseRate);
            var rentalNetInFlowDescriptor = rentalInflation.ApplyInflation(startTime, yearlyNetRent/Constants.DaysPerYear);
            if (yearlyRentCap.HasValue)
            {
                FlowHelpers.FlowCapping(rentalNetInFlowDescriptor, yearlyRentCap.Value/Constants.DaysPerYear, false);
            }
            return new SteadyFlow(rentalNetInFlowDescriptor, cashAccount, name);
        }

        public static Loan CreatePersonalLoan(string name, DateTime startTime, decimal loanAmount, Account cashAccount, decimal? loanTermYears, decimal annualInterestRate, bool alreadySettled)
        {
            var loan = new Loan($"Personal Loan {name}");

            var loanContract = new LoanContract(loan, null!, null, startTime, loanAmount, alreadySettled)
            {
                CashAccount = cashAccount,
                LoanTermYears = loanTermYears,
                AnnualInterestRate = annualInterestRate
            };
            loan.Contract = loanContract;

            return loan;
        }

        public static Loan CreateLoan(Property property, (DateTime, decimal)? deposit, decimal loanAmount, Account cashAccount, decimal offsetRatio, decimal? loanTermYears, decimal annualInterestRate, bool alreadySettled)
        {
            var loan = new Loan($"Loan for {property.Name}");

            var loanContract = new LoanContract(loan, property, deposit, null, loanAmount, alreadySettled)
            {
                CashAccount = cashAccount,
                OffsetRatio = offsetRatio,
                LoanTermYears = loanTermYears,
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
            property.Schedule!.Sale =(saleTime, (executor, schedule) =>
            {
                var salePrice = property.Balance;
                var salesProceeds = salePrice - saleCost;

                executor.ExecuteTransaction(property, -property.Balance, schedule, $"Sale of {property.Name}");

                if (loan is not null)
                {
                    var loanRepayment = loan.Balance;
                    executor.ExecuteTransaction(loan, -loanRepayment, schedule, $"Closure of loan {loan.Name}");
                    salesProceeds += loanRepayment;
                }

                property.SalesProceeds = new ChangeTracker();
                property.SalesProceeds.TrackChange(salesProceeds);

                executor.ExecuteTransaction(cashAccount, salesProceeds, schedule, $"Proceeds from sale of {property.Name}");
            });
        }
    }
}