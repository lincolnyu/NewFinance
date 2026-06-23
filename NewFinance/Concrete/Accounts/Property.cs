using NewFinance.Concrete.Contracts;
using NewFinance.Core;

namespace NewFinance.Concrete.Accounts
{
    public class Property(string name) : Investment(name)
    {
        public bool IsPurchasedAsNewBuild { get; set; } = false;

        public new PropertySchedule? Schedule
        {
            get => base.Schedule as PropertySchedule;
            set => base.Schedule = value;
        }
    }
}