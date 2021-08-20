using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Datasets.Interfaces;
using Polly;
using ObsoleteItem = CalculateFunding.Common.ApiClient.Calcs.Models.ObsoleteItems.ObsoleteItem;

namespace CalculateFunding.Services.Datasets
{
    public class CalcsRepository : ICalcsRepository
    {
        private IMapper _mapper;

        private readonly ICalculationsApiClient _apiClient;
        private readonly AsyncPolicy _apiClientPolicy;

        public CalcsRepository(ICalculationsApiClient apiClient, IDatasetsResiliencePolicies datasetsResiliencePolicies, IMapper mapper)
        {
            Guard.ArgumentNotNull(apiClient, nameof(apiClient));
            Guard.ArgumentNotNull(datasetsResiliencePolicies.CalculationsApiClient, nameof(datasetsResiliencePolicies.CalculationsApiClient));
            Guard.ArgumentNotNull(mapper, nameof(mapper));

            _apiClient = apiClient;
            _apiClientPolicy = datasetsResiliencePolicies.CalculationsApiClient;
            _mapper = mapper;
        }

        public async Task<BuildProject> GetBuildProjectBySpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            ApiResponse<Common.ApiClient.Calcs.Models.BuildProject> apiResponse = await _apiClientPolicy.ExecuteAsync(() => _apiClient.GetBuildProjectBySpecificationId(specificationId));

            return _mapper.Map<BuildProject>(apiResponse?.Content);
        }

        public async Task<BuildProject> UpdateBuildProjectRelationships(string specificationId, DatasetRelationshipSummary datasetRelationshipSummary)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            Guard.ArgumentNotNull(datasetRelationshipSummary, nameof(datasetRelationshipSummary));

            ApiResponse<Common.ApiClient.Calcs.Models.BuildProject> apiResponse = await _apiClientPolicy.ExecuteAsync(() => _apiClient.UpdateBuildProjectRelationships(specificationId, _mapper.Map<Common.ApiClient.Calcs.Models.DatasetRelationshipSummary>(datasetRelationshipSummary)));

            return _mapper.Map<BuildProject>(apiResponse?.Content);
        }

        public async Task<IEnumerable<CalculationResponseModel>> GetCurrentCalculationsBySpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            ApiResponse<IEnumerable<Common.ApiClient.Calcs.Models.Calculation>> apiResponse = await _apiClientPolicy.ExecuteAsync(() => _apiClient.GetCalculationsForSpecification(specificationId));

            return _mapper.Map<IEnumerable<CalculationResponseModel>>(apiResponse?.Content);
        }

        public async Task<HttpStatusCode> CompileAndSaveAssembly(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            ApiResponse<HttpStatusCode> apiResponse = await _apiClientPolicy.ExecuteAsync(() => _apiClient.CompileAndSaveAssembly(specificationId));

            return apiResponse.Content;
        }

        public async Task<IEnumerable<ObsoleteItem>> GetObsoleteItemsForSpecification(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            ApiResponse<IEnumerable<ObsoleteItem>> apiResponse = await _apiClientPolicy.ExecuteAsync(() => _apiClient.GetObsoleteItemsForSpecification(specificationId));

            return apiResponse.Content;
        }

        public async Task<ObsoleteItem> CreateObsoleteItem(ObsoleteItem obsoleteItem)
        {
            Guard.ArgumentNotNull(obsoleteItem, nameof(obsoleteItem));

            ApiResponse<ObsoleteItem> apiResponse = await _apiClientPolicy.ExecuteAsync(() => _apiClient.CreateObsoleteItem(obsoleteItem));

            return apiResponse.Content;
        }

        public async Task<Job> ReMapSpecificationReference(string specificationId, string datasetRelationshipId)
        {
            ApiResponse<Common.ApiClient.Calcs.Models.Job> apiResponse = await _apiClientPolicy.ExecuteAsync(() => _apiClient.ReMapSpecificationReference(specificationId, datasetRelationshipId));

            return _mapper.Map<Job>(apiResponse?.Content);
        }

        public async Task<TemplateMapping> GetTemplateMapping(string specificationId, string fundingStreamId)
        {
            ApiResponse<Common.ApiClient.Calcs.Models.TemplateMapping> apiResponse = await _apiClientPolicy.ExecuteAsync(() =>
                _apiClient.GetTemplateMapping(specificationId, fundingStreamId));

            return _mapper.Map<TemplateMapping>(apiResponse?.Content);
        }
    }
}
