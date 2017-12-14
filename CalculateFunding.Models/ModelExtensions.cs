using System.Linq;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Models
{
    public static class ModelExtensions
    {
        public static Product GetProduct(this Implementation implementation, string id)
        {
            return null;
            //Product product = null;
            //foreach (var fundingPolicy in budget.FundingPolicies)
            //{
            //    foreach (var allocationLine in fundingPolicy.AllocationLines)
            //    {
            //        foreach (var productFolder in allocationLine.ProductFolders)
            //        {
            //            product = productFolder.Products.FirstOrDefault(x => x.Id == id);
            //        }
            //    }
            //}
            // return product;
        }
    }
}