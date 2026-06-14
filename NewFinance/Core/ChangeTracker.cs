
namespace NewFinance.Core
{
    public class ChangeTracker : IHasName
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
 