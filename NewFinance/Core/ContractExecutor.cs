namespace NewFinance.Core
{
    public class ContractExecutor
    {
        /// <summary>
        ///  The list of contracts to execute. The dependent contracts should be ordered in a way that they are executed after the contracts they depend on.
        /// </summary>
        public List<Contract> Contracts { get; } = new List<Contract>();

        public DateTime? NextForcedTime { get; set; }

        public void Reset()
        {
            foreach (var c in Contracts)
            {
                c.Reset(this);
            }
        }

        public DateTime Execute(DateTime currentTime)
        {
            DateTime minNextTime = this.ExecuteContracts(Contracts, currentTime);

            if (NextForcedTime != null)
            {
                if (currentTime >= NextForcedTime)
                {
                    NextForcedTime = null;
                }
                else if (NextForcedTime < minNextTime)
                {
                    minNextTime = NextForcedTime.Value;
                }
            }

            System.Diagnostics.Debug.Assert(minNextTime > currentTime, $"Next execution time {minNextTime} should be greater than current time {currentTime}");
    
            return minNextTime;
        }

        public void ReEnsureNextForcedTime(DateTime time)
        {
            if (NextForcedTime == null || time < NextForcedTime)
            {
                NextForcedTime = time;
            }
        }
    }
}