using NewFinance.Common;

namespace NewFinance.Concrete.Contracts
{
    public static class InflationFactory
    {
        public static SteadyFlowDescriptor ApplyInflationPreciseMatching(DateTime flowStartTime, decimal initialRate /* amount per day */, DateTime inflationStartTime, IEnumerable<(decimal, DateTime)> inflationRates)
        {
            // between flowStartTime and inflationStartTime, the amount is not adjusted for inflation.
            List<(decimal Rate, DateTime EndTime)> inflows = [];
            var currentTime = flowStartTime;
            var currentRate = initialRate;
            if (currentTime < inflationStartTime)
            {
                inflows.Add((currentRate, inflationStartTime));
                currentTime = inflationStartTime;
            }

            System.Diagnostics.Debug.Assert(currentTime >= inflationStartTime);
            
            foreach (var (inflationRate, inflationRateExpiryTime) in inflationRates)
            {
                // process [currentTime, inflationRateExpiryTime) 
                if (inflationRateExpiryTime <= currentTime)
                {
                    continue;
                }

                if (inflows.Count > 0)
                {
                    currentRate *= 1+inflationRate;
                }

                inflows.Add((currentRate, inflationRateExpiryTime));

                currentTime = inflationRateExpiryTime;
            }

            // Always add this to the end.
            inflows.Add((currentRate, DateTime.MaxValue));

            return new SteadyFlowDescriptor(flowStartTime, inflows);
        }
    }
}
