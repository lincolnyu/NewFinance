using NewFinance.Common;

namespace NewFinance.Concrete.Contracts
{
    public static class InflationHelpers
    {
        public static SteadyFlowDescriptor ApplyInflationPreciseMatching(this Inflation inflation,  DateTime flowStartTime, decimal initialRate /* amount per day */)
        {
            // between flowStartTime and inflationStartTime, the amount is not adjusted for inflation.
            List<(decimal Rate, DateTime EndTime)> inflows = [];
            var currentTime = flowStartTime;
            var currentRate = initialRate;
            if (currentTime < inflation.StartTime)
            {
                inflows.Add((currentRate, inflation.StartTime));
                currentTime = inflation.StartTime;
            }

            foreach (var (inflationRate, inflationRateExpiryTime) in inflation.Rates)
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

        public static decimal GetRelativeInflationFactor(this Inflation inflation, DateTime fromTime, DateTime toTime)
        {
            decimal cumulativeInflationFactor = 1m;
            if (toTime <= inflation.StartTime)
            {
                return cumulativeInflationFactor;
            }
            var currentTime = fromTime;
            if (currentTime < inflation.StartTime)
            {
                currentTime = inflation.StartTime;
            }
            System.Diagnostics.Debug.Assert(currentTime >= inflation.StartTime);

            foreach (var (inflationRate, inflationRateExpiryTime) in inflation.Rates)
            {
                // process [currentTime, inflationRateExpiryTime)
                if (inflationRateExpiryTime <= currentTime)
                {
                    continue;
                }

                cumulativeInflationFactor *= 1 + inflationRate;

                if (inflationRateExpiryTime >= toTime)
                {
                    break;
                }

                currentTime = inflationRateExpiryTime;
            }

            return cumulativeInflationFactor;
        }
    }
}
