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
            foreach(var asset in entity.Assets.Concat(entity.Liabilities))
            {
                if (asset.Ownership.TryGetValue(entity, out var share))
                {
                    equity += asset.Balance * share;
                }
                else
                {
                    equity += asset.Balance;
                }
            }
            return equity;
        }
    }
}