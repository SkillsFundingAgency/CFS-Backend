using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.Versioning;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class SpecificationsInMemoryClient : ISpecificationsApiClient
    {
        private readonly Dictionary<string, SpecificationPublishDateModel> _specificationPublishDateModels 
            = new Dictionary<string, SpecificationPublishDateModel>();
        readonly Dictionary<string, IEnumerable<ProfileVariationPointer>> _profileVariationPointers 
        = new Dictionary<string, IEnumerable<ProfileVariationPointer>>();

        public Task<ValidatedApiResponse<SpecificationSummary>> CreateSpecification(CreateSpecificationModel specification)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResult<SpecificationDatasourceRelationshipSearchResultItem>> FindSpecificationAndRelationships(SearchFilterRequest filterOptions)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResult<SpecificationSearchResultItem>> FindSpecifications(SearchFilterRequest filterOptions)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<SpecificationSummary>>> GetApprovedSpecifications(string fundingPeriodId, string fundingStreamId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<Reference>>> GetFundingPeriodsByFundingStreamIds(string fundingStreamId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<PublishStatusResponseModel>> UpdateSpecificationStatus(string specificationId, PublishStatusRequestModel publishStatusRequestModel)
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> DeselectSpecificationForFunding(string specificationId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<string>>> GetDistinctFundingStreamsForSpecifications()
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<bool>> DeleteSpecificationById(string specificationName)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<bool>> PermanentDeleteSpecificationById(string specificationName)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<ProfileVariationPointer>>> GetProfileVariationPointers(string specificationId)
        {
            return _profileVariationPointers.TryGetValue(specificationId,
                out IEnumerable<ProfileVariationPointer> pointers)
                ? Task.FromResult(new ApiResponse<IEnumerable<ProfileVariationPointer>>(HttpStatusCode.OK, pointers))
                : Task.FromResult(new ApiResponse<IEnumerable<ProfileVariationPointer>>(HttpStatusCode.NotFound));
        }

        public Task<ApiResponse<SpecificationPublishDateModel>> GetPublishDates(string specificationId)
        {
            ApiResponse<SpecificationPublishDateModel> result;
            if (_specificationPublishDateModels.ContainsKey(specificationId))
            {
                result = new ApiResponse<SpecificationPublishDateModel>(HttpStatusCode.OK, _specificationPublishDateModels[specificationId]);
            }
            else
            {
                result = new ApiResponse<SpecificationPublishDateModel>(HttpStatusCode.NotFound);
            }

            return Task.FromResult(result);
        }

        public Task<ApiResponse<IEnumerable<SpecificationSummary>>> GetSpecificationSummaries()
        {
            throw new NotImplementedException();
        }

        public Task<ValidatedApiResponse<SpecificationSummary>> UpdateSpecification(string specificationId, EditSpecificationModel specification)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<SpecificationSummary>> GetSpecificationByName(string specificationName)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<SpecificationSummary>>> GetSpecifications(string fundingPeriodId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<SpecificationSummary>>> GetSelectedSpecificationsByFundingPeriodIdAndFundingStreamId(string fundingPeriodId,
            string fundingStreamId) =>
            throw new NotImplementedException();

        public Task<ApiResponse<IEnumerable<SpecificationSummary>>> GetSpecificationsSelectedForFunding()
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<SpecificationSummary>>> GetSpecificationsSelectedForFundingByPeriod(string fundingPeriodId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<SpecificationSummary>>> GetSpecificationSummaries(IEnumerable<string> specificationIds)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<SpecificationSummary>> GetSpecificationSummaryById(string specificationId)
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> SelectSpecificationForFunding(string specificationId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<SpecificationSummary>>> GetSpecificationsByFundingPeriodIdAndFundingStreamId(string fundingPeriodId,
            string fundingStreamId) =>
            throw new NotImplementedException();

        public Task<ApiResponse<IEnumerable<SpecificationSummary>>> GetSpecificationResultsByFundingPeriodIdAndFundingStreamId(string fundingPeriodId,
            string fundingStreamId) =>
            throw new NotImplementedException();

        public Task<ApiResponse<IEnumerable<SpecificationSummary>>> GetApprovedSpecificationsByFundingPeriodIdAndFundingStreamId(string fundingPeriodId,
            string fundingStreamId) =>
            throw new NotImplementedException();

        public Task<HttpStatusCode> SetAssignedTemplateVersion(string specificationId, string templateVersion, string fundingStreamId)
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> SetPublishDates(string specificationId, SpecificationPublishDateModel specificationPublishDateModel)
        {
            throw new NotImplementedException();
        }

        public void SetSpecificationPublishDateModel(string specificationId, PublishedFundingDates publishedFundingDates)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            SpecificationPublishDateModel specificationPublishDateModel = new SpecificationPublishDateModel
            {
                EarliestPaymentAvailableDate = publishedFundingDates.EarliestPaymentAvailableDate,
                ExternalPublicationDate = publishedFundingDates.ExternalPublicationDate
            };

            _specificationPublishDateModels[specificationId] = specificationPublishDateModel;
        }

        public void SetProfileVariationPointers(string specificationId,
            params ProfileVariationPointer[] variationPointers)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            _profileVariationPointers[specificationId] = variationPointers;
        }

        public Task<ApiResponse<IEnumerable<string>>> GetFundingStreamIdsForSelectedFundingSpecification()
        {
            throw new NotImplementedException();
        }

        public Task<ValidatedApiResponse<HttpStatusCode>> SetProfileVariationPointer(string specificationId, ProfileVariationPointer profileVariationPointer)
        {
            throw new NotImplementedException();
        }

        public Task<ValidatedApiResponse<HttpStatusCode>> SetProfileVariationPointers(string specificationId, IEnumerable<ProfileVariationPointer> profileVariationPointer)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<SpecificationReport>>> GetReportMetadataForSpecifications(string specificationId, string targetFundingPeriodId = null)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<SpecificationsDownloadModel>> DownloadSpecificationReport(string specificationReportIdentifier)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<JobModel>> ReIndexSpecification(string specificationId) => throw new NotImplementedException();

        public Task<HttpStatusCode> SetProviderVersion(string specificationId, string providerVersionId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<SpecificationSummary>>> GetSpecificationsWithProviderVersionUpdatesAsUseLatest()
        {
            throw new NotImplementedException();
        }

        public Task<NoValidatedContentApiResponse> UpdateFundingStructureLastModified(UpdateFundingStructureLastModifiedRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<FundingStructure>> GetFundingStructure(string fundingStreamId, string fundingPeriodId, string specificationId, string etag = null)
        {
            throw new NotImplementedException();
        }

        public Task<ValidatedApiResponse<HttpStatusCode>> MergeProfileVariationPointers(string specificationId, IEnumerable<ProfileVariationPointer> profileVariationPointer)
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> ClearForceOnNextRefresh(string specificationId)
        {
            throw new NotImplementedException();
        }
    }
}
