using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class PostTaxCsvCollator(TextWriter? writer = null) : Contract(null, "Post Tax CSV Collator")
    {
        private readonly TextWriter? _writer = writer;

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
            var isAdditionalReportDate = AdditionalReportDates.Contains(currentTime);
            if (currentTime.IsEOFY() || isAdditionalReportDate)
            {
                var populateColumNames = ColumnNames.Count == 0;

                var row = new List<string>() {  
                    isAdditionalReportDate? 
                        currentTime.ToString("yyyy-MM-dd") : 
                        $"EOF {currentTime.Year}"
                };
                _writer?.WriteLine($"{currentTime:yyyy}:");
                foreach (var col in ReportedItems)
                {
                    if (col.Item1 is Entity entity)
                    {
                        foreach(var account in entity.Assets.Concat(entity.Liabilities))
                        {
                            if (account.Ownership.TryGetValue(entity, out var share))
                            {
                                _writer?.WriteLine($" '{account.Name}' balance = {account.Balance * share:0,000.00}");
                                row.Add((account.Balance * share).ToString("0.00"));
                            }
                            else
                            {
                                _writer?.WriteLine($" '{account.Name}' balance = {account.Balance:0,000.00}");
                                row.Add(account.Balance.ToString("0.00"));
                            }
                            if (populateColumNames)
                            {
                                ColumnNames.Add($"{entity.Name}.{account.Name}");
                            }
                        }
                    }
                    else if (col.Item1 is IHasBalance balanceItem)
                    {
                        var name = GetColumnName(col);
                        _writer?.WriteLine($" '{name}' balance = {balanceItem.Balance:0,000.00}");
                        row.Add(balanceItem.Balance.ToString("0.00"));
                        if (populateColumNames)
                        {
                            ColumnNames.Add(name);
                        }
                    }
                    else if (col.Item1 is ChangeTracker tracker)
                    {
                        var name = GetColumnName(col);
                        if (name.EndsWith("ITD"))
                        {
                            var val = tracker["PostTaxCsvCollator-ITD"].TrackedChange;
                            _writer?.WriteLine($" '{name}' = {val:0,000.00}");
                            row.Add(val.ToString("0.00"));
                        }
                        else
                        {
                            var val = tracker["PostTaxCsvCollator"].GetTrackedChangeAndReset();
                            _writer?.WriteLine($" '{name}' = {val:0,000.00}");
                            row.Add(val.ToString("0.00"));
                        }
                        if (populateColumNames)
                        {
                            ColumnNames.Add(name);
                        }
                    }
                    else if (col.Item1 is Func<int, int, string> cellWriter)
                    {
                        row.Add(cellWriter(Table.Count, row.Count));
                        ColumnNames.Add(col.Item2);
                    }
                    else
                    {
                        throw new Exception($"Unsupported column type {col.Item1.GetType()}");
                    }
                }
                Table.Add(row);
            }

            var nextEOFY = currentTime.NextEOFY();
            return (currentTime, nextEOFY);
        }
    }
}