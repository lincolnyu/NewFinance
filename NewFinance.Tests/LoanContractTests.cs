namespace NewFinance.Tests;

using NewFinance.Concrete.Accounts;
using NewFinance.Concrete.Contracts;
using NewFinance.Core;

public class LoanContractTests
{
    private static readonly DateTime Settlement = new(2025, 1, 1);

    private sealed class Scenario
    {
        public required Loan Loan { get; init; }
        public required LoanContract Contract { get; init; }
        public required Account Cash { get; init; }
        public required ContractExecutor Executor { get; init; }

        private DateTime? _nextTime;

        public void RunUntil(DateTime end)
        {
            _nextTime ??= Contract.StartTime;
            while (_nextTime is not null && _nextTime <= end)
            {
                _nextTime = Executor.Execute(_nextTime.Value);
            }
        }

        public List<Account.Transaction> Repayments(string prefix) =>
            Executor.Transactions.Where(t => t.Account == Cash && t.Name.StartsWith(prefix)).ToList();

        public decimal TrackedInterest => Executor.ChangeTrackers![Contract.PaidInterestTrackerKey!].TotalChange;
    }

    private static Scenario CreateScenario(decimal loanAmount, decimal? termYears, decimal annualRate,
        DateTime? settlement = null, decimal initialCash = 0m, decimal offsetRatio = 0m)
    {
        var cash = new Account("Cash", initialCash);
        var loan = new Loan("Test Loan");
        var contract = new LoanContract(loan, null, null, settlement ?? Settlement, loanAmount)
        {
            CashAccount = cash,
            LoanTermYears = termYears,
            AnnualInterestRate = annualRate,
            OffsetRatio = offsetRatio
        }.CreateAllNaturalTrackerKeys();
        loan.Contract = contract;

        var executor = new ContractExecutor { ChangeTrackers = new ChangeTrackers() };
        executor.Contracts.Add(contract);

        return new Scenario { Loan = loan, Contract = contract, Cash = cash, Executor = executor };
    }

    [Fact]
    public void FirstRepayment_IsAnnuityRoundedUpToCent_WithDailyInterestOver365()
    {
        var s = CreateScenario(500_000m, 30, 0.06m);

        s.RunUntil(new DateTime(2025, 2, 1));

        // 500,000 at 6% over 360 months = 2,997.7526... -> rounded up to the cent.
        var repayment = Assert.Single(s.Repayments("P+I repayment"));
        Assert.Equal(new DateTime(2025, 2, 1), repayment.ExecutedTime);
        Assert.Equal(-2_997.76m, repayment.Amount);

        // 31 days of interest on 500,000 at 6% / 365 = 2,547.945...
        Assert.Equal(-2_547.95m, Math.Round(s.TrackedInterest, 2));
        // Principal = 2,997.76 - 2,547.945... = 449.81.
        Assert.Equal(-499_550.19m, Math.Round(s.Loan.Balance, 2));
    }

    [Fact]
    public void PrincipalAndInterestLoan_PaysOffExactlyAtEndOfTerm()
    {
        var s = CreateScenario(500_000m, 30, 0.06m);

        s.RunUntil(Settlement.AddMonths(359));
        Assert.True(s.Loan.Balance < 0);

        s.RunUntil(Settlement.AddMonths(360));
        Assert.Equal(0m, s.Loan.Balance);
        Assert.Equal(360, s.Repayments("P+I repayment").Count);

        // Nothing more is charged once the loan is paid off.
        s.RunUntil(Settlement.AddMonths(363));
        Assert.Equal(360, s.Repayments("P+I repayment").Count);
    }

    [Fact]
    public void ChargeDates_AreAnchoredToSettlementDay_WithoutMonthEndDrift()
    {
        var s = CreateScenario(12_000m, 1, 0.05m, settlement: new DateTime(2025, 1, 31));

        s.RunUntil(new DateTime(2025, 5, 31));

        Assert.Equal(
            [new DateTime(2025, 2, 28), new DateTime(2025, 3, 31), new DateTime(2025, 4, 30), new DateTime(2025, 5, 31)],
            s.Repayments("P+I repayment").Select(t => t.ExecutedTime));
    }

    [Fact]
    public void InterestOnlyLoan_ChargesInterestMonthly_AndKeepsBalance()
    {
        var s = CreateScenario(100_000m, null, 0.06m);

        s.RunUntil(new DateTime(2026, 1, 1));

        var repayments = s.Repayments("Interest payment");
        Assert.Equal(12, repayments.Count);
        // 2025 has 365 days, so a full year of daily interest is exactly the annual rate.
        Assert.Equal(-6_000m, Math.Round(repayments.Sum(t => t.Amount), 2));
        Assert.Equal(-6_000m, Math.Round(s.TrackedInterest, 2));
        Assert.Equal(-100_000m, s.Loan.Balance);
    }

    [Fact]
    public void Offset_ReducesInterest_ButNotTheRepayment()
    {
        // The personal loan pays its proceeds into cash, so start at -400,000 to leave 100,000 in the offset.
        var s = CreateScenario(500_000m, 30, 0.06m, initialCash: -400_000m, offsetRatio: 1m);

        s.RunUntil(new DateTime(2025, 2, 1));

        var repayment = Assert.Single(s.Repayments("P+I repayment"));
        Assert.Equal(-2_997.76m, repayment.Amount);
        // 31 days of interest on (500,000 - 100,000) at 6% / 365 = 2,038.356...
        Assert.Equal(-2_038.36m, Math.Round(s.TrackedInterest, 2));
    }

    [Fact]
    public void RateChange_RecalculatesRepayment_AndStillPaysOffAtEndOfTerm()
    {
        var s = CreateScenario(500_000m, 30, 0.06m);

        s.RunUntil(Settlement.AddMonths(60));
        s.Contract.AnnualInterestRate = 0.08m;
        s.RunUntil(Settlement.AddMonths(61));

        Assert.True(s.Repayments("P+I repayment").Last().Amount < -2_997.76m);

        s.RunUntil(Settlement.AddMonths(360));
        Assert.Equal(0m, s.Loan.Balance);
        Assert.Equal(360, s.Repayments("P+I repayment").Count);
    }
}
