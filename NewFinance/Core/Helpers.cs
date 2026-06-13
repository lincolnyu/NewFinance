namespace NewFinance.Core
{
    public static class Helpers
    {
        public static void AddAsset(this Entity entity, Account account, decimal ownershipFraction)
        {
            entity.Assets.Add(account);
            account.Ownership.Add((entity, ownershipFraction));
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

        public static DateTime ExecuteContracts(this ContractExecutor executor, IEnumerable<Contract> contracts, DateTime currentTime)
        {
            DateTime minNextTime = DateTime.MaxValue;

            foreach (var contract in contracts)
            {
                var nextTime = contract.Execute(executor, currentTime);
                if (nextTime < minNextTime)
                {
                    minNextTime = nextTime;
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
    }
}