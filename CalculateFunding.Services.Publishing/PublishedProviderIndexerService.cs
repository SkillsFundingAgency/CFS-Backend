﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedProviderIndexerService : IPublishedProviderIndexerService
    {
        private readonly ISearchRepository<PublishedProviderIndex> _searchRepository;
        private readonly AsyncPolicy _searchPolicy;
        private readonly ILogger _logger;
        private readonly IPublishingEngineOptions _publishingEngineOptions;

        public PublishedProviderIndexerService(
            ILogger logger,
            ISearchRepository<PublishedProviderIndex> searchRepository,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            IPublishingEngineOptions publishingEngineOptions)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(publishingResiliencePolicies?.PublishedProviderSearchRepository, nameof(publishingResiliencePolicies.PublishedProviderSearchRepository));
            Guard.ArgumentNotNull(publishingEngineOptions, nameof(publishingEngineOptions));

            _logger = logger;
            _searchRepository = searchRepository;
            _publishingEngineOptions = publishingEngineOptions;
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
            });
        }

        public async Task IndexPublishedProviders(IEnumerable<PublishedProviderVersion> publishedProviderVersions)
        {
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
                            await Index(batch);
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }

            await TaskHelper.WhenAllAndThrow(allTasks.ToArray());
        }

        private async Task Index(IEnumerable<PublishedProviderVersion> publishedProviderVersions)
        {
            if (publishedProviderVersions == null)
            {
                string error = "Null published provider version supplied";
                _logger.Error(error);
                throw new NonRetriableException(error);
            }

            IEnumerable<IndexError> publishedProviderIndexingErrors = await _searchPolicy.ExecuteAsync(
                () => _searchRepository.Index(publishedProviderVersions.Select(p => CreatePublishedProviderIndex(p))));

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

        private async Task<string> Remove(IEnumerable<PublishedProviderVersion> publishedProviderVersions)
        {
            if (publishedProviderVersions == null)
            {
                string error = "Null published provider version supplied";
                _logger.Error(error);
                return error;
            }

            IEnumerable<IndexError> publishedProviderIndexingErrors = await _searchPolicy.ExecuteAsync(
                () => _searchRepository.Remove(publishedProviderVersions.Select(p => CreatePublishedProviderIndex(p))));

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

        private PublishedProviderIndex CreatePublishedProviderIndex(PublishedProviderVersion publishedProviderVersion) =>
            new PublishedProviderIndex
            {
                Id = publishedProviderVersion.PublishedProviderId,
                ProviderType = publishedProviderVersion.Provider.ProviderType,
                ProviderSubType = publishedProviderVersion.Provider.ProviderSubType,
                LocalAuthority = publishedProviderVersion.Provider.Authority,
                FundingStatus = publishedProviderVersion.Status.ToString(),
                ProviderName = publishedProviderVersion.Provider.Name,
                UKPRN = publishedProviderVersion.Provider.UKPRN,
                FundingValue = Convert.ToDouble(publishedProviderVersion.TotalFunding),
                SpecificationId = publishedProviderVersion.SpecificationId,
                FundingStreamId = publishedProviderVersion.FundingStreamId,
                FundingPeriodId = publishedProviderVersion.FundingPeriodId,
                HasErrors = publishedProviderVersion.HasErrors,
                UPIN = publishedProviderVersion.Provider.UPIN,
                URN = publishedProviderVersion.Provider.URN,
                Errors = publishedProviderVersion.Errors != null ? publishedProviderVersion
                    .Errors
                    .Select(_ => _.SummaryErrorMessage)
                    .Where(_ => !string.IsNullOrEmpty(_))
                    .Distinct()
                    .ToArraySafe()
                : Array.Empty<string>()
            };
    }
}