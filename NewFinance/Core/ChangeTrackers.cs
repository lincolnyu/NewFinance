namespace NewFinance.Core
{
    public class ChangeTrackers
    {
        private readonly Dictionary<(object?, string), Tracker> _trackers = new Dictionary<(object?, string), Tracker>();

        public Tracker this[object? obj, string name] => GetOrCreateTracker(obj, name);

        public bool TryGetTracker(object? obj, string name, out Tracker? tracker)
        {
            return _trackers.TryGetValue((obj, name), out tracker);
        }

        public Tracker GetOrCreateTracker(object? obj, string name)
        {
            if (!_trackers.TryGetValue((obj, name), out var tracker))
            {
                tracker = new Tracker() { Name = name };
                _trackers[(obj, name)] = tracker;
            }
            return tracker;
        }

        public void CreateTracker(object? obj, string name)
        {
            if (!_trackers.ContainsKey((obj, name)))
            {
                _trackers[(obj, name)] = new Tracker() { Name = name };
            }
        }

        public void RemoveTracker(object? obj, string name)
        {
            _trackers.Remove((obj, name));
        }

        public void ClearTrackers()
        {
            _trackers.Clear();
        }

        public IEnumerable<(object? obj, string name, Tracker tracker)> GetTrackers()
        {
            return _trackers.Select(kvp => (kvp.Key.Item1, kvp.Key.Item2, kvp.Value));
        }

        public class Tracker : IHasName
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

            private readonly Dictionary<object, Subscription> _trackers = new Dictionary<object, Subscription>();

            public string Name {get;set;} = "";

            public decimal TotalChange { get; private set; } = 0;

            public IEnumerable<(object subscriber, Subscription subscription)> GetSubscriptions()
            {
                return _trackers.Select(kvp => (kvp.Key, kvp.Value));
            }

            public Subscription this[object subscriber] => GetOrCreateTracker(subscriber);

            public Subscription GetOrCreateTracker(object subscriber)
            {
                if (!_trackers.TryGetValue(subscriber, out var tracker))
                {
                    tracker = new Subscription(TotalChange);
                    _trackers[subscriber] = tracker;
                }
                return tracker;
            }

            public void CreateTracker(object subscriber, bool trackExistingChange)
            {
                if (!_trackers.ContainsKey(subscriber))
                {
                    _trackers[subscriber] = new Subscription(trackExistingChange ? TotalChange : 0);
                }
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

            public void TrackChange(decimal increase)
            {
                TotalChange += increase;
                foreach (var tracker in _trackers.Values)
                {
                    tracker.AddChange(increase);
                }
            }
        }   
    }
}
 