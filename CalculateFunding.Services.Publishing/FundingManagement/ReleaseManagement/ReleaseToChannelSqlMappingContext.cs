using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    /// <summary>
    /// Scoped class to store mappings of entities to SQL
    /// </summary>
    public class ReleaseToChannelSqlMappingContext : IReleaseToChannelSqlMappingContext
    {
        public ReleaseToChannelSqlMappingContext()
        {
            ReleasedProviders = new Dictionary<string, ReleasedProvider>();
            ReleasedProviderVersions = new Dictionary<string, ReleasedProviderVersion>();
            ReleasedProviderVersionChannels = new Dictionary<string, ReleasedProviderVersionChannel>();
            FundingGroups = new Dictionary<OrganisationGroupResult, int>();
        }

        public Dictionary<string, ReleasedProvider> ReleasedProviders { get; }

        public Dictionary<string, ReleasedProviderVersion> ReleasedProviderVersions { get; }

        public Dictionary<string, ReleasedProviderVersionChannel> ReleasedProviderVersionChannels { get; }

        public Specification Specification { get; set; }

        public Dictionary<OrganisationGroupResult, int> FundingGroups { get; set; }
    }
}
