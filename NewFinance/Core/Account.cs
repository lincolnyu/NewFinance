namespace NewFinance.Core
{
    public class Account(string name, decimal initialBalance = 0m) : IHasName, IHasBalance
    {
        public string Name { get; } = name;

        public decimal Balance { get; private set; } = initialBalance;

        public Dictionary<Entity, decimal> Ownership { get; } = new Dictionary<Entity, decimal>();

        public class Transaction(string name = "") : IHasName
        {
            public DateTime ExecutedTime { get; private set; }

            public string Name { get; } = name;

            public Contract Contract { get; set; } = null!;

            public Account Account { get; set; } = null!;

            public decimal Amount { get; set; }

            public decimal BalanceAfterTransaction { get; private set;}

            public void ExecuteAndRecord(ContractExecutor executor)
            {
                ExecutedTime = executor.CurrentTime!.Value;
                
                executor.TransactionStarted?.Invoke(this);

                Account.Balance += Amount;
                BalanceAfterTransaction = Account.Balance;
                executor.Transactions.Add(this);
            }
        }
    }
}
