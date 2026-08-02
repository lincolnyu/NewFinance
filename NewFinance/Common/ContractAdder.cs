using NewFinance.Core;

namespace NewFinance.Common
{
    class ContractAdder
    {
        HashSet<Contract> _addedContracts = new HashSet<Contract>();
        public List<Contract> OrderedContracts { get; } = new List<Contract>();
        public void AddContract(Contract contract)
        {
            if (_addedContracts.Add(contract))
            {
                OrderedContracts.Add(contract);
            }
        }

        public void UnionWith(IEnumerable<Contract> contracts)
        {
            foreach (var contract in contracts)
            {
                AddContract(contract);
            }
        }

        public void LoadExecutor(ContractExecutor executor)
        {
            executor.Contracts.AddRange(OrderedContracts);
        }
    }
}
