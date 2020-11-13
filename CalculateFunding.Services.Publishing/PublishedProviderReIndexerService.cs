using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Processing;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedProviderReIndexerService : JobProcessingService, IPublishedProviderReIndexerService
    {
        private readonly ISearchRepository<PublishedProviderIndex> _searchRepository;
        private readonly AsyncPolicy _searchRepositoryResilience;
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly AsyncPolicy _publishedFundingResilience;
        private readonly ILogger _logger;

        private const int BatchSize = 1000;

        public PublishedProviderReIndexerService(ISearchRepository<PublishedProviderIndex> searchRepository,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            IPublishedFundingRepository publishedFundingRepository,
            IJobManagement jobManagement,
            ILogger logger) : base(jobManagement, logger)
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

        public override async Task Process(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            Reference user = message.GetUserDetails();

            if (user != null)
            {
                _logger.Information($"{nameof(PublishedProviderReIndexerService)} initiated by: '{user.Name}'");
            }

            string specificationId = message.GetUserProperty<string>("specification-id");

            await _publishedFundingResilience.ExecuteAsync(() => _publishedFundingRepository.AllPublishedProviderBatchProcessing(async providerVersions =>
            {
                IList<PublishedProviderIndex> results = new List<PublishedProviderIndex>();

                foreach (PublishedProvider publishedProvider in providerVersions)
                {
                    results.Add(new PublishedProviderIndex
                    {
                        Id = publishedProvider.PublishedProviderId,
                        ProviderType = publishedProvider.Current.Provider.ProviderType,
                        ProviderSubType = publishedProvider.Current.Provider.ProviderSubType,
                        LocalAuthority = publishedProvider.Current.Provider.Authority,
                        FundingStatus = publishedProvider.Current.Status.ToString(),
                        ProviderName = publishedProvider.Current.Provider.Name,
                        UKPRN = publishedProvider.Current.Provider.UKPRN,
                        FundingValue = Convert.ToDouble(publishedProvider.Current.TotalFunding),
                        SpecificationId = publishedProvider.Current.SpecificationId,
                        FundingStreamId = publishedProvider.Current.FundingStreamId,
                        FundingPeriodId = publishedProvider.Current.FundingPeriodId,
                        UPIN = publishedProvider.Current.Provider.UPIN,
                        URN = publishedProvider.Current.Provider.URN
                    });
                }

                IEnumerable<IndexError> errors = await _searchRepositoryResilience.ExecuteAsync(() => _searchRepository.Index(results));

                if (errors?.Any() == true)
                {
                    string errorMessage = $"Failed to index published provider documents with errors: {string.Join(";", errors.Select(m => m.ErrorMessage))}";

                    _logger.Error(errorMessage);

                    throw new RetriableException(errorMessage);
                }
            },
            BatchSize,
            specificationId));
        }
    }
}