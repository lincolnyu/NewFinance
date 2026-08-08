namespace NewFinance.Core
{
    public class ChangeTrackers
    {
        private readonly Dictionary<ITrackerKey, Tracker> _trackers = [];

        public Tracker this[ITrackerKey key] => GetOrCreateTracker(key);

        public bool TryGetTracker(ITrackerKey key, out Tracker? tracker)
        {
            return _trackers.TryGetValue(key, out tracker);
        }

        public Tracker GetOrCreateTracker(ITrackerKey key)
        {
            if (!_trackers.TryGetValue(key, out var tracker))
            {
                tracker = new Tracker() { Key = key };
                _trackers[key] = tracker;
            }
            return tracker;
        }

        public void CreateTracker(ITrackerKey key)
        {
            if (!_trackers.ContainsKey(key))
            {
                _trackers[key] = new Tracker() { Key = key };
            }
        }

        public void RemoveTracker(ITrackerKey key)
        {
            _trackers.Remove(key);
        }

        public void ClearTrackers()
        {
            _trackers.Clear();
        }

        public IEnumerable<(ITrackerKey key, Tracker tracker)> GetTrackers()
        {
            return _trackers.Select(kvp => (kvp.Key, kvp.Value));
        }

        public class Tracker// : IHasName
        {
            public class Subscription(decimal initialChange)
            {
                public decimal TrackedChange { get; private set; } = initialChange;

                public void AddChange(decimal change)
                {
                    TrackedChange += change;
                }

                public void Reset()
                {
                    TrackedChange = 0;
                }
            }

            private readonly Dictionary<object, Subscription> _subscriptions = new Dictionary<object, Subscription>();

            public required ITrackerKey Key {get;set;}

            public decimal TotalChange { get; private set; } = 0;

            public IEnumerable<(object subscriber, Subscription subscription)> GetSubscriptions()
            {
                return _subscriptions.Select(kvp => (kvp.Key, kvp.Value));
            }

            public Subscription this[object subscriber] => GetOrCreateSubscription(subscriber, true);

            public Subscription GetOrCreateSubscription(object subscriber, bool trackExistingChange)
            {
                if (!_subscriptions.TryGetValue(subscriber, out var tracker))
                {
                    tracker = new Subscription(trackExistingChange ? TotalChange : 0);
                    _subscriptions[subscriber] = tracker;
                }
                return tracker;
            }

            public void Subscribe(object subscriber, bool trackExistingChange)
            {
                if (!_subscriptions.ContainsKey(subscriber))
                {
                    _subscriptions[subscriber] = new Subscription(trackExistingChange ? TotalChange : 0);
                }
            }

            public void Unsubscribe(object subscriber)
            {
                _subscriptions.Remove(subscriber);
            }

            public void UnsubscribeAll()
            {
                _subscriptions.Clear();
            }

            public void ResetAll()
            {
                foreach (var tracker in _subscriptions.Values)
                {
                    tracker.Reset();
                }
            }

            public void TrackChange(decimal increase)
            {
                TotalChange += increase;
                foreach (var tracker in _subscriptions.Values)
                {
                    tracker.AddChange(increase);
                }
            }
        }   
    }
}
 