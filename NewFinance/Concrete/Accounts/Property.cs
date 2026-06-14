using NewFinance.Concrete.Contracts;
using NewFinance.Core;

namespace NewFinance.Concrete.Accounts
{
    public class Property(PropertySchedule schedule, string name) : Account(name)
    {
        public PropertySchedule Schedule { get; } = schedule;
    }
}