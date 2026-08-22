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
                var share = account.GetShare(entity);
                if (share is not null)
                {
                    equity += account.Balance * share.Value;
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