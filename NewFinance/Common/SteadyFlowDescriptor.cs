namespace NewFinance.Common
{
    public record struct SteadyFlowDescriptor(DateTime StartTime, List<(decimal Rate, DateTime EndTime)> Inflows);
}  