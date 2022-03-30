using CalculateFunding.Common.Models;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System;
using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IReleaseToChannelSqlMappingContext
    {
        /// <summary>
        /// Released providers. Key is the ProviderId e.g. UKPRN
        /// </summary>
        Dictionary<string, ReleasedProvider> ReleasedProviders { get; }

        /// <summary>
        /// Released provider versions. Key is the ProviderId e.g. UKPRN
        /// </summary>
        Dictionary<string, ReleasedProviderVersion> ReleasedProviderVersions { get; }

        /// <summary>
        /// Released provider version channels. Key is providerId_channelId e.g. UKPRN_1
        /// </summary>
        Dictionary<string, Guid> ReleasedProviderVersionChannels { get; }

        Specification Specification { get; set; }

        Dictionary<int, Dictionary<OrganisationGroupResult, Guid>> FundingGroups { get; set; }

        /// <summary>
        /// FundingGroupVersions. Key is channelId, then string of funding ID for group
        /// </summary>
        Dictionary<int, Dictionary<string, FundingGroupVersion>> FundingGroupVersions { get; set; }

        string JobId { get; set; }

        Reference Author { get; set; }

        string CorrelationId { get; set; }
    }
}