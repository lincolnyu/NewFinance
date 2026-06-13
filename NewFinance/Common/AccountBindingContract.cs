using NewFinance.Core;

namespace NewFinance.Common
{
    public abstract class AccountBindingContract : Contract
    {
        public Account? Account {get; }

        public AccountBindingContract(DateTime startTime, Account account) : base(startTime)
        {
            Account = account;
        }

        public override void Reset(ContractExecutor executor)
        {
            base.Reset(executor);

            Account?.Balance = 0;
        }
    }
}