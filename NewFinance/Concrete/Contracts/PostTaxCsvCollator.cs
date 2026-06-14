using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class PostTaxCsvCollator : Contract
    {
        public PostTaxCsvCollator() : base(null, "Post Tax CSV Collator")
        {
        }

        public List<List<string>> Table { get; } = [];

        public List<(object, string)> ColumnItems {get;} = [];

        public string GetColumnName((object, string) col)
        {
            if (col.Item1 is IHasName hasName && !string.IsNullOrWhiteSpace(hasName.Name))
            {
                return hasName.Name;
            }
            return col.Item2;
        }

        protected override (DateTime processedTime, DateTime bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            if (currentTime.IsEOFY())
            {
                var row = new List<string>() { currentTime.ToString("yyyy") };
                Console.WriteLine($"{currentTime:yyyy}:");
                foreach (var col in ColumnItems)
                {
                    if (col.Item1 is IHasBalance balanceItem)
                    {
                        var name = GetColumnName(col);
                        Console.WriteLine($" '{name}' balance = {balanceItem.Balance:0,000.00}");
                        row.Add(balanceItem.Balance.ToString("0.00"));
                    }
                    else if (col.Item1 is ChangeTracker tracker)
                    {
                        var val = tracker["PostTaxCsvCollator"].GetTrackedChangeAndReset(); 
                        var name = GetColumnName(col);
                        Console.WriteLine($" '{name}' change = {val:0,000.00}");
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