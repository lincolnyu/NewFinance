using NewFinance.Common;
using NewFinance.Concrete.Accounts;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts;

/// <summary>
///     Schedule for investment. It tracks the value of the property, the yield and the fees.
/// </summary>
/// <param name="investment">The investment associated with this schedule.</param>
/// <param name="startTime">
///     The start time of this contract in simulation, which is the start of its value tracking. Normally no later than
///     yield start time which is set up separately.
///     It is usually the max of purchaseTime and simulation start time.
/// </param>
/// <param name="initialValue">The initial value of the investment (at the `startTime`).</param>
/// <param name="getGrowthRate">A function to get the growth rate of the investment.`</param>
public abstract class InvestmentSchedule(
    Investment investment,
    DateTime startTime,
    decimal initialValue,
    Func<decimal, decimal> getGrowthRate) : AggregatedContract(startTime, $"Schedule for {investment.Name}")
{
    public Investment Investment { get; } = investment;

    /// <summary>
    ///     The current value of the investment, which is a compound flow that grows over time based on the growth rate
    ///     function provided.
    /// </summary>
    public CompoundFlow Value { get; } = new(startTime, initialValue, getGrowthRate,
        TimeSpan.FromDays(365), investment, $"Value of {investment.Name}");

    protected abstract IEnumerable<Contract> SubContracts { get; }

    public override IEnumerable<Contract> ChildContracts => SubContracts.Prepend(Value);
}