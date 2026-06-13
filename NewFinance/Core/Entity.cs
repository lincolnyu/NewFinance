namespace NewFinance.Core
{
    public class Entity
    {
        public string Name { get; set; } = "";

        // Assets are positive value, but we want to keep them separate from liabilities for clarity.
        public List<Account> Assets { get; } = new List<Account>();

        // Liabilities are negative value, but we want to keep them separate from assets for clarity.
        public List<Account> Liabilities { get; } = new List<Account>();       
    }
}
