using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class OneOff(DateTime time, string name, Action oneOffAction) : Contract(time, name)
    {
        protected override (DateTime processedTime, DateTime? bookedTime) Execute(ContractExecutor executor, DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            if (currentTime == StartTime)
            {
                oneOffAction(); // Execute the provided action at the specified time.
                IsCompleted = true; // Mark the contract as completed after execution.
                return (currentTime, null);
            }
            else
            {
                throw new InvalidOperationException($"OneOff contract should only be executed at its start time {StartTime}, but got {currentTime}");
            }
        }
    }
}