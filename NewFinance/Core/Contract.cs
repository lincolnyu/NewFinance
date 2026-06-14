namespace NewFinance.Core
{
    // Event is basically a financial contract or series of financial events projected to occur over time.
    public abstract class Contract(DateTime? startTime, string name) : IHasName
    {
        public DateTime? StartTime { get; set; } = startTime;

        public string Name { get; set; } = name;

        protected DateTime? LastProcessedTime { get; private set; }

        protected DateTime? LastBookedTime {get; private set;}

        public virtual DateTime Execute(ContractExecutor executor, DateTime currentTime)
        {
            // Keep booking start time until the current time reaches the start time, then execute the contract for the first time.
            // This guarantees that the contract will be executed at the start time if specified.
            // After the first execution, the following executions will be guaranteed to happen either at or before the booked time of their previous executions.
            // It's up to the Execute() method implementation to determine if a premature call should be executed and/or a booked time should be requested again.
            if (StartTime != null && currentTime < StartTime)
            {
                return StartTime.Value;
            }

            System.Diagnostics.Debug.Assert(LastBookedTime == null || currentTime <= LastBookedTime);

            // Processed time may not necessarily be the current time. It is the time that has been processed in the currennt execution so the next execution will know where to start.
            var (processedTime, bookedTime) = Execute(executor,  LastProcessedTime, LastBookedTime, currentTime);

            LastBookedTime = bookedTime;
            LastProcessedTime = processedTime;

            return bookedTime;
        }

        public virtual void Reset(ContractExecutor executor)
        {
            LastProcessedTime = null;
            LastBookedTime = null;
        }

        protected abstract (DateTime processedTime, DateTime bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime);
    }
}