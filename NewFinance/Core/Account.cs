namespace NewFinance.Core
{
    public class Account(string name) : IHasName
    {
        public string Name { get; } = name;

        public List<(Entity Owner, decimal Share)> Ownership { get; } = new List<(Entity Owner, decimal Share)>();

        public decimal Balance { get; set; }
    }
}