namespace NewFinance.Concrete
{
    public static class ContractHelpers
    {
        public static DateTime RunPeriodic(TimeSpan period, DateTime lastProcessedTime, DateTime currentTime, Action action)
        {
            var time = lastProcessedTime.Add(period);
            while (time <= currentTime)
            {
                action();
                time = time.Add(period);
            }
            return time;
        }
    }
}