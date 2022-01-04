using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.Storage.Blob;
using Polly;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedFundingContentsPersistanceService : IPublishedFundingContentsPersistanceService
    {
        private readonly IPublishedFundingContentsGeneratorResolver _publishedFundingContentsGeneratorResolver;
        private readonly IBlobClient _blobClient;
        private readonly AsyncPolicy _publishedFundingRepositoryPolicy;
        private readonly IPublishingEngineOptions _publishingEngineOptions;

        public PublishedFundingContentsPersistanceService(
            IPublishedFundingContentsGeneratorResolver publishedFundingContentsGeneratorResolver,
            IBlobClient blobClient,
            IPublishingResiliencePolicies publishingResiliencePolicies, 
            IPublishingEngineOptions publishingEngineOptions)
        {
            Guard.ArgumentNotNull(publishedFundingContentsGeneratorResolver, nameof(publishedFundingContentsGeneratorResolver));
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(publishingEngineOptions, nameof(publishingEngineOptions));
            Guard.ArgumentNotNull(publishingResiliencePolicies?.PublishedFundingBlobRepository, nameof(publishingResiliencePolicies.PublishedFundingBlobRepository));

            _publishedFundingContentsGeneratorResolver = publishedFundingContentsGeneratorResolver;
            _blobClient = blobClient;
            _publishingEngineOptions = publishingEngineOptions;
            _publishedFundingRepositoryPolicy = publishingResiliencePolicies.PublishedFundingBlobRepository;
        }

        public async Task SavePublishedFundingContents(IEnumerable<PublishedFundingVersion> publishedFundingToSave, TemplateMetadataContents templateMetadataContents)
        {
            IPublishedFundingContentsGenerator generator = _publishedFundingContentsGeneratorResolver.GetService(templateMetadataContents.SchemaVersion);

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
                            string contents = generator.GenerateContents(publishedFundingVersion, templateMetadataContents);

                            if (string.IsNullOrWhiteSpace(contents))
                            {
                                throw new RetriableException($"Generator failed to generate content for published provider version with id: '{publishedFundingVersion.Id}'");
                            }

                            string blobName = GetBlobName(publishedFundingVersion);
                            await _publishedFundingRepositoryPolicy.ExecuteAsync(() => 
                                UploadBlob(GetBlobName(publishedFundingVersion), contents, GetMetadata(publishedFundingVersion)));
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }
            await TaskHelper.WhenAllAndThrow(allTasks.ToArray());
        }

        private string GetBlobName(PublishedFundingVersion publishedFundingVersion)
        {
            return $"{publishedFundingVersion.FundingStreamId}-{publishedFundingVersion.FundingPeriod.Id}-{publishedFundingVersion.GroupingReason}-{publishedFundingVersion.OrganisationGroupTypeCode}-{publishedFundingVersion.OrganisationGroupIdentifierValue}-{publishedFundingVersion.MajorVersion}_{publishedFundingVersion.MinorVersion}.json";
        }

        private async Task UploadBlob(string blobName, string contents, IDictionary<string, string> metadata)
        {
            await _blobClient.UploadFileAsync(blobName, contents);
            ICloudBlob blob = _blobClient.GetBlockBlobReference(blobName);
            await _blobClient.AddMetadataAsync(blob, metadata);
        }

        private IDictionary<string, string> GetMetadata(PublishedFundingVersion publishedFundingVersion)
        {
            return new Dictionary<string, string>
            {
                { "specification-id",  publishedFundingVersion.SpecificationId}
            };
        }
    }
}
