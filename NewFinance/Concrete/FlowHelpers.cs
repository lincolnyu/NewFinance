using NewFinance.Common;

namespace NewFinance.Concrete.Contracts
{
    public static class FlowHelpers
    {
        public static SteadyFlowDescriptor ConstantFlowDescriptor(DateTime startTime, decimal rate)
        {
            return new SteadyFlowDescriptor(startTime, [(rate, DateTime.MaxValue)]);
        }

        public static Inflation ConstantInflation(DateTime startTime, decimal annualInflationRate)
        {
            return new Inflation(startTime, [(annualInflationRate, DateTime.MaxValue)]);
        }

        public static SteadyFlowDescriptor ApplyInflation(this Inflation inflation, DateTime flowStartTime, decimal initialRate)
        {
            var reviewDates = inflation.Rates.Select(r => r.Item2).ToList();
            return ApplyInflation(inflation, flowStartTime, initialRate, reviewDates);
        }

        public static SteadyFlowDescriptor ApplyInflation(this Inflation inflation, DateTime flowStartTime, decimal initialDailyRate, IEnumerable<DateTime> reviewDates)
        {
            List<(decimal Rate, DateTime EndTime)> inflows = [];

            DateTime lastReviewDate = flowStartTime;
            DateTime currentStartDate = flowStartTime;
            var currentRate = initialDailyRate;

            if (currentStartDate < inflation.StartTime)
            {
                inflows.Add((currentRate, inflation.StartTime));
                currentStartDate = inflation.StartTime;
            }
    
            System.Diagnostics.Debug.Assert(currentStartDate >= inflation.StartTime);

            foreach (var reviewDate in reviewDates)
            {
                if (reviewDate == DateTime.MaxValue)
                {
                    break;
                }

                if (reviewDate <= currentStartDate)
                {
                    continue;
                }

                var inflationFactor = GetRelativeInflationFactor(inflation, lastReviewDate, currentStartDate);
                lastReviewDate = currentStartDate;
                currentRate *= inflationFactor;
                inflows.Add((currentRate, reviewDate));

                currentStartDate = reviewDate;
            }

            var finalInflationFactor = GetRelativeInflationFactor(inflation, lastReviewDate, currentStartDate);
            currentRate *= finalInflationFactor;
            inflows.Add((currentRate, DateTime.MaxValue));

            return new SteadyFlowDescriptor(flowStartTime, inflows);
        }

        public static decimal GetRelativeInflationFactor(this Inflation inflation, DateTime fromTime, DateTime toTime)
        {
            decimal cumulativeInflationFactor = 1m;
            if (toTime <= inflation.StartTime || toTime == fromTime)
            {
                return cumulativeInflationFactor;
            }
            var currentTime = fromTime;
            if (currentTime < inflation.StartTime)
            {
                currentTime = inflation.StartTime;
            }

            System.Diagnostics.Debug.Assert(currentTime >= inflation.StartTime);
            System.Diagnostics.Debug.Assert(toTime > inflation.StartTime);

            foreach (var (inflationRate, inflationRateExpiryTime) in inflation.Rates)
            {
                // process [currentTime, inflationRateExpiryTime)
                if (inflationRateExpiryTime <= currentTime)
                {
                    continue;
                }

                System.Diagnostics.Debug.Assert(toTime > currentTime);

                var finishing = toTime <= inflationRateExpiryTime;

                var years = (decimal)(((finishing? toTime : inflationRateExpiryTime) - currentTime).TotalDays / (double)Constants.DaysPerYear);

                cumulativeInflationFactor *= (decimal)Math.Pow((double)(1 + inflationRate), (double)years);
                if (finishing)
                {
                    break;
                }

                currentTime = inflationRateExpiryTime;
            }

            return cumulativeInflationFactor;
        }

        public static void FlowCapping(SteadyFlowDescriptor flow, decimal capRate, bool isNegativeFlow)
        {
            for (int i = 0; i < flow.Inflows.Count; i++)
            {
                var (rate, endTime) = flow.Inflows[i];
                if (isNegativeFlow ? rate < capRate : rate > capRate)
                {
                    flow.Inflows[i] = (capRate, endTime);
                }
            }
        }
    }
}
