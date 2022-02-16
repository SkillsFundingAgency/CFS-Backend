using System;
using System.Collections.Generic;
using System.Linq;
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
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedFundingStatusUpdateService : IPublishedFundingStatusUpdateService
    {
        private readonly AsyncPolicy _publishingResiliencePolicy;
        private readonly AsyncPolicy _versionRepositoryPolicy;
        private readonly IVersionRepository<PublishedFundingVersion> _publishedFundingVersionRepository;
        private readonly IPublishedFundingIdGeneratorResolver _publishedFundingIdGeneratorResolver;
        private readonly ILogger _logger;
        private readonly IPublishingEngineOptions _publishingEngineOptions;
        private readonly IVersionBulkRepository<PublishedFundingVersion> _publishedFundingVersionBulkRepository;
        private readonly IPublishedFundingBulkRepository _publishedFundingBulkRepository;

        public PublishedFundingStatusUpdateService(
            IPublishingResiliencePolicies publishingResiliencePolicies,
            IVersionRepository<PublishedFundingVersion> publishedFundingVersionRepository,
            IPublishedFundingIdGeneratorResolver publishedFundingIdGeneratorResolver,
            ILogger logger,
            IPublishingEngineOptions publishingEngineOptions,
            IVersionBulkRepository<PublishedFundingVersion> publishedFundingVersionBulkRepository,
            IPublishedFundingBulkRepository publishedFundingBulkRepository)
        {
            Guard.ArgumentNotNull(publishingResiliencePolicies?.PublishedFundingRepository, nameof(publishingResiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(publishingResiliencePolicies?.PublishedProviderVersionRepository, nameof(publishingResiliencePolicies.PublishedProviderVersionRepository));
            Guard.ArgumentNotNull(publishedFundingVersionRepository, nameof(publishedFundingVersionRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(publishingEngineOptions, nameof(publishingEngineOptions));
            Guard.ArgumentNotNull(publishedFundingVersionBulkRepository, nameof(publishedFundingVersionBulkRepository));
            Guard.ArgumentNotNull(publishedFundingBulkRepository, nameof(publishedFundingBulkRepository));

            _publishingResiliencePolicy = publishingResiliencePolicies.PublishedFundingRepository;
            _publishedFundingVersionRepository = publishedFundingVersionRepository;
            _publishedFundingIdGeneratorResolver = publishedFundingIdGeneratorResolver;
            _logger = logger;
            _publishingEngineOptions = publishingEngineOptions;
            _publishedFundingVersionBulkRepository = publishedFundingVersionBulkRepository;
            _publishedFundingBulkRepository = publishedFundingBulkRepository;
            _versionRepositoryPolicy = publishingResiliencePolicies.PublishedProviderVersionRepository;
        }

        public async Task UpdatePublishedFundingStatus(
            IEnumerable<(PublishedFunding PublishedFunding, PublishedFundingVersion PublishedFundingVersion)> publishedFundingToSave, 
            PublishedFundingStatus status)
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

                            publishedFundingVersion.Status = status;
                           
                            publishedFundingVersion.MajorVersion = publishedFundingVersion.Version;

                            publishedFunding.Current = publishedFundingVersion;

                            publishedFundingVersion.FundingId = _publishedFundingIdGeneratorResolver.GetService(publishedFundingVersion.SchemaVersion).GetFundingId(publishedFundingVersion);
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }
            await TaskHelper.WhenAllAndThrow(allTasks.ToArray());

            IEnumerable<(PublishedFundingVersion newVersion, string partitionKey)> publishedFundingVersionsToSave = publishedFundingToSave
                .Select(_ => (newVersion: _.PublishedFundingVersion, partitionKey: _.PublishedFundingVersion.PartitionKey));

            List<Task<PublishedFundingVersion>> requests = new List<Task<PublishedFundingVersion>>(publishedFundingVersionsToSave.Count());

            foreach ((PublishedFundingVersion newVersion, string partitionKey) in publishedFundingVersionsToSave)
            {
                requests.Add(
                    _versionRepositoryPolicy.ExecuteAsync(() => 
                        _publishedFundingVersionBulkRepository.SaveVersion(newVersion, partitionKey)));
            }

            await TaskHelper.WhenAllAndThrow(requests.ToArray());

            foreach (Task<PublishedFundingVersion> request in requests)
            {
                Exception ex = request.Exception;
                if (ex != null && ex.InnerException != null)
                {
                    string errorMessage = $"Failed to save version when updating status:' {status}' on published funding: {request?.Result?.FundingId}.";

                    _logger.Error(ex, errorMessage);

                    throw new RetriableException(errorMessage, ex);
                }
            }

            await _publishingResiliencePolicy.ExecuteAsync(() => 
            _publishedFundingBulkRepository.UpsertPublishedFundings(
                publishedFundingToSave.Select(_ => _.PublishedFunding),
                (Task<HttpStatusCode> task, PublishedFunding publishedFunding) => 
                {
                    HttpStatusCode statusCode = task.Result;

                    if (!statusCode.IsSuccess())
                    {
                        string errorMessage = $"Failed to save published funding for id: {publishedFunding.Id} with status code {statusCode}";

                        _logger.Warning(errorMessage);

                        throw new InvalidOperationException(errorMessage);
                    }
                }));
        }
    }
}
