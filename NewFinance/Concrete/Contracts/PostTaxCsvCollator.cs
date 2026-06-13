using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class PostTaxCsvCollator : Contract
    {
        public PostTaxCsvCollator(Account account) : base(null)
        {
            CashAccount = account;
        }

        public List<List<string>> Table { get; } = [];

        public Account CashAccount {get;}

        protected override (DateTime processedTime, DateTime bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            if (currentTime.IsEOFY())
            {
                Console.WriteLine($"Current time: {currentTime}: Cash account balance: {CashAccount.Balance: 0,000.00}");
            }

            var nextEOFY = currentTime.NextEOFY();
            return (currentTime, nextEOFY);
        }
    }
}