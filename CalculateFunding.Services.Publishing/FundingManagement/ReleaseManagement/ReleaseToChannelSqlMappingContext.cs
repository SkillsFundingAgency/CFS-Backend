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
        }

        public Dictionary<string, ReleasedProvider> ReleasedProviders { get; }

        public Dictionary<string, ReleasedProviderVersion> ReleasedProviderVersions { get; }
    }
}
