using NewFinance.Common;
using NewFinance.Concrete.Entities;
using NewFinance.Concrete.Rules;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class Employment : BandedFlow
    {
        public const string ChangeTrackerPaygWithheld = "PaygWithheld";

        private static readonly TimeSpan DefaultPaygWithholdingFrequency = TimeSpan.FromDays(14);

        public Employment(BandedFlowDescriptor descriptor, TaxIndividual individual, Account cashAccount) : base(descriptor, cashAccount, $"Employment of {individual.Name}")
        {
            FlowBookingInterval = DefaultPaygWithholdingFrequency;
            Individual = individual;
        }

        public TimeSpan PaygWithholdingFrequency
        {
            get => FlowBookingInterval!.Value;
            set => FlowBookingInterval = value;
        }

        public bool WithholdPayg { get; set; } = true;
        public TaxIndividual Individual { get; }

        protected override void ApplyInflow(ContractExecutor executor, decimal inflow, TimeSpan executionTimeSpan)
        {
            var paygWithheld = WithholdPayg ? EstimatePaygWithholding(inflow, executionTimeSpan, Individual) : 0m;

            executor.ExecuteTransaction(Account!, inflow - paygWithheld, this, $"Inflow for {Name}");
            executor.ChangeTrackers?.GetOrCreateTracker(this, ChangeTrackerInflow).TrackChange(inflow);
            executor.ChangeTrackers?.GetOrCreateTracker(this, ChangeTrackerPaygWithheld).TrackChange(paygWithheld);
        }

        static internal decimal EstimatePaygWithholding(decimal grossIncome, TimeSpan period, TaxIndividual? individual)
        {
            if (grossIncome <= 0 || period.TotalDays <= 0)
            {
                return 0m;
            }

            var fractionOfYear = (decimal)(period.TotalDays / (double)Constants.DaysPerYear);
            var annualisedIncome = grossIncome / fractionOfYear;
            var annualTax = IndividualTax.CalculateResidentIncomeTax(annualisedIncome) +  new MedicareLevyRules().CalculateFY26(annualisedIncome, individual);

            return annualTax * fractionOfYear;
        }
    }
}
