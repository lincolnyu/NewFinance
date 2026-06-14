namespace NewFinance.Core
{
    public class Account(string name) : IHasName, IHasBalance
    {
        public string Name { get; } = name;

        public decimal Balance { get; set; }

        public Dictionary<Entity, decimal> Ownership { get; } = new Dictionary<Entity, decimal>();
    }
}