namespace NewFinance.Common
{
    public record struct SteadyFlowDescriptor(DateTime StartTime, List<(decimal DailyRate, DateTime EndTime)> Inflows);
}  