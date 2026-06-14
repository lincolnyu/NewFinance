using NewFinance.Common;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class Employment : SteadyFlow
    {
        private static readonly TimeSpan DefaultPaygWithholdingFrequency = TimeSpan.FromDays(14);

        public Employment(SteadyFlowDescriptor descriptor, Account cashAccount) : base(descriptor, cashAccount, "Employment")
        {
            FlowBookingInterval = DefaultPaygWithholdingFrequency;
        }

        public TimeSpan PaygWithholdingFrequency
        {
            get => FlowBookingInterval!.Value;
            set => FlowBookingInterval = value;
        }

        public ChangeTracker PaygWithheldTracker { get; } = new ChangeTracker();

        public bool WithholdPayg { get; set; } = true;

        public override void Reset(ContractExecutor executor)
        {
            base.Reset(executor);
            PaygWithheldTracker.ResetAll();
        }

        protected override void ApplyInflow(decimal inflow, TimeSpan executionTimeSpan)
        {
            var paygWithheld = WithholdPayg ? EstimatePaygWithholding(inflow, executionTimeSpan) : 0m;

            Account!.Balance += inflow - paygWithheld;
            InflowTracker.TrackChange(inflow);
            PaygWithheldTracker.TrackChange(paygWithheld);
        }

        internal static decimal EstimatePaygWithholding(decimal grossIncome, TimeSpan period)
        {
            if (grossIncome <= 0 || period.TotalDays <= 0)
            {
                return 0m;
            }

            var fractionOfYear = (decimal)(period.TotalDays / (double)Constants.DaysPerYear);
            var annualisedIncome = grossIncome / fractionOfYear;
            var annualTax = IndividualTax.CalculateResidentIncomeTax(annualisedIncome) + IndividualTax.CalculateMedicareLevy(annualisedIncome);

            return annualTax * fractionOfYear;
        }
    }
}
