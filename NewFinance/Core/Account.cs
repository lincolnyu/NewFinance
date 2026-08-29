using System.Text.Json.Serialization;

namespace NewFinance.Core
{
    public class Account : IHasName, IHasBalance
    {
        [JsonConstructor]
        public Account() { }

        public Account(string name, decimal balance = 0m)
        {
            Name = name;
            Balance = balance;
        }

        public string Name { get; set; } = "";

        public decimal Balance { get; set; }

        public List<OwnershipShare> Ownership { get; set; } = [];

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
                ExecutedTime = executor.CurrentTime;
                
                executor.TransactionStarted?.Invoke(this);

                Account.Balance += Amount;
                BalanceAfterTransaction = Account.Balance;
                executor.Transactions.Add(this);
            }
        }
    }
}
