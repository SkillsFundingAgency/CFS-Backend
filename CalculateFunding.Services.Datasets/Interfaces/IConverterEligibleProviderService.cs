using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Models.Datasets.Converter;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IConverterEligibleProviderService
    {
        Task<IEnumerable<ProviderConverterDetail>> GetEligibleConvertersForProviderVersion(string providerVersionId,
            FundingConfiguration fundingConfiguration);

        Task<IEnumerable<ProviderConverterDetail>> GetConvertersForProviderVersion(string providerVersionId,
            FundingConfiguration fundingConfiguration,
            Func<ProviderConverterDetail, bool> predicate = null);
    }
}