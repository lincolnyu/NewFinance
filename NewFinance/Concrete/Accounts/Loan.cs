using NewFinance.Concrete.Contracts;
using NewFinance.Core;

namespace NewFinance.Concrete.Accounts
{
    public class Loan(LoanContract contract) : Account
    {
        public LoanContract Contract { get; } = contract;
    }
}