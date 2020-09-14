using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Models.Publishing
{
    public class PublishedProviderFundingCount
    {
        public PublishedProviderFundingCount()
        {
            FundingStreamsFundings = Enumerable.Empty<PublishedProivderFundingStreamFunding>();
            ProviderTypes = Enumerable.Empty<ProviderTypeSubType>();
            LocalAuthorities = Enumerable.Empty<string>();
        }

        public int Count { get; set; }
        public IEnumerable<ProviderTypeSubType> ProviderTypes { get; set; }
        public int ProviderTypesCount => ProviderTypes.Count();
        public IEnumerable<string> LocalAuthorities { get; set; }
        public int LocalAuthoritiesCount => LocalAuthorities.Count();
        public IEnumerable<PublishedProivderFundingStreamFunding> FundingStreamsFundings { get; set;}
        public decimal? TotalFunding { get; set; }
    }
}