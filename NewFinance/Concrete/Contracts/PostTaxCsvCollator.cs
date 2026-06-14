using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class PostTaxCsvCollator : Contract
    {
        public PostTaxCsvCollator() : base(null, "Post Tax CSV Collator")
        {
        }

        public List<List<string>> Table { get; } = [];

        public List<Account> Accounts {get;} = [];

        protected override (DateTime processedTime, DateTime bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            if (currentTime.IsEOFY())
            {
                var row = new List<string>() { currentTime.ToString("yyyy-MM-dd") };
                Console.WriteLine($"{currentTime:yyyy-MM-dd}:");
                foreach (var account in Accounts)
                {
                    Console.WriteLine($" '{account.Name}' balance = {account.Balance:0,000.00}");
                    row.Add(account.Balance.ToString("0.00"));
                }
                Table.Add(row);
            }

            var nextEOFY = currentTime.NextEOFY();
            return (currentTime, nextEOFY);
        }
    }
}