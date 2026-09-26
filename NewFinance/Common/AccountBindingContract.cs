using NewFinance.Core;

namespace NewFinance.Common;

public abstract class AccountBindingContract(DateTime startTime, Account account, string name)
    : Contract(startTime, name)
{
    public Account? Account { get; } = account;
}