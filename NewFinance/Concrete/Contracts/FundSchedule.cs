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

                        decimal remainingFees = fees;
                        if (schedule.FeePaymentAccount is not null)
                        {
                            if(schedule.FeePaymentAccount.Balance > remainingFees)
                            {
                                executor.ExecuteTransaction(schedule.FeePaymentAccount, -remainingFees, schedule.FeeContract!, $"Fees for {schedule.Investment.Name}");
                                remainingFees = 0;
                            }
                            else
                            {
                                executor.ExecuteTransaction(schedule.FeePaymentAccount, -schedule.FeePaymentAccount.Balance, schedule.FeeContract!, $"Fees for {schedule.Investment.Name}");
                                remainingFees -= schedule.FeePaymentAccount.Balance;
                            }
                        }

                        if (remainingFees > Investment.Balance)
                        {
                            // TODO Handle and report the error
                        }
                        else if (remainingFees > 0)
                        {
                            Trade(executor, -remainingFees, out var _);
                        }
                    });

                    return (currentTime, newBookedTime); // Returning null as booked time letting the primary contract to drive.
                }
            );
        }

        public void Trade(ContractExecutor executor, decimal netBuy, out decimal? profit)
        {
            profit = null;
            var currentPrice = Value.CurrentPricePerShare;
            decimal shares = netBuy / currentPrice;
            if (netBuy > 0)    // buy
            {
                _positions[currentPrice] = _positions.GetValueOrDefault(currentPrice, 0m) + shares;
                executor.ExecuteTransaction(Investment, netBuy, this, $"Buy shares for {Investment.Name}");
            }
            else    // sell
            {
                decimal sharesToSell = -shares;
                if (-netBuy > Investment.Balance) // sell amount is greater than balance. it's an error
                {
                    // TODO report the error
                    return;
                }

                if (-netBuy == Investment.Balance)
                {
                    foreach (var position in _positions)
                    {
                        profit = (currentPrice - position.Key) * position.Value + (profit ?? 0);
                    }
                    _positions.Clear();
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

                executor.ExecuteTransaction(Investment, netBuy, this, $"Sell shares for {Investment.Name}");

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