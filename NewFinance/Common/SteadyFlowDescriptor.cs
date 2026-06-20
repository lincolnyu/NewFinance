namespace NewFinance.Common
{
    public record struct SteadyFlowDescriptor(DateTime StartTime, List<(decimal DailyRate, DateTime EndTime)> Inflows)
    {
        private DateTime? GetStartTime(int index)
        {
            if (index == 0)
            {
                return StartTime;
            }
            else
            {
                return index-1 <  Inflows.Count ? Inflows[index - 1].EndTime : (DateTime?)null;
            }
        }

        public SteadyFlowDescriptor Add(SteadyFlowDescriptor other)
        {
            var combinedInflows = Combine(other).Select(x => (DailyRate: (x.Rate1 ?? 0) + (x.Rate2 ?? 0), x.EndTIme)).ToList();
            return new SteadyFlowDescriptor(StartTime < other.StartTime ? StartTime : other.StartTime, combinedInflows);  
        }

        public IEnumerable<(decimal? Rate1, decimal? Rate2, DateTime StartTime, DateTime EndTIme)> Combine(SteadyFlowDescriptor other)
        {
            int thisBucketIndex = 0;
            int thatBucketIndex = 0;
            decimal? thisRate = null;
            decimal? thatRate = null;

            var thisBucketTime = GetStartTime(thisBucketIndex);
            var thatBucketTime = other.GetStartTime(thatBucketIndex);
            DateTime? currentStartTime;
            if (thisBucketTime < thatBucketTime)
            {
                thisRate = Inflows[thisBucketIndex].DailyRate;
                currentStartTime = thisBucketTime;
                thisBucketIndex++;
            }
            else if (thisBucketTime > thatBucketTime)
            {
                thatRate = other.Inflows[thatBucketIndex].DailyRate;
                currentStartTime = thatBucketTime;
                thatBucketIndex++;
            }
            else
            {
                thisRate = Inflows[thisBucketIndex].DailyRate;
                thatRate = other.Inflows[thatBucketIndex].DailyRate;
                currentStartTime = thisBucketTime; // or thatBucketTime, they are the same
                thisBucketIndex++;
                thatBucketIndex++;
            }

            for (; currentStartTime is not null && (thisRate is not null || thatRate is not null);)
            {
                thisBucketTime = GetStartTime(thisBucketIndex)?? DateTime.MaxValue;
                thatBucketTime = other.GetStartTime(thatBucketIndex)?? DateTime.MaxValue;

                if (thisBucketTime == thatBucketTime)
                {
                    yield return (thisRate, thatRate, currentStartTime.Value, thisBucketTime.Value);
                    currentStartTime = thisBucketTime;
                    thisBucketIndex++;
                    thatBucketIndex++;
                    thisRate = thisBucketIndex-1 < Inflows.Count ? Inflows[thisBucketIndex - 1].DailyRate : (decimal?)null;
                    thatRate = thatBucketIndex-1 < other.Inflows.Count ? other.Inflows[thatBucketIndex - 1].DailyRate : (decimal?)null;
                }
                else if (thisBucketTime < thatBucketTime)
                {
                    yield return (thisRate, thatRate, currentStartTime.Value, thisBucketTime.Value);
                    currentStartTime = thisBucketTime;
                    thisBucketIndex++;
                    thisRate = thisBucketIndex-1 < Inflows.Count ? Inflows[thisBucketIndex - 1].DailyRate : (decimal?)null    ;
                }
                else
                {
                    yield return (thisRate, thatRate, currentStartTime.Value, thatBucketTime!.Value);
                    currentStartTime = thatBucketTime;
                    thatBucketIndex++;
                    thatRate = thatBucketIndex-1 < other.Inflows.Count ? other.Inflows[thatBucketIndex - 1].DailyRate : (decimal?)null;
                }
            }
        }
    }
}  