namespace NewFinance.Concrete;

public record struct Inflation(DateTime StartTime, IList<(decimal, DateTime)> Rates);