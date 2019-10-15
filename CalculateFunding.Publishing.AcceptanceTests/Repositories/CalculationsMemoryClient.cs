using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Calcs.Models.Code;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Common.ApiClient.Providers.Models.Search;
using CalculateFunding.Common.ApiClient.Providers.ViewModels;
using CalculateFunding.Common.Models.Search;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Results;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class CalculationsInMemoryClient : ICalculationsApiClient
    {
        private Dictionary<string, Dictionary<string, CalculationResult>> _calculations = new Dictionary<string, Dictionary<string, CalculationResult>>();
        private Dictionary<string, Dictionary<string, Provider>> _scopedProviders = new Dictionary<string, Dictionary<string, Provider>>();
        private IEnumerable<CalculationMetadata> _calculationMetadata;
        private TemplateMapping _templateMapping;

        public CalculationsInMemoryClient(TemplateMapping templateMapping, IEnumerable<CalculationMetadata> calculationMetadata = null)
        {
            _templateMapping = templateMapping;
            _calculationMetadata = calculationMetadata;
        }

        public Task<ApiResponse<TemplateMapping>> AssociateTemplateIdWithSpecification(string specificationId, string templateVersion, string fundingStreamId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<BooleanResponseModel>> CheckHasAllApprovedTemplateCalculationsForSpecificationId(string specificationId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<HttpStatusCode>> CompileAndSaveAssembly(string specificationId)
        {
            throw new NotImplementedException();
        }

        public Task<ValidatedApiResponse<Calculation>> CreateCalculation(string specificationId, CalculationCreateModel calculationCreateModel)
        {
            throw new NotImplementedException();
        }

        public Task<ValidatedApiResponse<Calculation>> EditCalculation(string specificationId, string calculationId, CalculationEditModel calculationEditModel)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<SearchResults<CalculationSearchResult>>> FindCalculations(SearchFilterRequest filterOptions)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<CalculationVersion>>> GetAllVersionsByCalculationId(string calculationId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<byte[]>> GetAssemblyBySpecificationId(string specificationId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<BuildProject>> GetBuildProjectBySpecificationId(string specificationId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<Calculation>> GetCalculationById(string calculationId)
        {
            throw new NotImplementedException();
        }

        public async Task<ApiResponse<IEnumerable<CalculationMetadata>>> GetCalculations(string specificationId)
        {
            return await Task.FromResult(new ApiResponse<IEnumerable<CalculationMetadata>>(HttpStatusCode.OK, _calculationMetadata.Select(_ =>
            {
                _.SpecificationId = specificationId;
                return _;
            })));
        }

        public Task<ApiResponse<IEnumerable<CalculationStatusCounts>>> GetCalculationStatusCounts(SpecificationIdsRequestModel request)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<CalculationSummaryModel>>> GetCalculationSummariesForSpecification(string specificationId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<TypeInformation>>> GetCodeContextForSpecification(string specificationId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<CalculationCurrentVersion>>> GetCurrentCalculationsBySpecificationId(string specificationId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<CalculationVersion>>> GetMultipleVersionsByCalculationId(IEnumerable<int> versionIds, string calculationId)
        {
            throw new NotImplementedException();
        }

        public async Task<ApiResponse<TemplateMapping>> GetTemplateMapping(string specificationId, string fundingStreamId)
        {
            _templateMapping.SpecificationId = specificationId;
            _templateMapping.FundingStreamId = fundingStreamId;

            ApiResponse<TemplateMapping> result = new ApiResponse<TemplateMapping>(HttpStatusCode.OK, _templateMapping);

            return await Task.FromResult(result);
        }

        public Task<ApiResponse<bool>> IsCalculationNameValid(string specificationId, string calculationName, string existingCalculationId = null)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<PreviewResponse>> PreviewCompile(PreviewRequest previewRequest)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<BuildProject>> UpdateBuildProjectRelationships(string specificationId, DatasetRelationshipSummary datasetRelationshipSummary)
        {
            throw new NotImplementedException();
        }

        public Task<ValidatedApiResponse<PublishStatusResult>> UpdatePublishStatus(string calculationId, PublishStatusEditModel model)
        {
            throw new NotImplementedException();
        }
    }
}
