using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IReleaseToChannelSqlMappingContext
    {
        /// <summary>
        /// Released providers. Key is the ProviderId eg UKPRN
        /// </summary>
        Dictionary<string, ReleasedProvider> ReleasedProviders { get; }
    }
}