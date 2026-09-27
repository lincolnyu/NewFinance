using System.Diagnostics;

namespace NewFinance.Core;

public class ContractExecutor
{
    /// <summary>
    ///     The list of contracts to execute. The dependent contracts should be ordered in a way that they are executed after
    ///     the contracts they depend on.
    /// </summary>
    public List<Contract> Contracts { get; } = new();

    public DateTime? NextForcedTime { get; set; }

    public ChangeTrackers? ChangeTrackers { get; set; }

    public Action<Account.Transaction>? TransactionStarted { get; set; }

    public DateTime CurrentTime { get; private set; }

    public List<Account.Transaction> Transactions { get; } = new();

    public DateTime? Execute(DateTime currentTime)
    {
        CurrentTime = currentTime;

        var minNextTime = this.ExecuteContracts(Contracts, currentTime);

        foreach (var contract in Contracts) contract.PostExecute();

        if (NextForcedTime != null)
        {
            if (currentTime >= NextForcedTime)
                NextForcedTime = null;
            else if (NextForcedTime < minNextTime) minNextTime = NextForcedTime.Value;
        }

        Debug.Assert(minNextTime is null || minNextTime > currentTime,
            $"Next execution time {minNextTime} should be greater than current time {currentTime}");

        return minNextTime;
    }

    public void ReEnsureNextForcedTime(DateTime time)
    {
        if (NextForcedTime == null || time < NextForcedTime) NextForcedTime = time;
    }
}