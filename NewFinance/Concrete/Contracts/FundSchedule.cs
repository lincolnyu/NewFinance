using NewFinance.Common;
using NewFinance.Concrete.Accounts;
using NewFinance.Core;

namespace NewFinance.Concrete.Contracts
{
    public class FundSchedule : InvestmentSchedule
    {
        public ITrackerKey? FundCapitalGainTrackerKey { get; init; }
        public ITrackerKey? FundYieldTrackerKey { get; init; }
        public ITrackerKey? FundFeesTrackerKey { get; init; }

        private Dictionary<decimal, decimal> _positions = new Dictionary<decimal, decimal>();   // Price to shares mapping.

        // Define properties and methods for FundSchedule here
        public FundSchedule(Fund fund, DateTime startTime, decimal initialValue, Func<decimal, decimal> getGrowthRate, 
            Func<FundSchedule, TimeSpan, decimal> getYield, Func<decimal, (Account, decimal)>? cash) : base(fund, startTime, initialValue, getGrowthRate)
        {
            YieldContract = new ContextualContract(
                startTime, 
                "Investment Yield",
                this,
                (context, executor, lastProcessedTime, lastBookedTime, currentTime) =>
                {
                    var schedule = (FundSchedule)context;
                    var lastTime = lastProcessedTime ?? schedule.StartTime!.Value;

                    var yield = getYield(schedule, currentTime - lastTime);
                    decimal reinvestment = yield;

                    if (cash is not null)
                    {
                        (var cashAccount, var cashAmount) = cash(yield); 
                        reinvestment -= cashAmount;

                        if (reinvestment < 0)
                        {
                            // sell
                            Trade(executor, reinvestment, out var profit);
                            // TODO capital gain
                            // tax for profit
                        }
                        else if (reinvestment > 0)
                        {
                            // buy
                            Trade(executor, reinvestment, out var _);
                        }

                        if(cashAmount > 0)
                        {
                            executor.ExecuteTransaction(cashAccount, cashAmount, YieldContract!, $"Yield for {schedule.Investment.Name}");
                            if (FundYieldTrackerKey is not null)
                            {
                                executor.ChangeTrackers?.GetOrCreateTracker(FundYieldTrackerKey).TrackChange(cashAmount);
                            }
                            // tax for yield
                        }
                    }
                    
                    if (reinvestment > 0)
                    {
                        executor.ExecuteTransaction(schedule.Investment, reinvestment, schedule.YieldContract!, $"Reinvestment for {schedule.Investment.Name}");
                    }

                    return (currentTime, null); // Returning null as booked time letting the primary contract to drive.
                }
            );

            FeeContract = new ContextualContract(
                startTime, 
                "Investment Fees",
                this,
                (context, executor, lastProcessedTime, lastBookedTime, currentTime) =>
                {
                    var newBookedTime = ContractHelpers.RunPeriodic(FeePeriod, lastProcessedTime ?? startTime, currentTime, () =>
                    {
                        var schedule = (FundSchedule)context;
                        var lastTime = lastProcessedTime ?? schedule.StartTime!.Value;

                        var fee = schedule.Investment.Balance * FeeRateToValue;
                        if (fee > FeeCap)
                        {
                            fee = FeeCap.Value;
                        }
                        var fees = fee * (currentTime - lastTime).Days / Constants.DaysPerYear;

                        if (FundFeesTrackerKey is not null)
                        {
                            executor.ChangeTrackers?.GetOrCreateTracker(FundFeesTrackerKey).TrackChange(-fees);
                        }

                        executor.ExecuteTransaction(schedule.FeePaymentAccount?? schedule.Investment, -fees, schedule.FeeContract!, $"Fees for {schedule.Investment.Name}");
                    });

                    return (currentTime, newBookedTime); // Returning null as booked time letting the primary contract to drive.
                }
            );
        }

        public void Trade(ContractExecutor executor, decimal netFund, out decimal? profit)
        {
            profit = null;
            var currentPrice = this.Value.CurrentPricePerShare;
            decimal shares = netFund / currentPrice;
            if (netFund > 0)    // buy
            {
                _positions[currentPrice] = _positions.GetValueOrDefault(currentPrice, 0m) + shares;
                executor.ExecuteTransaction(Investment, netFund, this, $"Buy shares for {Investment.Name}");
            }
            else    // sell
            {
                decimal sharesToSell = -shares;
                if (-netFund > Investment.Balance)
                {
                    return;
                }

                if (-netFund == Investment.Balance)
                {
                    _positions.Clear();
                    foreach (var position in _positions)
                    {
                        profit = (currentPrice - position.Key) * position.Value + (profit ?? 0);
                    }
                    executor.ExecuteTransaction(Investment, -Investment.Balance, this, $"Sell all shares for {Investment.Name}");
                    if (profit != 0 && FundCapitalGainTrackerKey is not null)
                    {
                        executor.ChangeTrackers?.GetOrCreateTracker(FundCapitalGainTrackerKey).TrackChange(profit ?? 0);
                    }
                    return;
                }

                var sortedPositions = _positions.OrderByDescending(p => p.Key).ToList(); // Sort by price descending
                foreach (var position in sortedPositions)
                {
                    if (sharesToSell <= 0)
                        break;

                    decimal purchasePrice = position.Key;
                    decimal availableShares = position.Value;

                    if (availableShares <= sharesToSell)
                    {
                        // Sell all available shares at this price

                        profit = (currentPrice - purchasePrice) * availableShares + (profit ?? 0);

                        _positions.Remove(purchasePrice);
                        sharesToSell -= availableShares;
                    }
                    else
                    {
                        profit = (currentPrice - purchasePrice) * sharesToSell + (profit ?? 0);

                        // Sell only the required number of shares at this price
                        _positions[purchasePrice] -= sharesToSell;
                        sharesToSell = 0;
                    }
                }

                executor.ExecuteTransaction(Investment, netFund, this, $"Sell shares for {Investment.Name}");

                if (profit != 0 && FundCapitalGainTrackerKey is not null)
                {
                    executor.ChangeTrackers?.GetOrCreateTracker(FundCapitalGainTrackerKey).TrackChange(profit ?? 0);
                }

                if (sharesToSell > 0)
                {
                    throw new InvalidOperationException("Not enough shares to sell.");
                }
            }
        }

        public ContextualContract YieldContract { get; }

        public ContextualContract FeeContract { get; }

        public TimeSpan FeePeriod { get; set; }
        public decimal FeeRateToValue { get; set; }
        public decimal? FeeCap { get; set; }

        public Account? FeePaymentAccount { get; set; }

        protected override IEnumerable<Contract> SubContracts
        {
            get
            {
                yield return YieldContract;
                yield return FeeContract;
            }
        }

        public override DateTime? Execute(ContractExecutor executor, DateTime currentTime)
        {
            var result = base.Execute(executor, currentTime);
            if(currentTime == StartTime)
            {
                _positions[1m] = Investment.Balance;
            }
            return result;
        }
    }
}