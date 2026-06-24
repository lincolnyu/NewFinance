using NewFinance.Common;
using NewFinance.Concrete.Accounts;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class LoanContract : AccountBindingContract
    {
        public const string ChangeTrackerPaidInterest = "PaidInterestTracker";
        public const string ChangeTrackerPaidPrincipal = "PaidPrincipalTracker";

        public Property? Property { get; private set; }

        public required Account CashAccount { get; set; }

        public decimal? Deposit {get;}  // Paid at purchase time.

        public DateTime? SettlementTime {get;}

        public decimal LoanAmount { get; set; }

        public decimal PurchaseAdditionalCost{ get; set; }

        public decimal OffsetRatio { get; set; }

        public decimal? LoanTermYears { get; set; }

        public decimal AnnualInterestRate { get; set; }   // e.g. 0.05 for 5%

        public Action<LoanContract, ContractExecutor, decimal>? OnSettlement { get; set; }

        public LoanContract(Loan loanAccount, Property? property, decimal? deposit, DateTime? settlementTime, decimal loanAmount) 
            : base(GetStartTime(property, deposit, settlementTime), loanAccount, 
            string.IsNullOrEmpty(loanAccount.Name) ? (property is null? "Loan" : $"Loan for {property?.Name}")  : loanAccount.Name)
        {
            Deposit = deposit;
            SettlementTime = settlementTime ?? property!.Schedule!.StartTime!.Value;
            Property = property;
            LoanAmount = loanAmount;
            PurchaseAdditionalCost = property?.PurchaseAdditionalCost ?? 0;
            System.Diagnostics.Debug.Assert(deposit is not null && SettlementTime is not null && Property!.Schedule!.PurchaseTime <= SettlementTime || deposit is null);
        }

        private static DateTime GetStartTime(Property? property, decimal? deposit, DateTime? settlementTime)
        {
            if(deposit is not null)
            {
                var purchaseTime = property!.Schedule!.PurchaseTime;
                return purchaseTime;
            }
            return settlementTime ?? property!.Schedule!.StartTime!.Value;
        }

        protected override (DateTime processedTime, DateTime? bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            if (Deposit is not null)
            {
                var purchaseTime = Property!.Schedule!.PurchaseTime;
                if (currentTime == purchaseTime)
                {
                    System.Diagnostics.Debug.Assert(purchaseTime <= SettlementTime);
                    executor.ExecuteTransaction(CashAccount, -Deposit.Value, this, $"Deposit for {Name}");
                    if (purchaseTime < SettlementTime)
                    {
                        return (currentTime, SettlementTime!.Value);
                    }
                }
            }

            if (currentTime == SettlementTime)
            {
                var totalFundsRequired = (Property?.Schedule?.PurchasePrice??0) + PurchaseAdditionalCost - (Deposit ?? 0);
                var cashRequired = totalFundsRequired - LoanAmount;
                executor.ExecuteTransaction(CashAccount, -cashRequired, this, $"Settlement for {Name}");

                OnSettlement?.Invoke(this, executor, -cashRequired);
                
                executor.ExecuteTransaction(Account!, -LoanAmount, this, $"Loan amount for {Name}");

                return (currentTime, currentTime.AddMonths(1));
            }
            else
            {
                // Update in every iteration of Execution() call, as the conditions may change for each iteration.
                if (LoanAmount == 102667)
                {
                    LoanAmount = LoanAmount;
                }
                DateTime newTime;
                while (true)
                {
                    newTime = lastProcessedTime!.Value.AddMonths(1);
                    if (newTime > currentTime)
                    {
                        break;
                    }
                    ApplyRepayment(executor, newTime - lastProcessedTime!.Value);
                    lastProcessedTime = newTime;
                }
                return (lastProcessedTime!.Value, newTime);
            }
        }

        private decimal CalculateMonthlyPayment()
        {
            double monthlyRate = (double)AnnualInterestRate / 12 ;  // e.g. 5.55 -> 0.004625
            double numPayments = (double)LoanTermYears! * 12;

            if (monthlyRate == 0) return (decimal)((double)LoanAmount / numPayments);

            double power = Math.Pow(1 + monthlyRate, numPayments);
            return (decimal)((double)LoanAmount * (monthlyRate * power) / (power - 1));
        }
        // private decimal CalculateMonthlyPayment()  // for recalc on existing loans
        // {
        //     double r = (double)AnnualInterestRate / 12.0;
        //     double n = (double)LoanTermYears! * 12;

        //     if (Math.Abs(r) < 0.000001)
        //         return (decimal)Math.Ceiling( (double)LoanAmount / n * 100) / 100;

        //     double power = Math.Pow(1 + r, n);
        //     double payment = (double)LoanAmount * (r * power) / (power - 1);

        //     // ANZ-like adjustments
        //     payment = Math.Round(payment, 2);           // bank rounding
        //     // payment = Math.Floor(payment);           // sometimes they floor it

        //     return (decimal)payment;
        // }

        private void ApplyRepayment(ContractExecutor executor, TimeSpan time)
        {
            var fractionOfYear = time.Days / Constants.DaysPerYear;

            var interestApplicable = Math.Max(0, (-Account!.Balance) - CashAccount.Balance * OffsetRatio);  // Assuming the offset account reduces the interest applied on the loan balance.
            var interest = AnnualInterestRate * fractionOfYear * interestApplicable;

            if (LoanTermYears.HasValue)
            {
                var currentMonthlyPayment = CalculateMonthlyPayment();
                var principalPayment = currentMonthlyPayment * fractionOfYear * 12 - interest;
                if (Account!.Balance + principalPayment > 0)  // If the calculated principal payment exceeds the remaining balance, adjust it to only pay off the remaining balance.
                {
                    principalPayment = -Account.Balance;
                }

                executor.ExecuteTransaction(CashAccount, -(interest + principalPayment), this, $"P+I repayment for {Name}");
                executor.ExecuteTransaction(Account!, principalPayment, this, $"Principal payment for {Name}");

                executor.ChangeTrackers?[this, ChangeTrackerPaidPrincipal].TrackChange(-principalPayment);
            }
            else
            {
                executor.ExecuteTransaction(CashAccount, -interest, this, $"Interest payment for {Name}");
            }

            executor.ChangeTrackers?[this, ChangeTrackerPaidInterest].TrackChange(-interest);
        }
    }
}