using NewFinance.Concrete.Contracts;
using NewFinance.Core;

namespace NewFinance.Concrete.Accounts
{
    public class Property(string name) : Account(name)
    {
        public PropertySchedule? Schedule { get; set; }
    }
}