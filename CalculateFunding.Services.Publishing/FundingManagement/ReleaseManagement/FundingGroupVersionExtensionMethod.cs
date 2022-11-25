using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public static class FundingGroupVersionExtensionMethod
    {
        public static IDictionary<string, IEnumerable<FundingGroupVersion>> GroupByChannelCode(this IEnumerable<FundingGroupVersion> fundingGroupVersions)
        {
            Dictionary<string, List<FundingGroupVersion>> results = new Dictionary<string, List<FundingGroupVersion>>();

            foreach (FundingGroupVersion fundingGroupVersion in fundingGroupVersions)
            {
                if (!results.TryGetValue(fundingGroupVersion.UrlKey, out List<FundingGroupVersion> outFundingGroupVersions))
                {
                    outFundingGroupVersions = new List<FundingGroupVersion>();
                    results.Add(fundingGroupVersion.UrlKey, outFundingGroupVersions);
                }

                outFundingGroupVersions.Add(fundingGroupVersion);
            }

            return results.ToDictionary(_ => _.Key, _ => _.Value.AsEnumerable());
        }
    }
}
