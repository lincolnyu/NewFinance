namespace NewFinance.Core
{
    public static class Helpers
    {
        public static void AddAsset(this Entity entity, Account account, decimal ownershipFraction)
        {
            entity.Assets.Add(account);
            account.Ownership.Add(entity, ownershipFraction);
        }

        public static void AddLiability(this Entity entity, Account account, decimal ownershipFraction)
        {
            entity.Liabilities.Add(account);
            account.Ownership.Add(entity, ownershipFraction);
        }

        public static DateTime NextAnniversayCrossing(this DateTime start, int month, int day)
        {
            DateTime candidate = new DateTime(start.Year, month, day);
            if (candidate <= start)
            {
                candidate = new DateTime(start.Year + 1, month, day);
            }
            return candidate;
        }

        public static DateTime Min(DateTime requestedTimePropertyValue, DateTime requestedTimeRentalIncome)
        {
            return requestedTimePropertyValue < requestedTimeRentalIncome ? requestedTimePropertyValue : requestedTimeRentalIncome;
        }

        public static DateTime? ExecuteContracts(this ContractExecutor executor, IEnumerable<Contract> contracts, DateTime currentTime)
        {
            DateTime? minNextTime = null;

            foreach (var contract in contracts)
            {
                var nextTime = contract.Execute(executor, currentTime);
                if (nextTime is not null && (minNextTime is null || nextTime < minNextTime))
                {
                    minNextTime = nextTime.Value;
                }
            }

            return minNextTime;
        }

        public static decimal GetTrackedChangeAndReset(this ChangeTracker.Subscription subscription)
        {
            var change = subscription.TrackedChange;
            subscription.Reset();
            return change;
        }

        public static bool IsEOFY(this DateTime time)
        {
            return time.Month == 6 && time.Day == 30;
        }


        public static DateTime CurrentBOFY(this DateTime time)
        {
            var boef = new DateTime(time.Month > 6 || (time.Month == 6 && time.Day == 30) ? time.Year : time.Year - 1, 7, 1);
            return boef;
        }

        public static DateTime NextEOFY(this DateTime time)
        {
            var nextEofy = new DateTime(time.Month > 6 || (time.Month == 6 && time.Day == 30) ? time.Year + 1 : time.Year, 6, 30);
            return nextEofy;
        }
    }
}