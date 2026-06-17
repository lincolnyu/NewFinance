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

        public decimal AnnualPrincipalPayment { get; set; }

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

        override public void Reset(ContractExecutor executor)
        {
            base.Reset(executor);

            Account!.Balance = 0;
        }

        protected override (DateTime processedTime, DateTime? bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            if (currentTime == Deposit?.Item1)
            {
                Account!.Balance = -Deposit.Value.Item2;
                CashAccount.Balance += Deposit.Value.Item2;
                return (currentTime, StartOrSettlementTime!.Value);
            }

            if (currentTime == StartOrSettlementTime)
            {
                if (!AlreadySettled)
                {
                    ExecuteSettlement();
                }
                Account!.Balance = -LoanAmount;

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
                    ApplyRepayment(newTime - lastProcessedTime!.Value);    
                    lastProcessedTime = newTime;
                }
                return (lastProcessedTime!.Value, newTime);
            }
        }

        private void ExecuteSettlement()
        {
            var totalFundsRequired = (Property?.Balance??0) + PurchaseAdditionalCost - (Deposit?.Item2 ?? 0);
            var cashRequired = totalFundsRequired - LoanAmount;
            CashAccount.Balance -= cashRequired;
        }

        private void ApplyRepayment(TimeSpan time)
        {
            var fractionOfYear = time.Days / Constants.DaysPerYear;
            var principalPayment = Math.Max(0, Math.Min(-Account!.Balance, AnnualPrincipalPayment * fractionOfYear)); // Assuming MonthlyPrincipalPayment is the payment for a full month.

            var interestApplicable = Math.Max(0, (-Account!.Balance) - CashAccount.Balance * OffsetRatio);  // Assuming the offset account reduces the interest applied on the loan balance.
            var interest = AnnualInterestRate * fractionOfYear * interestApplicable;
            
            CashAccount.Balance -= interest + principalPayment;
            Account!.Balance += principalPayment;

            PaidInterestTracker.TrackChange(-interest);
            PaidPrincipalTracker.TrackChange(-principalPayment);
        }
    }
}