using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedProviderReIndexerService : IPublishedProviderReIndexerService
    {
        private readonly ISearchRepository<PublishedProviderIndex> _searchRepository;
        private readonly Policy _searchRepositoryResilience;
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly Policy _publishedFundingResilience;
        private readonly ILogger _logger;

        private const int BatchSize = 1000;

        public PublishedProviderReIndexerService(ISearchRepository<PublishedProviderIndex> searchRepository,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            IPublishedFundingRepository publishedFundingRepository,
            ILogger logger)
        {
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(publishingResiliencePolicies?.PublishedProviderSearchRepository, nameof(publishingResiliencePolicies.PublishedProviderSearchRepository));
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(publishingResiliencePolicies?.PublishedFundingRepository, nameof(publishingResiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _searchRepository = searchRepository;
            _searchRepositoryResilience = publishingResiliencePolicies.PublishedProviderSearchRepository;
            _publishedFundingRepository = publishedFundingRepository;
            _publishedFundingResilience = publishingResiliencePolicies.PublishedFundingRepository;
            _logger = logger;
        }

        public async Task Run(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            Reference user = message.GetUserDetails();

            if (user != null)
            {
                _logger.Information($"{nameof(PublishedProviderReIndexerService)} initiated by: '{user.Name}'");
            }

            await _searchRepositoryResilience.ExecuteAsync(() => _searchRepository.DeleteIndex());

            await _publishedFundingResilience.ExecuteAsync(() => _publishedFundingRepository.AllPublishedProviderBatchProcessing(async providerVersions =>
            {
                IList<PublishedProviderIndex> results = new List<PublishedProviderIndex>();

                foreach (PublishedProviderVersion publishedProviderVersion in providerVersions)
                {
                    results.Add(new PublishedProviderIndex
                    {
                        Id = publishedProviderVersion.Id,
                        ProviderType = publishedProviderVersion.Provider.ProviderType,
                        LocalAuthority = publishedProviderVersion.Provider.LocalAuthorityName,
                        FundingStatus = publishedProviderVersion.Status.ToString(),
                        ProviderName = publishedProviderVersion.Provider.Name,
                        UKPRN = publishedProviderVersion.Provider.UKPRN,
                        FundingValue = Convert.ToDouble(publishedProviderVersion.TotalFunding),
                        SpecificationId = publishedProviderVersion.SpecificationId,
                        FundingStreamId = publishedProviderVersion.FundingStreamId,
                        FundingPeriodId = publishedProviderVersion.FundingPeriodId
                    });
                }
                
                IEnumerable<IndexError> errors = await _searchRepositoryResilience.ExecuteAsync(() => _searchRepository.Index(results));

                if (errors?.Any() == true)
                {
                    string errorMessage = $"Failed to index published provider documents with errors: {string.Join(";", errors.Select(m => m.ErrorMessage))}";

                    _logger.Error(errorMessage);

                    throw new RetriableException(errorMessage);
                }
            }, BatchSize));
        }
    }
}