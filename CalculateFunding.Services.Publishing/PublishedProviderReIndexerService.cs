using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
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
        private readonly IJobManagement _jobManagement;
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
            ILogger logger)
        {
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(publishingResiliencePolicies?.PublishedProviderSearchRepository, nameof(publishingResiliencePolicies.PublishedProviderSearchRepository));
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(publishingResiliencePolicies?.PublishedFundingRepository, nameof(publishingResiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _searchRepository = searchRepository;
            _searchRepositoryResilience = publishingResiliencePolicies.PublishedProviderSearchRepository;
            _publishedFundingRepository = publishedFundingRepository;
            _publishedFundingResilience = publishingResiliencePolicies.PublishedFundingRepository;
            _jobManagement = jobManagement;
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

            string jobId = message.GetUserProperty<string>("jobId");

            Guard.IsNullOrWhiteSpace(jobId, nameof(jobId));

            JobViewModel currentJob;

            try
            {
                currentJob = await _jobManagement.RetrieveJobAndCheckCanBeProcessed(jobId);
            }
            catch
            {
                string errorMessage = "Job cannot be run";
                _logger.Error(errorMessage);

                throw new NonRetriableException(errorMessage);
            }

            // Update job to set status to processing
            await _jobManagement.UpdateJobStatus(jobId, 0, 0, null, null);

            await _searchRepositoryResilience.ExecuteAsync(() => _searchRepository.DeleteIndex());

            await _publishedFundingResilience.ExecuteAsync(() => _publishedFundingRepository.AllPublishedProviderBatchProcessing(async providerVersions =>
            {
                IList<PublishedProviderIndex> results = new List<PublishedProviderIndex>();

                foreach (PublishedProvider publishedProvider in providerVersions)
                {
                    results.Add(new PublishedProviderIndex
                    {
                        Id = publishedProvider.Current.FundingId,
                        ProviderType = publishedProvider.Current.Provider.ProviderType,
                        LocalAuthority = publishedProvider.Current.Provider.LocalAuthorityName,
                        FundingStatus = publishedProvider.Current.Status.ToString(),
                        ProviderName = publishedProvider.Current.Provider.Name,
                        UKPRN = publishedProvider.Current.Provider.UKPRN,
                        FundingValue = Convert.ToDouble(publishedProvider.Current.TotalFunding),
                        SpecificationId = publishedProvider.Current.SpecificationId,
                        FundingStreamId = publishedProvider.Current.FundingStreamId,
                        FundingPeriodId = publishedProvider.Current.FundingPeriodId
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
            BatchSize));

            _logger.Information($"Completing published provider reindex job. JobId='{jobId}'");
            await _jobManagement.UpdateJobStatus(jobId, 0, 0, true, null);
            _logger.Information($"Completed published provider reindex job. JobId='{jobId}'");
        }
    }
}