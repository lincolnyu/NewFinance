using System.Diagnostics;
using System.Reflection;

namespace NewFinance.Core
{
    public static class Helpers
    {
        public const string ExpectedTrackerKeySuffix = "TrackerKey";

        public static SourcedTrackerKey CreateTrackerKeyWithSource(this object source, string name) => new SourcedTrackerKey(source, name);

        public static T CreateNaturalTrackerKey<T>(this T source, string trackerKeyPropertyName) where T : Contract
        {
            var trackerKeyProperty = typeof(T).GetProperty(trackerKeyPropertyName, BindingFlags.Public | BindingFlags.Instance);
            CreateNaturalTrackerKey(source, trackerKeyProperty!);
            return source;
        }

        private static void CreateNaturalTrackerKey<T>(this T source, PropertyInfo trackerKeyProperty) where T : Contract
        {
            var trackerKeyPropertyName = trackerKeyProperty.Name;
            Debug.Assert(trackerKeyPropertyName.EndsWith(ExpectedTrackerKeySuffix));
            var trackerKeyName = trackerKeyPropertyName[..^ExpectedTrackerKeySuffix.Length].TrimEnd('_');
            var trackerKey = CreateTrackerKeyWithSource(source!, trackerKeyName);
            trackerKeyProperty!.SetValue(source, trackerKey);
        }

        public static T CreateAllNaturalTrackerKeys<T>(this T source) where T : Contract
        {
            foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.PropertyType.IsAssignableTo(typeof(ITrackerKey)) && property.Name.EndsWith(ExpectedTrackerKeySuffix))
                {
                    CreateNaturalTrackerKey(source, property);
                }
            }
            return source;
        }

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

        public static DateTime NextAnniversaryCrossing(this DateTime start, int month, int day)
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

        public static void ExecuteTransaction(this ContractExecutor executor, Account account, decimal amount, Contract contract, string name = "")
        {
            var transaction = new Account.Transaction(name) { Account = account, Amount = amount, Contract = contract };
            transaction.ExecuteAndRecord(executor);
        }

        public static ChangeTrackers.Tracker.Subscription? GetOrCreateTrackerSubscription(this ContractExecutor executor, ITrackerKey? trackerKey, object subscriber)
        {
            return trackerKey is not null ? executor.ChangeTrackers?[trackerKey][subscriber] : null;
        }

        public static decimal GetTrackedChangeAndReset(this ChangeTrackers.Tracker.Subscription subscription)
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