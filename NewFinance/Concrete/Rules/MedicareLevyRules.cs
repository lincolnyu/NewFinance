using NewFinance.Concrete.Entities;

namespace NewFinance.Concrete.Rules
{
    public class MedicareLevyRules
    {
        // === Medicare Levy (2%) Rules - Fully Configurable ===
        // Assuming no Medicare Levy Surcharge (MLS) due to adequate private hospital cover, so we only calculate the standard Medicare Levy here.
        // https://grok.com/share/bGVnYWN5_a74c7aee-930e-459f-b9b7-b0befaf3e70b
        
        // Single person thresholds
        public decimal SingleLowerThreshold { get; set; } = 27_222m;
        public decimal SingleUpperThreshold { get; set; } = 34_027m;

        // Family thresholds (base)
        public decimal FamilyLowerThresholdBase { get; set; } = 45_907m;
        public decimal FamilyUpperThresholdBase { get; set; } = 57_383m;

        // Per child increases (for families / sole parents)
        public decimal FamilyLowerPerChild { get; set; } = 4_216m;
        public decimal FamilyUpperPerChild { get; set; } = 5_270m;

        // Levy rates
        public decimal FullLevyRate { get; set; } = 0.02m;     // 2%
        public decimal PhaseInRate { get; set; } = 0.10m;      // 10% phase-in rate

        /// <summary>
        /// Calculates only the Medicare Levy (2%). 
        /// MLS is ignored because we assume adequate private hospital cover.
        /// </summary>
        public decimal Calculate(decimal taxableIncome, TaxIndividual individual)
        {
            taxableIncome = Math.Max(0, taxableIncome);

            decimal levy = CalculateMedicareLevyInternal(taxableIncome, individual);

            // Optional: You can still record it separately in ChangeTracker if desired
            // e.g. individual.ChangeTracker.Record("Medicare Levy", levy);

            return levy;
        }

        private decimal CalculateMedicareLevyInternal(decimal taxableIncome, TaxIndividual ind)
        {
            bool isFamily = ind.Family is not null;
            int numChildren = ind.Family?.DependencyCount ?? 0; // we assume there are NO non-family dependents here.

            decimal lowerThreshold;
            decimal upperThreshold;

            if (isFamily)
            {
                lowerThreshold = FamilyLowerThresholdBase + (FamilyLowerPerChild * numChildren);
                upperThreshold = FamilyUpperThresholdBase + (FamilyUpperPerChild * numChildren);
            }
            else
            {
                lowerThreshold = SingleLowerThreshold;
                upperThreshold = SingleUpperThreshold;
            }

            if (taxableIncome <= lowerThreshold)
            {
                return 0m;                                      // Full exemption
            }

            if (taxableIncome <= upperThreshold)
            {
                // Phase-in range
                decimal excess = taxableIncome - lowerThreshold;
                decimal phaseInAmount = excess * PhaseInRate;
                decimal fullLevyAmount = taxableIncome * FullLevyRate;

                return Math.Min(phaseInAmount, fullLevyAmount);
            }

            // Full levy
            return taxableIncome * FullLevyRate;
        }
    }
}