namespace NewFinance.Tests;

using NewFinance.Common;
using NewFinance.Core;

public class CompoundFlowTests
{
    private static readonly DateTime Start = new(2025, 1, 1);
    private static readonly TimeSpan Yearly = TimeSpan.FromDays(365);

    // Wakes the executor at the given times and optionally posts flows, standing in for other contracts.
    private sealed class ScriptedContract(DateTime start, IEnumerable<DateTime> times,
        Action<ContractExecutor, DateTime>? action = null) : Contract(start, "Scripted")
    {
        private readonly List<DateTime> _times = times.OrderBy(t => t).ToList();

        protected override (DateTime processedTime, DateTime? bookedTime) Execute(ContractExecutor executor,
            DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
        {
            if (_times.Contains(currentTime)) action?.Invoke(executor, currentTime);
            var next = _times.FirstOrDefault(t => t > currentTime);
            return (currentTime, next == default ? null : next);
        }
    }

    private static DateTime? RunUntil(ContractExecutor executor, DateTime end)
    {
        DateTime? next = Start;
        while (next is not null && next <= end) next = executor.Execute(next.Value);
        return next;
    }

    private static IEnumerable<DateTime> Daily(DateTime from, DateTime to)
    {
        for (var t = from; t <= to; t = t.AddDays(1)) yield return t;
    }

    private static decimal Grow(decimal value, decimal rate, DateTime from, DateTime to) =>
        value * (decimal)Math.Pow(1 + (double)rate, (to - from).TotalDays / (double)Constants.DaysPerYear);

    private static (Account account, CompoundFlow flow) CreateFlow(decimal initialValue, decimal rate,
        TimeSpan? step = null)
    {
        var account = new Account("Asset");
        var flow = new CompoundFlow(Start, initialValue, _ => rate, step ?? Yearly, account, "Asset");
        return (account, flow);
    }

    [Fact]
    public void ConstantRate_GrowsAsEffectiveAnnualRate()
    {
        var (account, flow) = CreateFlow(100_000m, 0.05m);
        var executor = new ContractExecutor();
        executor.Contracts.Add(flow);

        var end = Start.AddYears(1);
        RunUntil(executor, end);

        Assert.Equal(Math.Round(Grow(100_000m, 0.05m, Start, end), 6), Math.Round(account.Balance, 6));
        Assert.Equal(Math.Round(Grow(1m, 0.05m, Start, end), 12), Math.Round(flow.CurrentPricePerShare, 12));
    }

    [Fact]
    public void DailyExecution_MatchesYearlyExecution()
    {
        var end = Start.AddYears(10);

        var (yearlyAccount, yearlyFlow) = CreateFlow(100_000m, 0.07m);
        var yearly = new ContractExecutor();
        yearly.Contracts.Add(yearlyFlow);
        yearly.Contracts.Add(new ScriptedContract(Start, [end]));
        RunUntil(yearly, end);

        var (dailyAccount, dailyFlow) = CreateFlow(100_000m, 0.07m);
        var daily = new ContractExecutor();
        daily.Contracts.Add(dailyFlow);
        daily.Contracts.Add(new ScriptedContract(Start, Daily(Start, end)));
        RunUntil(daily, end);

        Assert.Equal(Math.Round(Grow(100_000m, 0.07m, Start, end), 6), Math.Round(dailyAccount.Balance, 6));
        Assert.Equal(Math.Round(yearlyAccount.Balance, 6), Math.Round(dailyAccount.Balance, 6));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ExternalDeposit_GrowsFromDepositTime_RegardlessOfOrder(bool depositorFirst)
    {
        var (account, flow) = CreateFlow(100_000m, 0.05m);
        var depositTime = new DateTime(2025, 7, 1);
        var depositor = new ScriptedContract(Start, [depositTime],
            (executor, _) => executor.ExecuteTransaction(account, 10_000m, flow, "Deposit"));

        var executor = new ContractExecutor();
        if (depositorFirst) executor.Contracts.Add(depositor);
        executor.Contracts.Add(flow);
        if (!depositorFirst) executor.Contracts.Add(depositor);

        var end = Start.AddYears(1);
        RunUntil(executor, end);

        var expected = Grow(100_000m, 0.05m, Start, end) + Grow(10_000m, 0.05m, depositTime, end);
        Assert.Equal(Math.Round(expected, 6), Math.Round(account.Balance, 6));
    }

    [Fact]
    public void NestedInAggregatedContract_TracksExternalFlows()
    {
        var (account, flow) = CreateFlow(100_000m, 0.05m);
        var depositTime = new DateTime(2025, 7, 1);
        var depositor = new ScriptedContract(Start, [depositTime],
            (executor, _) => executor.ExecuteTransaction(account, 10_000m, flow, "Deposit"));

        var executor = new ContractExecutor();
        executor.Contracts.Add(new AggregatedContract(Start, "Schedule", [flow, depositor]));

        var end = Start.AddYears(1);
        RunUntil(executor, end);

        var expected = Grow(100_000m, 0.05m, Start, end) + Grow(10_000m, 0.05m, depositTime, end);
        Assert.Equal(Math.Round(expected, 6), Math.Round(account.Balance, 6));
    }

    [Fact]
    public void Units_ChangeOnlyWithExternalFlows()
    {
        var (account, flow) = CreateFlow(100_000m, 0.05m);
        var depositTime = new DateTime(2026, 1, 1);
        var end = new DateTime(2026, 6, 1);
        var depositor = new ScriptedContract(Start, [depositTime],
            (executor, _) => executor.ExecuteTransaction(account, 10_000m, flow, "Deposit"));

        var executor = new ContractExecutor();
        executor.Contracts.Add(flow);
        executor.Contracts.Add(depositor);
        executor.Contracts.Add(new ScriptedContract(Start, Daily(Start, end)));

        RunUntil(executor, Start);
        Assert.Equal(100_000m, flow.Units);

        RunUntil(executor, depositTime.AddDays(-1));
        Assert.Equal(100_000m, flow.Units);

        RunUntil(executor, end);
        Assert.Equal(Math.Round(100_000m + 10_000m / Grow(1m, 0.05m, Start, depositTime), 8),
            Math.Round(flow.Units, 8));
    }

    [Fact]
    public void WithdrawingEverything_StopsGrowth()
    {
        var (account, flow) = CreateFlow(100_000m, 0.05m);
        var saleTime = new DateTime(2025, 7, 1);
        var seller = new ScriptedContract(Start, [saleTime],
            (executor, _) => executor.ExecuteTransaction(account, -account.Balance, flow, "Sale"));

        var executor = new ContractExecutor();
        executor.Contracts.Add(flow);
        executor.Contracts.Add(seller);

        RunUntil(executor, Start.AddYears(3));

        Assert.Equal(0m, account.Balance);
    }

    [Fact]
    public void ValueCap_StopsGrowthOnceReached()
    {
        var account = new Account("Asset");
        const decimal cap = 110_000m;
        var flow = new CompoundFlow(Start, 100_000m, v => v >= cap ? 0m : 0.05m, Yearly, account, "Asset");

        var end = Start.AddYears(5);
        var executor = new ContractExecutor();
        executor.Contracts.Add(flow);
        executor.Contracts.Add(new ScriptedContract(Start, Daily(Start, end)));

        RunUntil(executor, Start.AddYears(3));
        var valueAfterCap = account.Balance;
        RunUntil(executor, end);

        Assert.InRange(valueAfterCap, cap, Grow(cap, 0.05m, Start, Start.AddDays(1)));
        Assert.Equal(valueAfterCap, account.Balance);
    }

    [Fact]
    public void BookedTimes_AreAnchoredToStart()
    {
        var (_, flow) = CreateFlow(100_000m, 0.05m, TimeSpan.FromDays(30));
        var executor = new ContractExecutor();
        executor.Contracts.Add(flow);
        executor.Contracts.Add(new ScriptedContract(Start, [Start.AddDays(45)]));

        var booked = new List<DateTime>();
        DateTime? next = Start;
        while (next is not null && next <= Start.AddDays(90))
        {
            next = executor.Execute(next.Value);
            if (next is not null) booked.Add(next.Value);
        }

        Assert.Equal([Start.AddDays(30), Start.AddDays(45), Start.AddDays(60), Start.AddDays(90), Start.AddDays(120)],
            booked);
    }

    [Fact]
    public void NonPositiveStep_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateFlow(1m, 0.05m, TimeSpan.Zero));
    }
}
