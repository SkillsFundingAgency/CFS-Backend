using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.DataSets;
using CalculateFunding.Common.ApiClient.DataSets.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Migrations.Specification.Clone.Helpers;
using Polly;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Migrations.Specification.Clone.Clones
{
    public class SourceApiClient : ISourceApiClient
    {
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly ICalculationsApiClient _calculationsApiClient;
        private readonly IDatasetsApiClient _datasetsApiClient;

        private readonly AsyncPolicy _specificationsPolicy;
        private readonly AsyncPolicy _policiesPolicy;
        private readonly AsyncPolicy _calcsPolicy;
        private readonly AsyncPolicy _datasetsPolicy;

        private readonly ILogger _logger;

        public SourceApiClient(
            IBatchCloneResiliencePolicies batchCloneResiliencePolicies,
            ISpecificationsApiClient specificationsApiClient,
            IPoliciesApiClient policiesApiClient,
            ICalculationsApiClient calculationsApiClient,
            IDatasetsApiClient datasetsApiClient,
            ILogger logger)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));

            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(calculationsApiClient, nameof(calculationsApiClient));
            Guard.ArgumentNotNull(datasetsApiClient, nameof(datasetsApiClient));


            Guard.ArgumentNotNull(batchCloneResiliencePolicies, nameof(batchCloneResiliencePolicies));
            Guard.ArgumentNotNull(batchCloneResiliencePolicies.SpecificationsApiClient, nameof(batchCloneResiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(batchCloneResiliencePolicies.PoliciesApiClient, nameof(batchCloneResiliencePolicies.PoliciesApiClient));
            Guard.ArgumentNotNull(batchCloneResiliencePolicies.CalcsApiClient, nameof(batchCloneResiliencePolicies.CalcsApiClient));
            Guard.ArgumentNotNull(batchCloneResiliencePolicies.DatasetsApiClient, nameof(batchCloneResiliencePolicies.DatasetsApiClient));

            _logger = logger;

            _specificationsApiClient = specificationsApiClient;
            _policiesApiClient = policiesApiClient;
            _calculationsApiClient = calculationsApiClient;
            _datasetsApiClient = datasetsApiClient;

            _specificationsPolicy = batchCloneResiliencePolicies.SpecificationsApiClient;
            _policiesPolicy = batchCloneResiliencePolicies.PoliciesApiClient;
            _calcsPolicy = batchCloneResiliencePolicies.CalcsApiClient;
            _datasetsPolicy = batchCloneResiliencePolicies.DatasetsApiClient;

        }

        public async Task<SpecificationSummary> GetSpecificationSummaryById(string specificationId)
        {
            ApiResponse<SpecificationSummary> specificationSummaryResponse =
                await _specificationsPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(specificationId));
            specificationSummaryResponse.ValidateApiResponse(_logger, $"Error while retrieving SpecificationId={specificationId} summary.");
            return specificationSummaryResponse.Content;
        }

        public async Task<FundingPeriod> GetFundingPeriodById(string fundingPeriodId)
        {
            ApiResponse<FundingPeriod> targetFundingPeriodResponse =
                await _policiesPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingPeriodById(fundingPeriodId));
            targetFundingPeriodResponse.ValidateApiResponse(_logger, $"Error while retrieving FundingPeriodId={fundingPeriodId}");
            return targetFundingPeriodResponse.Content;
        }

        public async Task<IEnumerable<Calculation>> GetCalculationsForSpecification(string specificationId)
        {
            ApiResponse<IEnumerable<Calculation>> calculationsResponse =
                await _calcsPolicy.ExecuteAsync(() => _calculationsApiClient.GetCalculationsForSpecification(specificationId));
            calculationsResponse.ValidateApiResponse(_logger, $"GetCalculationMetadataForSpecification operation failed for SpecificationId={specificationId}");
            return calculationsResponse.Content;
        }

        public async Task<IEnumerable<DatasetSpecificationRelationshipViewModel>> GetRelationshipsBySpecificationId(string specificationId)
        {
            ApiResponse<IEnumerable<DatasetSpecificationRelationshipViewModel>> datasetSpecificationRelationshipViewModelResponse =
                    await _datasetsPolicy.ExecuteAsync(() => _datasetsApiClient.GetRelationshipsBySpecificationId(specificationId));
            datasetSpecificationRelationshipViewModelResponse.ValidateApiResponse(_logger, $"GetRelationshipsBySpecificationId operation failed for SpecificationId={specificationId}");
            return datasetSpecificationRelationshipViewModelResponse.Content;
        }


    }
}
