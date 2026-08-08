namespace NewFinance.Core
{
    public record TrackerKeyWithSource(object Source, string name) : ITrackerKey;
}