using System.Collections.Generic;
using System.Linq;
using Allocations.Models.Specs;

namespace Allocations.Models
{
    public static class ModelExtensions
    {
        public static Product GetProduct(this Budget budget, string id)
        {
            foreach (var fundingPolicy in budget.FundingPolicies)
            {
                foreach (var allocationLine in fundingPolicy.AllocationLines ?? new List<AllocationLine>())
                {
                    foreach (var productFolder in allocationLine?.ProductFolders ?? new List<ProductFolder>())
                    {
                        var product = productFolder.Products.FirstOrDefault(x => x.Id == id);
                        if (product != null) return product;
                    }
                }
            }
            return null;
        }
    }
}