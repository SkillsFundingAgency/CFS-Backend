using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.Interfaces
{
    public interface IProviderVariationReasonsReleaseService
    {
        Task PopulateReleasedProviderChannelVariationReasons(IDictionary<string, IEnumerable<CalculateFunding.Models.Publishing.VariationReason>> variationReasonsForProviders, Channel channel);
    }
}
