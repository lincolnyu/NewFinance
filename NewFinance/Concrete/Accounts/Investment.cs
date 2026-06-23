using NewFinance.Concrete.Contracts;
using NewFinance.Core;

namespace NewFinance.Concrete.Accounts
{
    public class Investment(string name) : Account(name)
    {
        public InvestmentSchedule? Schedule { get; set; }

        public decimal PurchaseAdditionalCost { get; set; }

    }
}