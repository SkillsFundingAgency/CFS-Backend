using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IFundingConfigurationService
    {
        Task<IDictionary<string, FundingConfiguration>> GetFundingConfigurations(SpecificationSummary specificationSummary);
    }
}
