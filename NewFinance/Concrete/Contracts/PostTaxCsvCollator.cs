using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class PostTaxCsvCollator : Contract
    {
        public PostTaxCsvCollator() : base(null, "Post Tax CSV Collator")
        {
        }

        public List<List<string>> Table { get; } = [];

        public List<IHasName> ColumnItems {get;} = [];

        protected override (DateTime processedTime, DateTime bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            if (currentTime.IsEOFY())
            {
                var row = new List<string>() { currentTime.ToString("yyyy-MM-dd") };
                Console.WriteLine($"{currentTime:yyyy-MM-dd}:");
                foreach (var col in ColumnItems)
                {
                    if (col is Account account)
                    {
                        Console.WriteLine($" '{account.Name}' balance = {account.Balance:0,000.00}");
                        row.Add(account.Balance.ToString("0.00"));
                    }
                    else if (col is ChangeTracker tracker)
                    {
                        var val = tracker["PostTaxCsvCollator"].GetTrackedChangeAndReset(); 
                        Console.WriteLine($" '{tracker.Name}' change = {val:0,000.00}");
                        row.Add(val.ToString("0.00"));
                    }
                }
                Table.Add(row);
            }

            var nextEOFY = currentTime.NextEOFY();
            return (currentTime, nextEOFY);
        }
    }
}