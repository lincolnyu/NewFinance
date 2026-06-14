namespace NewFinance.Concrete.Contracts
{
    public record struct Inflation(DateTime StartTime, IList<(decimal, DateTime)> Rates);
}