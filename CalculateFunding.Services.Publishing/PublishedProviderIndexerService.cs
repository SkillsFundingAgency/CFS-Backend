using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;
using Serilog;
using static CalculateFunding.Services.Core.Extensions.DateRangeExtensions;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedProviderIndexerService : IPublishedProviderIndexerService
    {
        private readonly ISearchRepository<PublishedProviderIndex> _searchRepository;
        private readonly IPoliciesApiClient _policies;
        private readonly AsyncPolicy _policiesPolicy;
        private readonly AsyncPolicy _searchPolicy;
        private readonly ILogger _logger;
        private readonly IPublishingEngineOptions _publishingEngineOptions;

        public PublishedProviderIndexerService(
            ILogger logger,
            ISearchRepository<PublishedProviderIndex> searchRepository,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            IPublishingEngineOptions publishingEngineOptions,
            IPoliciesApiClient policies)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(publishingResiliencePolicies?.PublishedProviderSearchRepository, nameof(publishingResiliencePolicies.PublishedProviderSearchRepository));
            Guard.ArgumentNotNull(publishingEngineOptions, nameof(publishingEngineOptions));
            Guard.ArgumentNotNull(policies, nameof(policies));
            Guard.ArgumentNotNull(publishingResiliencePolicies.PoliciesApiClient, nameof(publishingResiliencePolicies.PoliciesApiClient));

            _logger = logger;
            _searchRepository = searchRepository;
            _publishingEngineOptions = publishingEngineOptions;
            _policies = policies;
            _policiesPolicy = publishingResiliencePolicies.PoliciesApiClient;
            _searchPolicy = publishingResiliencePolicies.PublishedProviderSearchRepository;
        }

        public async Task IndexPublishedProvider(PublishedProviderVersion publishedProviderVersion)
        {
            if (publishedProviderVersion == null)
            {
                string error = "Null published provider version supplied";
                _logger.Error(error);
                throw new NonRetriableException(error);
            }

            await Index(new[]
            {
                publishedProviderVersion
            },
                new ConcurrentDictionary<string, HashSet<string>>());
        }

        public async Task IndexPublishedProviders(IEnumerable<PublishedProviderVersion> publishedProviderVersions)
        {
            ConcurrentDictionary<string, HashSet<string>> fundingPeriodDates = new ConcurrentDictionary<string, HashSet<string>>();
            
            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(_publishingEngineOptions.IndexPublishedProvidersConcurrencyCount);
            foreach (IEnumerable<PublishedProviderVersion> batch in publishedProviderVersions.ToBatches(100))
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            await Index(batch, fundingPeriodDates);
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }

            await TaskHelper.WhenAllAndThrow(allTasks.ToArray());
        }

        private async Task Index(IEnumerable<PublishedProviderVersion> publishedProviderVersions,
            ConcurrentDictionary<string, HashSet<string>> fundingPeriodDates)
        {
            if (publishedProviderVersions == null)
            {
                string error = "Null published provider version supplied";
                
                _logger.Error(error);
                
                throw new NonRetriableException(error);
            }

            IEnumerable<IndexError> publishedProviderIndexingErrors = await _searchPolicy.ExecuteAsync(
                () => _searchRepository.Index(publishedProviderVersions.Select(_ => CreatePublishedProviderIndex(_, fundingPeriodDates))));

            List<IndexError> publishedProviderIndexingErrorsAsList = publishedProviderIndexingErrors.ToList();
            if (!publishedProviderIndexingErrorsAsList.IsNullOrEmpty())
            {
                string publishedProviderIndexingErrorsConcatted = string.Join(". ", publishedProviderIndexingErrorsAsList.Select(e => e.ErrorMessage));
                string formattedErrorMessage =
                    $"Could not index Published Providers because: {publishedProviderIndexingErrorsConcatted}";
                _logger.Error(formattedErrorMessage);
                throw new RetriableException(formattedErrorMessage);
            }
        }

        public async Task<string> Remove(IEnumerable<PublishedProviderVersion> publishedProviderVersions)
        {
            if (publishedProviderVersions == null)
            {
                string error = "Null published provider version supplied";
                _logger.Error(error);
                return error;
            }

            IEnumerable<IndexError> publishedProviderIndexingErrors = await _searchPolicy.ExecuteAsync(
                () => _searchRepository.Remove(publishedProviderVersions.Select(_ => CreatePublishedProviderIndex(_))));

            List<IndexError> publishedProviderIndexingErrorsAsList = publishedProviderIndexingErrors.ToList();
            if (!publishedProviderIndexingErrorsAsList.IsNullOrEmpty())
            {
                string publishedProviderIndexingErrorsConcatted = string.Join(". ", publishedProviderIndexingErrorsAsList.Select(e => e.ErrorMessage));
                string formattedErrorMessage =
                    $"Could not index Published Providers because: {publishedProviderIndexingErrorsConcatted}";
                return formattedErrorMessage;
            }

            return string.Empty;
        }

        private PublishedProviderIndex CreatePublishedProviderIndex(PublishedProviderVersion publishedProviderVersion,
            ConcurrentDictionary<string, HashSet<string>> fundingPeriodMonths = null)
        {
            Provider provider = publishedProviderVersion.Provider;

            string monthYearOpened = GetMonthYearOpened(publishedProviderVersion, fundingPeriodMonths)
                .GetAwaiter()
                .GetResult();
            
            return new PublishedProviderIndex
            {
                Id = publishedProviderVersion.PublishedProviderId,
                ProviderType = provider.ProviderType,
                ProviderSubType = provider.ProviderSubType,
                LocalAuthority = provider.Authority,
                FundingStatus = publishedProviderVersion.Status.ToString(),
                ProviderName = provider.Name,
                UKPRN = provider.UKPRN,
                FundingValue = Convert.ToDouble(publishedProviderVersion.TotalFunding),
                SpecificationId = publishedProviderVersion.SpecificationId,
                FundingStreamId = publishedProviderVersion.FundingStreamId,
                FundingPeriodId = publishedProviderVersion.FundingPeriodId,
                HasErrors = publishedProviderVersion.HasErrors,
                UPIN = provider.UPIN,
                URN = provider.URN,
                DateOpened = provider.DateOpened,
                MonthYearOpened = monthYearOpened,
                Indicative = publishedProviderVersion.IsIndicative ? "Only indicative allocations" : "Hide indicative allocations",
                Errors = publishedProviderVersion.Errors != null
                    ? publishedProviderVersion
                        .Errors
                        .Select(_ => _.SummaryErrorMessage)
                        .Where(_ => !string.IsNullOrEmpty(_))
                        .Distinct()
                        .ToArraySafe()
                    : Array.Empty<string>()
            };
        }

        private async Task<string> GetMonthYearOpened(PublishedProviderVersion publishedProviderVersion,
            ConcurrentDictionary<string, HashSet<string>> fundingPeriodMonths = null)
        {
            string monthYearOpened = publishedProviderVersion.Provider.DateOpened?.ToString("MMMM yyyy");

            if (fundingPeriodMonths == null)
            {
                return monthYearOpened;
            }

            string fundingPeriodId = publishedProviderVersion.FundingPeriodId;

            if (!fundingPeriodMonths.TryGetValue(fundingPeriodId, out HashSet<string> validMonths))
            {
                validMonths = await GetFundingPeriodsMonths(fundingPeriodId);

                fundingPeriodMonths.AddOrUpdate(fundingPeriodId,
                    _ => validMonths, (_,current) => validMonths);
            }

            return validMonths?.Contains(monthYearOpened) == true ? monthYearOpened : null;
        }

        private async Task<HashSet<string>> GetFundingPeriodsMonths(string fundingPeriodId)
        {
            FundingPeriod fundingPeriod = (await _policiesPolicy.ExecuteAsync(() => _policies.GetFundingPeriodById(fundingPeriodId)))?.Content;

            if (fundingPeriod == null)
            {
                _logger.Error($"Request failed to find funding funding period {fundingPeriodId}");

                return null;
            }

            return new HashSet<string>(GetMonthsBetween(fundingPeriod.StartDate, fundingPeriod.EndDate));
        }
    }
}