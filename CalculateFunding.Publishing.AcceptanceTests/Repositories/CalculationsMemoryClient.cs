using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Calcs.Models.Code;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Models.Calcs;
using BuildProject = CalculateFunding.Common.ApiClient.Calcs.Models.BuildProject;
using Calculation = CalculateFunding.Common.ApiClient.Calcs.Models.Calculation;
using CalculationCreateModel = CalculateFunding.Common.ApiClient.Calcs.Models.CalculationCreateModel;
using CalculationEditModel = CalculateFunding.Common.ApiClient.Calcs.Models.CalculationEditModel;
using CalculationMetadata = CalculateFunding.Common.ApiClient.Calcs.Models.CalculationMetadata;
using CalculationVersion = CalculateFunding.Common.ApiClient.Calcs.Models.CalculationVersion;
using DatasetRelationshipSummary = CalculateFunding.Common.ApiClient.Calcs.Models.DatasetRelationshipSummary;
using PreviewRequest = CalculateFunding.Common.ApiClient.Calcs.Models.PreviewRequest;
using PreviewResponse = CalculateFunding.Common.ApiClient.Calcs.Models.PreviewResponse;
using TemplateMapping = CalculateFunding.Common.ApiClient.Calcs.Models.TemplateMapping;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class CalculationsInMemoryClient : ICalculationsApiClient
    {
        private Dictionary<string, Dictionary<string, CalculationResult>> _calculations = new Dictionary<string, Dictionary<string, CalculationResult>>();
        private Dictionary<string, Dictionary<string, Provider>> _scopedProviders = new Dictionary<string, Dictionary<string, Provider>>();
        private IEnumerable<CalculationMetadata> _calculationMetadata;
        public TemplateMapping Mapping { get; private set; }

        public CalculationsInMemoryClient()
        {
            Mapping = new TemplateMapping();
            _calculationMetadata = new List<CalculationMetadata>();
        }

        public void SetInMemoryTemplateMapping(TemplateMapping templateMapping)
        {
            Mapping = templateMapping;
        }

        public void SetInMemoryCalculationMetaData(IEnumerable<CalculationMetadata> calculationMetadata)
        {
            _calculationMetadata = calculationMetadata.ToArray();
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

        public Task<ApiResponse<IEnumerable<CalculationMetadata>>> GetCalculationMetadataForSpecification(string specificationId)
        {
            IEnumerable<CalculationMetadata> items = _calculationMetadata.Where(c => c.SpecificationId == specificationId);

            return Task.FromResult(items.Any() ? 
                new ApiResponse<IEnumerable<CalculationMetadata>>(HttpStatusCode.OK, items) : 
                new ApiResponse<IEnumerable<CalculationMetadata>>(HttpStatusCode.NotFound));
        }

        public Task<ApiResponse<IEnumerable<Calculation>>> GetCalculationsForSpecification(string specificationId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<CalculationStatusCounts>>> GetCalculationStatusCounts(SpecificationIdsRequestModel request)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<TypeInformation>>> GetCodeContextForSpecification(string specificationId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<CalculationVersion>>> GetMultipleVersionsByCalculationId(IEnumerable<int> versionIds, string calculationId)
        {
            throw new NotImplementedException();
        }

        public async Task<ApiResponse<TemplateMapping>> GetTemplateMapping(string specificationId, string fundingStreamId)
        {
            Mapping.SpecificationId = specificationId;
            Mapping.FundingStreamId = fundingStreamId;

            ApiResponse<TemplateMapping> result = new ApiResponse<TemplateMapping>(HttpStatusCode.OK, Mapping);

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

        Task<ApiResponse<IEnumerable<CalculationSummary>>> ICalculationsApiClient.GetCalculationSummariesForSpecification(string specificationId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<SearchResults<CalculationSearchResult>>> SearchCalculationsForSpecification(string specificationId, Common.ApiClient.Calcs.Models.CalculationType calculationType, PublishStatus? status, string searchTerm = null, int? page = null)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<TemplateMapping>> ProcessTemplateMappings(string specificationId, string templateVersion, string fundingStreamId)
        {
            throw new NotImplementedException();
        }
    }
}
