using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedFundingStatusUpdateService : IPublishedFundingStatusUpdateService
    {
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly AsyncPolicy _publishingResiliencePolicy;
        private readonly IVersionRepository<PublishedFundingVersion> _publishedFundingVersionRepository;
        private readonly IPublishedFundingIdGeneratorResolver _publishedFundingIdGeneratorResolver;
        private readonly ILogger _logger;
        private readonly IPublishingEngineOptions _publishingEngineOptions;

        public PublishedFundingStatusUpdateService(IPublishedFundingRepository publishedFundingRepository,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            IVersionRepository<PublishedFundingVersion> publishedFundingVersionRepository,
            IPublishedFundingIdGeneratorResolver publishedFundingIdGeneratorResolver,
            ILogger logger,
            IPublishingEngineOptions publishingEngineOptions)
        {
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(publishingResiliencePolicies?.PublishedFundingRepository, nameof(publishingResiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(publishedFundingVersionRepository, nameof(publishedFundingVersionRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(publishingEngineOptions, nameof(publishingEngineOptions));

            _publishedFundingRepository = publishedFundingRepository;
            _publishingResiliencePolicy = publishingResiliencePolicies.PublishedFundingRepository;
            _publishedFundingVersionRepository = publishedFundingVersionRepository;
            _publishedFundingIdGeneratorResolver = publishedFundingIdGeneratorResolver;
            _logger = logger;
            _publishingEngineOptions = publishingEngineOptions;
        }

        public async Task UpdatePublishedFundingStatus(IEnumerable<(PublishedFunding PublishedFunding, PublishedFundingVersion PublishedFundingVersion)> publishedFundingToSave, Reference author, PublishedFundingStatus released)
        {
            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: _publishingEngineOptions.UpdatePublishedFundingStatusConcurrencyCount);
            foreach ((PublishedFunding PublishedFunding, PublishedFundingVersion PublishedFundingVersion) _ in publishedFundingToSave)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            PublishedFunding publishedFunding = _.PublishedFunding;

                            PublishedFundingVersion currentVersion = publishedFunding.Current;

                            PublishedFundingVersion publishedFundingVersion = await _publishedFundingVersionRepository
                                .CreateVersion(_.PublishedFundingVersion, currentVersion, currentVersion.PartitionKey);

                            publishedFundingVersion.Status = released;
                            publishedFundingVersion.Author = author;
                            publishedFundingVersion.MajorVersion = publishedFundingVersion.Version;

                            publishedFunding.Current = publishedFundingVersion;

                            publishedFundingVersion.FundingId = _publishedFundingIdGeneratorResolver.GetService(publishedFundingVersion.SchemaVersion).GetFundingId(publishedFundingVersion);

                            try
                            {
                                await _publishedFundingVersionRepository.SaveVersion(publishedFundingVersion, publishedFundingVersion.PartitionKey);
                            }
                            catch (Exception ex)
                            {
                                string errorMessage = $"Failed to save version when updating status:' {released}' on published funding: {publishedFundingVersion.FundingId}.";

                                _logger.Error(ex, errorMessage);

                                throw new RetriableException(errorMessage, ex);
                            }

                            HttpStatusCode statusCode = await _publishingResiliencePolicy.ExecuteAsync(() => _publishedFundingRepository.UpsertPublishedFunding(publishedFunding));

                            if (!statusCode.IsSuccess())
                            {
                                string errorMessage = $"Failed to save published funding for id: {publishedFunding.Id} with status code {statusCode.ToString()}";

                                _logger.Warning(errorMessage);

                                throw new InvalidOperationException(errorMessage);
                            }
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }
            await TaskHelper.WhenAllAndThrow(allTasks.ToArray());
        }
    }
}
