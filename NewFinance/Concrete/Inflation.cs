namespace NewFinance.Concrete.Contracts
{
    public record struct Inflation(DateTime StartTime, IEnumerable<(decimal, DateTime)> Rates);
}