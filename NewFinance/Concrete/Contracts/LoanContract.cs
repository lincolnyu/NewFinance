using System.Diagnostics;
using NewFinance.Common;
using NewFinance.Concrete.Accounts;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts;

public class LoanContract : AccountBindingContract
{
    // Australian lenders (e.g. ANZ) calculate daily interest as annual rate / 365.
    private const decimal InterestDaysPerYear = 365m;

    private decimal _accumulatedInterest;

    // Number of repayments charged so far. Charge dates are anchored to the settlement date to avoid month-end drift.
    private int _chargeCount;

    private decimal? _monthlyPayment;
    private decimal _monthlyPaymentRate;

    private DateTime? _nextChargeTime;

    public LoanContract(Loan loanAccount, Property? property, decimal? deposit, DateTime? settlementTime,
        decimal loanAmount)
        : base(GetStartTime(property, deposit, settlementTime), loanAccount,
            string.IsNullOrEmpty(loanAccount.Name)
                ? property is null ? "Loan" : $"Loan for {property?.Name}"
                : loanAccount.Name)
    {
        Deposit = deposit;
        SettlementTime = GetSettlementTime(property, settlementTime);
        Property = property;
        LoanAmount = loanAmount;
        PurchaseAdditionalCost = property?.PurchaseAdditionalCost ?? 0;
        Debug.Assert((deposit is not null && SettlementTime is not null &&
                      Property!.Schedule!.PurchaseTime <= SettlementTime) || deposit is null);
    }

    public ITrackerKey? PaidInterestTrackerKey { get; set; }

    public ITrackerKey? PaidPrincipalTrackerKey { get; set; }

    public Property? Property { get; }

    public required Account CashAccount { get; set; }

    public decimal? Deposit { get; } // Paid at purchase time.

    public DateTime? SettlementTime { get; }

    public decimal LoanAmount { get; set; }

    public decimal PurchaseAdditionalCost { get; set; }

    public decimal OffsetRatio { get; set; }

    public decimal? LoanTermYears { get; set; }

    public decimal AnnualInterestRate { get; set; } // e.g. 0.05 for 5%

    public Action<LoanContract, ContractExecutor, decimal>? OnSettlement { get; set; }

    // Interest accrues daily on the daily balance (as lenders do) and is charged on the next charge time.
    private DateTime NextCalculationTime(DateTime current)
    {
        var nextCalculationTime = current.AddDays(1);
        return !_nextChargeTime.HasValue || nextCalculationTime < _nextChargeTime.Value
            ? nextCalculationTime
            : _nextChargeTime.Value;
    }

    private static DateTime GetSettlementTime(Property? property, DateTime? settlementTime)
    {
        return settlementTime ?? property!.Schedule!.StartTime!.Value;
    }

    private static DateTime GetStartTime(Property? property, decimal? deposit, DateTime? settlementTime)
    {
        var actualSettlementTime = GetSettlementTime(property, settlementTime);
        if (deposit is not null)
        {
            var purchaseTime = property!.Schedule!.PurchaseTime;
            if (purchaseTime > actualSettlementTime)
                throw new Exception("Purchase time is not allowed to be later than settlement time.");

            return purchaseTime;
        }

        return actualSettlementTime;
    }

