using NewFinance.Common;
using NewFinance.Concrete.Accounts;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class LoanContract : AccountBindingContract
    {
        public Property? Property { get; private set; }

        public required Account CashAccount { get; set; }

        public (DateTime, decimal)? Deposit {get;}

        public DateTime? StartOrSettlementTime {get;}

        public decimal LoanAmount { get; set; }

        public decimal PurchaseAdditionalCost{ get; set; }

        public bool AlreadySettled { get; }

        public decimal OffsetRatio { get; set; }

        public decimal? LoanTermYears { get; set; }

        public decimal AnnualInterestRate { get; set; }   // e.g. 0.05 for 5%

        public ChangeTracker PaidInterestTracker { get; } = new ChangeTracker();

        public ChangeTracker PaidPrincipalTracker { get; } = new ChangeTracker();

        public Action? OnStart { get; set; }

        public LoanContract(Loan loanAccount, Property? property, (DateTime, decimal)? deposit, DateTime? startOrSettlemntTime, decimal loanAmount, bool alreadySettled) : base(deposit?.Item1 ?? startOrSettlemntTime ?? property!.Schedule!.StartTime!.Value, loanAccount, 
            string.IsNullOrEmpty(loanAccount.Name) ? (property is null? "Loan" : $"Loan for {property?.Name}")  : loanAccount.Name)
        {
            Deposit = deposit;
            StartOrSettlementTime = startOrSettlemntTime ?? property!.Schedule!.StartTime!.Value;
            System.Diagnostics.Debug.Assert(deposit is not null && StartOrSettlementTime is not null && StartOrSettlementTime > deposit.Value.Item1 || deposit is null);
            Property = property;
            LoanAmount = loanAmount;
            PurchaseAdditionalCost = property?.PurchaseAdditionalCost ?? 0;
            AlreadySettled = alreadySettled;
        }

        protected override (DateTime processedTime, DateTime? bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            if (currentTime == Deposit?.Item1)
            {
                executor.ExecuteTransaction(CashAccount, -Deposit.Value.Item2, this, $"Deposit for {Name}");
                return (currentTime, StartOrSettlementTime!.Value);
            }

            if (currentTime == StartOrSettlementTime)
            {
                if (!AlreadySettled)
                {
                    ExecuteSettlement(executor);
                }
                executor.ExecuteTransaction(Account!, -LoanAmount, this, $"Loan amount for {Name}");
                OnStart?.Invoke();

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

        private void ExecuteSettlement(ContractExecutor executor)
        {
            var totalFundsRequired = (Property?.Balance??0) + PurchaseAdditionalCost - (Deposit?.Item2 ?? 0);
            var cashRequired = totalFundsRequired - LoanAmount;
            executor.ExecuteTransaction(CashAccount, -cashRequired, this, $"Settlement for {Name}");
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

                PaidPrincipalTracker.TrackChange(-principalPayment);
            }
            else
            {
                executor.ExecuteTransaction(CashAccount, -interest, this, $"Interest payment for {Name}");
            }

            PaidInterestTracker.TrackChange(-interest);
        }
    }
}