using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
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

        Specification Specification { get; set; }
    }
}