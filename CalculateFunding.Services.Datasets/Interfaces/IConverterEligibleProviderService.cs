using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Models.Datasets.Converter;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IConverterEligibleProviderService
    {
        Task<IEnumerable<EligibleConverter>> GetProviderIdsForConverters(string providerVersionId,
            FundingConfiguration fundingConfiguration);
    }
}