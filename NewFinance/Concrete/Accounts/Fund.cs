using NewFinance.Concrete.Contracts;
using NewFinance.Core;

namespace NewFinance.Concrete.Accounts
{
    public class Fund(string name) : Investment(name)
    {
        public new FundSchedule? Schedule 
        {
             get => base.Schedule as FundSchedule;
            set => base.Schedule = value;
        }
    }
}