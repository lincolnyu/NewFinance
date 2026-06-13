using NewFinance.Common;
using NewFinance.Concrete.Accounts;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class LoanContract : AccountBindingContract
    {
        public Property Property { get; private set; }

        public required Account CashAccount { get; set; }

        public decimal LoanAmount { get; set; }

        public decimal PurchaseAdditionalCost{ get; set; }

        public decimal OffsetRatio { get; set; }

        public decimal AnnualPrincipalPayment { get; set; }

        public decimal AnnualInterestRate { get; set; }   // e.g. 0.05 for 5%

        public decimal YearToDateInterestPaid { get; private set; }

        public LoanContract(Loan loanAccount, Property property, decimal loanAmount, decimal purchaseAdditionalCost) : base(property.Schedule.StartTime!.Value, loanAccount)
        {
            Property = property;
            LoanAmount = loanAmount;
            PurchaseAdditionalCost = purchaseAdditionalCost;
        }

        protected override (DateTime processedTime, DateTime bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            if (currentTime == StartTime)
            {
                ExecuteSettlement();
                return (currentTime, currentTime.AddMonths(1));
            }
            else
            {
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
            var totalCost = Property.Balance + PurchaseAdditionalCost;
            var cashRequired = totalCost - LoanAmount;
            CashAccount.Balance -= cashRequired;
            
            Account!.Balance = LoanAmount;
        }

        private void ApplyRepayment(TimeSpan time)
        {
            var fractionOfYear = time.Days / 365.25m;
            var interest = AnnualInterestRate * fractionOfYear;
            var principalPayment = AnnualPrincipalPayment * fractionOfYear; // Assuming MonthlyPrincipalPayment is the payment for a full month.
            
            CashAccount.Balance -= interest + principalPayment;
            Account!.Balance -= principalPayment;

            YearToDateInterestPaid += interest;
        }
    }
}