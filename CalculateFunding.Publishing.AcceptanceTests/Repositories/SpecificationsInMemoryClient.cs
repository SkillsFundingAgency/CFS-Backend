using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class SpecificationsInMemoryClient : ISpecificationsApiClient
    {
        private Dictionary<string, SpecificationPublishDateModel> _specificationPublishDateModels = new Dictionary<string, SpecificationPublishDateModel>();

        public Task<PagedResult<SpecificationSearchResultItem>> FindSpecifications(SearchFilterRequest filterOptions)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<SpecificationSummary>>> GetApprovedSpecifications(string fundingPeriodId, string fundingStreamId)
        {
            throw new NotImplementedException();
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

        public void SetSpecificationPublishDateModel(string specificationId, PublishedFundingDates publishedFundingDates)
        {
            Guard.ArgumentNotNull(specificationId, nameof(specificationId));

            SpecificationPublishDateModel specificationPublishDateModel = new SpecificationPublishDateModel
            {
                EarliestPaymentAvailableDate = publishedFundingDates.EarliestPaymentAvailableDate,
                ExternalPublicationDate = publishedFundingDates.ExternalPublicationDate
            };

            _specificationPublishDateModels[specificationId] = specificationPublishDateModel;
        }

        public Task<ApiResponse<IEnumerable<SpecificationSummary>>> GetSpecificationsSelectedForFunding()
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<SpecificationSummary>>> GetSpecificationsSelectedForFundingByPeriod(string fundingPeriodId)
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

        public Task<HttpStatusCode> SetAssignedTemplateVersion(string specificationId, string templateVersion, string fundingStreamId)
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> SetPublishDates(string specificationId, SpecificationPublishDateModel specificationPublishDateModel)
        {
            throw new NotImplementedException();
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

        public Task<ValidatedApiResponse<SpecificationVersion>> CreateSpecification(CreateSpecificationModel specification)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResult<SpecificationDatasourceRelationshipSearchResultItem>> FindSpecificationAndRelationships(SearchFilterRequest filterOptions)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<SpecificationSummary>>> GetSpecifications(string fundingPeriodId)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<SpecificationSummary>>> GetSpecificationSummaries(IEnumerable<string> specificationIds)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<string>>> GetFundingStreamIdsForSelectedFundingSpecification()
        {
            throw new NotImplementedException();
        }
    }
}
