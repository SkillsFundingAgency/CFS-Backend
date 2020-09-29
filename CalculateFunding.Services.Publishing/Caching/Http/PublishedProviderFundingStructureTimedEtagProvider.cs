using CacheCow.Server;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Caching.Http
{
    public class PublishedProviderFundingStructureTimedEtagProvider : ITimedETagQueryProvider<PublishedProviderFundingStructure>
    {
        private readonly IPublishedFundingRepository _publishedFundingRepository;

        public PublishedProviderFundingStructureTimedEtagProvider(IPublishedFundingRepository publishedFundingRepository)
        {
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            _publishedFundingRepository = publishedFundingRepository;
        }

        public async Task<TimedEntityTagHeaderValue> QueryAsync(HttpContext context)
        {
            (string fundingStreamId, string fundingPeriodId, string providerId) = ExtractParamsFrom(context.Request);

            PublishedProvider publishedProvider = await _publishedFundingRepository.GetPublishedProvider(fundingStreamId, fundingPeriodId, providerId);

            if (publishedProvider == null) 
            {
                throw new NonRetriableException($"Published Provider not found for given FundingStreamId-{fundingStreamId}, FundingPeriodId-{fundingPeriodId}, ProviderId-{providerId}");
            }

            string etag = publishedProvider.Current.Version.ToString();

            return new TimedEntityTagHeaderValue(etag);
        }

        private (string fundingStreamId, string fundingPeriodId, string providerId) ExtractParamsFrom(HttpRequest request)
        {
            string publishedProviderVersionId = request.RouteValues.GetValueOrDefault("publishedProviderVersionId")?.ToString();
            Guard.IsNullOrWhiteSpace(publishedProviderVersionId, nameof(publishedProviderVersionId));
            
            string[] idParts = publishedProviderVersionId.Split('-');

            if (idParts.Length < 5)
            {
                throw new NonRetriableException($"PublishedProviderVersionId '{publishedProviderVersionId}' is not in expected format " +
                    "'publishedprovider-{providerId}-{fundingPeriodId}-{fundingStreamId}-{version}'");
            }

            // Expected format $"publishedprovider-{providerId}-{fundingPeriodId}-{fundingStreamId}-{version}"
            string providerId = idParts[1];
            string fundingStreamId = idParts[idParts.Length - 2];
            string fundingPeriodId = idParts.Skip(2).Take(idParts.Length - 4).Join("-");

            return (fundingStreamId, fundingPeriodId, providerId);
        }

        public void Dispose()
        {
        }
    }
}
