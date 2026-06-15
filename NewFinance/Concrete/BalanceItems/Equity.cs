using NewFinance.Core;

namespace NewFinance.Concrete
{
    public class Equity(string name, Entity entity) : IHasBalance, IHasName
    {
        public string Name => name;

        public decimal Balance => CalculateBalance();

        private decimal CalculateBalance()
        {
            var equity = 0m;
            foreach(var account in entity.Assets.Concat(entity.Liabilities))
            {
                if (account.Ownership.TryGetValue(entity, out var share))
                {
                    equity += account.Balance * share;
                }
                else
                {
                    equity += account.Balance;
                }
            }
            return equity;
        }
    }
}