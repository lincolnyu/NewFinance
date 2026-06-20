using NewFinance.Common;
using NewFinance.Concrete.Entities;
using NewFinance.Concrete.Rules;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class Employment : SteadyFlow
    {
        private static readonly TimeSpan DefaultPaygWithholdingFrequency = TimeSpan.FromDays(14);

        public Employment(SteadyFlowDescriptor descriptor, TaxIndividual individual, Account cashAccount) : base(descriptor, cashAccount, $"Employment of {individual.Name}")
        {
            FlowBookingInterval = DefaultPaygWithholdingFrequency;
            Individual = individual;
        }

        public TimeSpan PaygWithholdingFrequency
        {
            get => FlowBookingInterval!.Value;
            set => FlowBookingInterval = value;
        }

        public ChangeTracker PaygWithheldTracker { get; } = new ChangeTracker();

        public bool WithholdPayg { get; set; } = true;
        public TaxIndividual Individual { get; }

        public override void Reset(ContractExecutor executor)
        {
            base.Reset(executor);
            PaygWithheldTracker.ResetAll();
        }

        protected override void ApplyInflow(ContractExecutor executor, decimal inflow, TimeSpan executionTimeSpan)
        {
            var paygWithheld = WithholdPayg ? EstimatePaygWithholding(inflow, executionTimeSpan) : 0m;

            executor.ExecuteTransaction(Account!, inflow - paygWithheld, this, $"Inflow for {Name}");
            InflowTracker.TrackChange(inflow);
            PaygWithheldTracker.TrackChange(paygWithheld);
        }

        internal decimal EstimatePaygWithholding(decimal grossIncome, TimeSpan period)
        {
            if (grossIncome <= 0 || period.TotalDays <= 0)
            {
                return 0m;
            }

            var fractionOfYear = (decimal)(period.TotalDays / (double)Constants.DaysPerYear);
            var annualisedIncome = grossIncome / fractionOfYear;
            var annualTax = IndividualTax.CalculateResidentIncomeTax(annualisedIncome) +  new MedicareLevyRules().Calculate(annualisedIncome, Individual);

            return annualTax * fractionOfYear;
        }
    }
}
