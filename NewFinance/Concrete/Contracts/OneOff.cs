using NewFinance.Common;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class OneOff : AccountBindingContract
    {
        public OneOff(DateTime time, Account account, decimal amount, string name) : base(time, account, name)
        {
            Amount=amount;
        }

        public decimal Amount { get; }

        protected override (DateTime processedTime, DateTime? bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            if (currentTime == StartTime)
            {
                Account!.Balance += Amount;; // No actual flow, just to trigger the contract execution at the right time.
                return (currentTime, null);
            }
            else
            {
                throw new InvalidOperationException($"OneOff contract should only be executed at its start time {StartTime}, but got {currentTime}");
            }
        }
    }
}