    protected override (DateTime processedTime, DateTime? bookedTime) Execute(ContractExecutor executor,
        DateTime? lastProcessedTime, DateTime? lastBookedTime, DateTime currentTime)
    {
        if (Deposit is not null)
        {
            var purchaseTime = Property!.Schedule!.PurchaseTime;
            if (currentTime == purchaseTime)
            {
                Debug.Assert(purchaseTime <= SettlementTime);
                executor.ExecuteTransaction(CashAccount, -Deposit.Value, this, $"Deposit for {Name}");
                if (purchaseTime < SettlementTime) return (currentTime, SettlementTime!.Value);
            }

            if (currentTime < purchaseTime) return (currentTime, purchaseTime);
        }

        if (currentTime == SettlementTime)
        {
            var totalFundsRequired = (Property?.Schedule?.PurchasePrice ?? 0) + PurchaseAdditionalCost - (Deposit ?? 0);
            var cashRequired = totalFundsRequired - LoanAmount;
            executor.ExecuteTransaction(CashAccount, -cashRequired, this, $"Settlement for {Name}");

            OnSettlement?.Invoke(this, executor, -cashRequired);

            executor.ExecuteTransaction(Account!, -LoanAmount, this, $"Loan amount for {Name}");

            _nextChargeTime = SettlementTime!.Value.AddMonths(_chargeCount + 1);
            return (currentTime, NextCalculationTime(currentTime));
        }

        if (currentTime < SettlementTime) return (currentTime, SettlementTime);

        Debug.Assert(_nextChargeTime.HasValue);

        AccrueInterest(currentTime - lastProcessedTime!.Value);

        if (currentTime >= _nextChargeTime)
        {
            ApplyRepayment(executor);

            _chargeCount++;
            _nextChargeTime = SettlementTime!.Value.AddMonths(_chargeCount + 1);
        }

        var newTime = NextCalculationTime(currentTime);
        return (currentTime, newTime);
    }

    // Like the lender, the repayment is fixed until the rate changes, then recalculated from the current balance over the remaining term.
    private int TermMonths => (int)(LoanTermYears!.Value * 12);

    private decimal GetMonthlyPayment()
    {
        if (_monthlyPayment is null || _monthlyPaymentRate != AnnualInterestRate)
        {
            var remainingMonths = TermMonths - _chargeCount;
            _monthlyPayment = CalculateMonthlyPayment(-Account!.Balance, remainingMonths);
            _monthlyPaymentRate = AnnualInterestRate;
        }

        return _monthlyPayment.Value;
    }

    private decimal CalculateMonthlyPayment(decimal balance, int remainingMonths)
    {
        if (remainingMonths <= 0) return balance; // Past the term: clear the rest.

        var monthlyRate = (double)AnnualInterestRate / 12; // e.g. 0.0555 -> 0.004625
        var payment = monthlyRate == 0
            ? (double)balance / remainingMonths
            : (double)balance * monthlyRate / (1 - Math.Pow(1 + monthlyRate, -remainingMonths));

        return Math.Ceiling((decimal)payment * 100) / 100; // Lenders round the repayment up to the cent.
    }

    private void AccrueInterest(TimeSpan time)
    {
        var fractionOfYear = (decimal)time.TotalDays / InterestDaysPerYear;

        // Assuming the offset account reduces the interest applied on the loan balance.
        var interestApplicable = Math.Max(0, -Account!.Balance - CashAccount.Balance * OffsetRatio);
        _accumulatedInterest += AnnualInterestRate * fractionOfYear * interestApplicable;
    }

    private void ApplyRepayment(ContractExecutor executor)
    {
        // Assuming interest and principal are charged at the same time.
        decimal principalPayment = 0;
        if (LoanTermYears.HasValue)
        {
            var monthlyPayment = GetMonthlyPayment();
            // The final scheduled repayment clears whatever is left (e.g. extra interest from leap years).
            var isFinalRepayment = _chargeCount + 1 >= TermMonths;
            principalPayment = isFinalRepayment
                ? Math.Max(0, -Account!.Balance)
                : Math.Max(0, Math.Min(monthlyPayment - _accumulatedInterest, -Account!.Balance));

            if (principalPayment > 0)
            {
                executor.ExecuteTransaction(Account!, principalPayment, this, $"Principal payment for {Name}");
                if (PaidPrincipalTrackerKey is not null)
                    executor.ChangeTrackers?[PaidPrincipalTrackerKey].TrackChange(-principalPayment);
            }
        }

        var cashDebit = _accumulatedInterest + principalPayment;
        if (cashDebit > 0)
        {
            var cashTransactionName =
                LoanTermYears.HasValue ? $"P+I repayment for {Name}" : $"Interest payment for {Name}";
            executor.ExecuteTransaction(CashAccount, -cashDebit, this, cashTransactionName);
        }

        if (PaidInterestTrackerKey is not null && _accumulatedInterest > 0)
            executor.ChangeTrackers?[PaidInterestTrackerKey].TrackChange(-_accumulatedInterest);

        _accumulatedInterest = 0;
    }
}