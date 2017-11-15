using System.Linq;
using Allocations.Models.Specs;

namespace Allocations.Models
{
    public static class ModelExtensions
    {
        public static Product GetProduct(this Budget budget, string id)
        {
            Product product = null;
            foreach (var fundingPolicy in budget.FundingPolicies)
            {
                foreach (var allocationLine in fundingPolicy.AllocationLines)
                {
                    foreach (var productFolder in allocationLine.ProductFolders)
                    {
                        product = productFolder.Products.FirstOrDefault(x => x.Id == id);
                    }
                }
            }
            return product;
        }
    }
}