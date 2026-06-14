using NewFinance.Concrete.Contracts;
using NewFinance.Core;

namespace NewFinance.Concrete.Accounts
{
    public class Loan(LoanContract contract, string name) : Account(name)
    {
        public LoanContract Contract { get; } = contract;
    }
}