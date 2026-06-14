
namespace NewFinance.Core
{
    public class ChangeTracker
    {
        public class Subscription
        {
            public decimal TrackedChange { get; private set; }

            public void AddChange(decimal change)
            {
                TrackedChange += change;
            }

            public void Reset()
            {
                TrackedChange = 0;
            }
        }

        private readonly Dictionary<object, Subscription> _trackers = new Dictionary<object, Subscription>();

        public Subscription this[object subscriber] => GetOrCreateTracker(subscriber);

        public Subscription GetOrCreateTracker(object subscriber)
        {
            if (!_trackers.TryGetValue(subscriber, out var tracker))
            {
                tracker = new Subscription();
                _trackers[subscriber] = tracker;
            }
            return _trackers[subscriber];
        }

        public void RemoveTracker(object subscriber)
        {
            _trackers.Remove(subscriber);
        }

        public void ClearTrackers()
        {
            _trackers.Clear();
        }

        public void ResetAll()
        {
            foreach (var tracker in _trackers.Values)
            {
                tracker.Reset();
            }
        }

        public void TrackIncrease(decimal increase)
        {
            foreach (var tracker in _trackers.Values)
            {
                tracker.AddChange(increase);
            }
        }
    }   
}
 