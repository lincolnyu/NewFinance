namespace NewFinance.Core
{
    public class Account
    {
        public List<(Entity Owner, decimal Share)> Ownership { get; } = new List<(Entity Owner, decimal Share)>();

        public decimal Balance { get; set; }
    }
}