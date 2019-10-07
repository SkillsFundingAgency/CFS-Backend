using System;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedFundingDateService : IPublishedFundingDateService
    {
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly Policy _specificationsApiPolicy;

        public PublishedFundingDateService(ISpecificationsApiClient specificationsApiClient, IPublishingResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));

            _specificationsApiClient = specificationsApiClient;
            _specificationsApiPolicy = resiliencePolicies.SpecificationsRepositoryPolicy;
        }

        public async Task<PublishedFundingDates> GetDatesForSpecification(string specificationId)
        {
            PublishedFundingDates result;

            ApiResponse<SpecificationPublishDateModel> publishDatesResponse = await _specificationsApiPolicy.ExecuteAsync(() =>
                _specificationsApiClient.GetPublishDates(specificationId));

            DateTime statusChangedDate = DateTime.Now;
            DateTime earliestPaymentAvailableDate = DateTime.Now;
            DateTime externalPublicationDate = DateTime.Now;

            if (publishDatesResponse?.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new InvalidOperationException($"Get Publish Dates API returned non success response. Response status code {publishDatesResponse.StatusCode}");
            }

            earliestPaymentAvailableDate =
                publishDatesResponse.Content.EarliestPaymentAvailableDate.HasValue
                ? publishDatesResponse.Content.EarliestPaymentAvailableDate.Value.UtcDateTime
                : DateTime.Now;

            externalPublicationDate =
                publishDatesResponse.Content.ExternalPublicationDate.HasValue
                ? publishDatesResponse.Content.ExternalPublicationDate.Value.UtcDateTime
                : DateTime.Now;

            result = new PublishedFundingDates()
            {
                StatusChangedDate = statusChangedDate,
                EarliestPaymentAvailableDate = earliestPaymentAvailableDate,
                ExternalPublicationDate = externalPublicationDate,
            };

            return result;
        }
    }
}
