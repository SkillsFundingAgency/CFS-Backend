using CalculateFunding.Common.Models;
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

        /// <summary>
        /// The job id of the release job
        /// </summary>
        public string JobId { get; set; }

        /// <summary>
        /// Correlation id for the release job
        /// </summary>
        public string CorrelationId { get; set; }

        /// <summary>
        /// The user that initiated the release job
        /// </summary>
        public Reference Author { get; set; }
    }
}
