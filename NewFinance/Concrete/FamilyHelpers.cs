using NewFinance.Concrete.Entities;
using NewFinance.Core;

namespace NewFinance.Concrete
{
    public static class FamilyHelpers
    {
        public static void AddTaxMember(this Family family, TaxIndividual taxIndividual)
        {
            family.TaxMembers.Add(taxIndividual);
            taxIndividual.Family = family;
        }

        public static void AddSharedAsset(this Family family, Account asset, params decimal[] ownershipShares)
        {
            for (int i = 0; i < family.TaxMembers.Count; i++)
            {
                family.TaxMembers[i].AddAsset(asset, ownershipShares[i]);
            }
            family.AddAsset(asset, 1m);
        }

        public static void AddSharedLiability(this Family family, Account liability, params decimal[] ownershipShares)
        {
            for (int i = 0; i < family.TaxMembers.Count; i++)
            {
                family.TaxMembers[i].AddLiability(liability, ownershipShares[i]);
            }
            family.AddLiability(liability, 1m);
        }
    }
}