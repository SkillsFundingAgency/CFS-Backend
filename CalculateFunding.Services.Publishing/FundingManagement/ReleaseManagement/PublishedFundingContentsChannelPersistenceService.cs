using Serilog;
using CalculateFunding.Services.Core;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using Polly;
using CalculateFunding.Common.Storage;
using Microsoft.Azure.Storage.Blob;
using CalculateFunding.Common.Helpers;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class PublishedFundingContentsChannelPersistenceService : IPublishedFundingContentsChannelPersistenceService
    {
        private readonly ILogger _logger;
        private readonly IPublishedFundingContentsGeneratorResolver _publishedFundingContentsGeneratorResolver;
        private readonly IBlobClient _blobClient;
        private readonly AsyncPolicy _blobClientPolicy;
        private readonly IPublishingEngineOptions _publishingEngineOptions;
        private readonly IPoliciesService _policiesService;

        public PublishedFundingContentsChannelPersistenceService(
            ILogger logger,
            IPublishedFundingContentsGeneratorResolver publishedFundingContentsGeneratorResolver,
            IBlobClient blobClient,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            IPublishingEngineOptions publishingEngineOptions,
            IPoliciesService policiesService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(publishedFundingContentsGeneratorResolver, nameof(publishedFundingContentsGeneratorResolver));
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(publishingResiliencePolicies?.PublishedFundingBlobRepository, nameof(publishingResiliencePolicies.PublishedFundingBlobRepository));
            Guard.ArgumentNotNull(publishingEngineOptions, nameof(publishingEngineOptions));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));

            _logger = logger;
            _publishedFundingContentsGeneratorResolver = publishedFundingContentsGeneratorResolver;
            _blobClient = blobClient;
            _blobClientPolicy = publishingResiliencePolicies.BlobClient;
            _publishingEngineOptions = publishingEngineOptions;
            _policiesService = policiesService;
        }

        public async Task SavePublishedFundingContents(
            IEnumerable<PublishedFundingVersion> publishedFundingToSave,
            Channel channel)
        {
            _logger.Information("Saving published funding contents");
            IDictionary<string, TemplateMetadataContents> schemaVersions = new Dictionary<string, TemplateMetadataContents>();
            
            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: _publishingEngineOptions.SavePublishedFundingContentsConcurrencyCount);
            foreach (PublishedFundingVersion publishedFundingVersion in publishedFundingToSave)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            string schemaVersionKey
                                = $"{publishedFundingVersion.FundingStreamId}-{publishedFundingVersion.FundingPeriod.Id}-{publishedFundingVersion.TemplateVersion}".ToLower();

                            if (!schemaVersions.ContainsKey(schemaVersionKey))
                            {
                                TemplateMetadataContents templateContents =
                                    await _policiesService.GetTemplateMetadataContents(
                                            publishedFundingVersion.FundingStreamId,
                                            publishedFundingVersion.FundingPeriod.Id,
                                            publishedFundingVersion.TemplateVersion);
                                schemaVersions[schemaVersionKey] = templateContents;
                            }

                            IPublishedFundingContentsGenerator generator = _publishedFundingContentsGeneratorResolver.GetService(schemaVersions[schemaVersionKey].SchemaVersion);

                            string contents = generator.GenerateContents(publishedFundingVersion, schemaVersions[schemaVersionKey]);

                            if (string.IsNullOrWhiteSpace(contents))
                            {
                                throw new RetriableException($"Generator failed to generate content for published provider version with id: '{publishedFundingVersion.Id}'");
                            }

                            string blobName = GetBlobName(publishedFundingVersion, channel);
                            await _blobClientPolicy.ExecuteAsync(() =>
                                UploadBlob(blobName, contents, GetMetadata(publishedFundingVersion)));

                            _logger.Information("Published funding contents saved to blob");
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }
            await TaskHelper.WhenAllAndThrow(allTasks.ToArray());
        }

        private string GetBlobName(PublishedFundingVersion publishedFundingVersion, Channel channel)
        {
            return $"{channel.ChannelCode}/{publishedFundingVersion.FundingStreamId}-{publishedFundingVersion.FundingPeriod.Id}-{publishedFundingVersion.GroupingReason}-{publishedFundingVersion.OrganisationGroupTypeCode}-{publishedFundingVersion.OrganisationGroupIdentifierValue}-{publishedFundingVersion.MajorVersion}_{publishedFundingVersion.MinorVersion}.json";
        }

        private async Task UploadBlob(string blobName, string contents, IDictionary<string, string> metadata)
        {
            try
            {
                await _blobClient.UploadFileAsync(blobName, contents);
                ICloudBlob blob = _blobClient.GetBlockBlobReference(blobName);
                await _blobClient.AddMetadataAsync(blob, metadata);
            }
            catch(Exception ex)
            {
                string errorMessage = $"Failed to save blob '{blobName}' to azure storage";
                _logger.Error(ex, errorMessage);
                throw new Exception(errorMessage, ex);
            }
        }

        private IDictionary<string, string> GetMetadata(PublishedFundingVersion publishedFundingVersion)
        {
            return new Dictionary<string, string>
            {
                { "specification-id",  publishedFundingVersion.SpecificationId }
            };
        }
    }
}
