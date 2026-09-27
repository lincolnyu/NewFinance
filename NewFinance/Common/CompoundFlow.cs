using NewFinance.Core;

namespace NewFinance.Common;

/// <summary>
///     Keeps the bound account marked to market as a holding of an asset whose price compounds over time.
///     <para>
///         It owns a price index (starting at 1) that grows at an effective annual rate, and revalues the account on every
///         execution so the balance is current whenever other contracts read it. It does not deal with cash flows such as
///         yield, fees or contributions, nor with units, lots, cost base or tax; those belong to other contracts, which
///         may freely deposit into or withdraw from the account.
///     </para>
///     <para>
///         The price is worked out from an anchor (time and price at the last rate change) rather than from the previous
///         execution, so the number of executions does not affect the result. Units are the balance held at the end of
///         the last iteration divided by the price, so flows posted by other contracts start growing from the time they
///         are posted, regardless of execution order.
///     </para>
/// </summary>
/// <param name="initialValue">The value deposited into the account at <paramref name="startTime" />.</param>
/// <param name="getGrowthRate">
///     The effective annual growth rate (e.g. 0.05 for 5%) given the current value of the account. It is re-evaluated on
///     every execution and applies from then until the next execution.
/// </param>
/// <param name="timeStep">
///     The interval at which this contract books its own executions, anchored to the start time. It does not affect the
///     accuracy of the value, which is exact whenever the contract is executed.
/// </param>
public class CompoundFlow(
    DateTime startTime,
    decimal initialValue,
    Func<decimal, decimal> getGrowthRate,
    TimeSpan timeStep,
    Account account,
    string name) : AccountBindingContract(startTime, account, name)
{
    private decimal _anchorPrice;
    private DateTime _anchorTime;
    private decimal _rate;

    public TimeSpan Step { get; } = timeStep > TimeSpan.Zero
        ? timeStep
        : throw new ArgumentOutOfRangeException(nameof(timeStep), "Time step must be positive.");

    public decimal CurrentPricePerShare { get; private set; } = 1m;

    /// <summary>
    ///     Units held as of the end of the last iteration, i.e. the balance divided by the price.
    /// </summary>
    public decimal Units { get; private set; }

    protected override (DateTime processedTime, DateTime? bookedTime) Execute(ContractExecutor executor,
        DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
    {
        if (lastProcessedTime is null)
        {
            executor.ExecuteTransaction(Account!, initialValue, this, $"Initial value for {Name}");
            Reanchor(currentTime, getGrowthRate(Account!.Balance));
            return (currentTime, NextBookedTime(currentTime));
        }

        var price = PriceAt(currentTime);
        var growth = Units * (price - CurrentPricePerShare);
        CurrentPricePerShare = price;
        if (growth != 0) executor.ExecuteTransaction(Account!, growth, this, $"Growth for {Name}");

        var rate = getGrowthRate(Account!.Balance);
        if (rate != _rate) Reanchor(currentTime, rate);

        return (currentTime, NextBookedTime(currentTime));
    }

    public override void PostExecute()
    {
        // Only once started; the price is positive from then on.
        if (LastProcessedTime is not null) Units = Account!.Balance / CurrentPricePerShare;
        base.PostExecute();
    }

    private void Reanchor(DateTime time, decimal rate)
    {
        if (rate <= -1)
            throw new InvalidOperationException($"Growth rate {rate} for {Name} must be greater than -100%.");
        _anchorTime = time;
        _anchorPrice = CurrentPricePerShare;
        _rate = rate;
    }

    private decimal PriceAt(DateTime time)
    {
        var years = (time - _anchorTime).TotalDays / (double)Constants.DaysPerYear;
        return _anchorPrice * (decimal)Math.Pow(1 + (double)_rate, years);
    }

    private DateTime NextBookedTime(DateTime currentTime)
    {
        var steps = (currentTime - StartTime!.Value).Ticks / Step.Ticks + 1;
        return StartTime.Value.AddTicks(Step.Ticks * steps);
    }
}