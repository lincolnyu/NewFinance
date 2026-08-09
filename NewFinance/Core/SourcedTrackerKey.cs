namespace NewFinance.Core
{
    public record SourcedTrackerKey(object Source, string Name) : ITrackerKey
    {
        public bool Equals(ITrackerKey? other)
        {
            if (other is not SourcedTrackerKey key)
            {
                return false;
            }
            return Source == key.Source && Name == key.Name;
        }
    }
}