using System.Data;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class PostTaxCsvCollator : Contract
    {
        public PostTaxCsvCollator() : base(null, "Post Tax CSV Collator")
        {
        }

        public List<List<string>> Table { get; } = [];

        public List<(object, string)> ReportedItems {get;} = [];

        public HashSet<DateTime> AdditionalReportDates {get;} = new HashSet<DateTime>();

        public List<string> ColumnNames {get;} = [];

        public string GetColumnName((object, string) col)
        {
            if (col.Item1 is IHasName hasName && !string.IsNullOrWhiteSpace(hasName.Name))
            {
                return hasName.Name;
            }
            return col.Item2;
        }

        public override void Reset(ContractExecutor executor)
        {
            base.Reset(executor);

            Table.Clear();
            ColumnNames.Clear();
        }

        protected override (DateTime processedTime, DateTime? bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            if (currentTime.IsEOFY() || AdditionalReportDates.Contains(currentTime))
            {
                var populateColumNames = ColumnNames.Count == 0;

                var row = new List<string>() { currentTime.ToString("yyyy") };
                Console.WriteLine($"{currentTime:yyyy}:");
                foreach (var col in ReportedItems)
                {
                    if (col.Item1 is Entity entity)
                    {
                        foreach(var account in entity.Assets.Concat(entity.Liabilities))
                        {
                            if (account.Ownership.TryGetValue(entity, out var share))
                            {
                                Console.WriteLine($" '{account.Name}' balance = {account.Balance * share:0,000.00}");
                                row.Add((account.Balance * share).ToString("0.00"));
                            }
                            else
                            {
                                Console.WriteLine($" '{account.Name}' balance = {account.Balance:0,000.00}");
                                row.Add(account.Balance.ToString("0.00"));
                            }
                            if (populateColumNames)
                            {
                                ColumnNames.Add($"{entity.Name}.{account.Name}");
                            }
                        }
                    }
                    if (col.Item1 is IHasBalance balanceItem)
                    {
                        var name = GetColumnName(col);
                        Console.WriteLine($" '{name}' balance = {balanceItem.Balance:0,000.00}");
                        row.Add(balanceItem.Balance.ToString("0.00"));
                        if (populateColumNames)
                        {
                            ColumnNames.Add(name);
                        }
                    }
                    else if (col.Item1 is ChangeTracker tracker)
                    {
                        var name = GetColumnName(col);
                        if (name.EndsWith("total"))
                        {
                            var val = tracker["PostTaxCsvCollator-total"].TrackedChange;
                            Console.WriteLine($" '{name}' = {val:0,000.00}");
                            row.Add(val.ToString("0.00"));
                        }
                        else
                        {
                            var val = tracker["PostTaxCsvCollator"].GetTrackedChangeAndReset();
                            Console.WriteLine($" '{name}' = {val:0,000.00}");
                            row.Add(val.ToString("0.00"));
                        }
                        if (populateColumNames)
                        {
                            ColumnNames.Add(name);
                        }
                    }
                }
                Table.Add(row);
            }

            var nextEOFY = currentTime.NextEOFY();
            return (currentTime, nextEOFY);
        }
    }
}