using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Core.Extensions;
using Polly;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;

namespace CalculateFunding.Services.Calcs
{
    public class CalculationNameInUseCheck : ICalculationNameInUseCheck
    {
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly AsyncPolicy _specificationsApiClientPolicy;
        private readonly AsyncPolicy _calculationRepositoryPolicy;

        public CalculationNameInUseCheck(ICalculationsRepository calculationsRepository,
            ISpecificationsApiClient specificationsApiClient,
            ICalcsResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(calculationsRepository, nameof(calculationsRepository));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.CalculationsRepository, nameof(resiliencePolicies.CalculationsRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.SpecificationsApiClient, nameof(resiliencePolicies.SpecificationsApiClient));
            
            _calculationsRepository = calculationsRepository;
            _specificationsApiClient = specificationsApiClient;
            _specificationsApiClientPolicy = resiliencePolicies.SpecificationsRepositoryPolicy;
            _calculationRepositoryPolicy = resiliencePolicies.CalculationsRepository;
        }

        public async Task<bool?> IsCalculationNameInUse(string specificationId, string calculationName, string existingCalculationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(calculationName, nameof(calculationName));

            ApiResponse<SpecModel.SpecificationSummary> specificationApiResponse = await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(specificationId));

            if(specificationApiResponse == null || !specificationApiResponse.StatusCode.IsSuccess() || specificationApiResponse.Content == null)
            {
                return null;
            }

            SpecModel.SpecificationSummary specification = specificationApiResponse.Content;

            IEnumerable<Calculation> existingCalculations = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationsBySpecificationId(specificationId));

            if (!existingCalculations.IsNullOrEmpty())
            {
                string calcSourceName = VisualBasicTypeGenerator.GenerateIdentifier(calculationName);

                foreach (Calculation calculation in existingCalculations)
                {
                    if (calculation.Id != existingCalculationId && string.Compare(calculation.Current.SourceCodeName, calcSourceName, true) == 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}