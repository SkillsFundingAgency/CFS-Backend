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
using Polly;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedFundingContentsPersistanceService : IPublishedFundingContentsPersistanceService
    {
        private readonly IPublishedFundingContentsGeneratorResolver _publishedFundingContentsGeneratorResolver;
        private readonly IBlobClient _blobClient;
        private readonly Policy _publishedFundingRepositoryPolicy;


        public PublishedFundingContentsPersistanceService(
            IPublishedFundingContentsGeneratorResolver publishedFundingContentsGeneratorResolver,
            IBlobClient blobClient,
            IPublishingResiliencePolicies publishingResiliencePolicies)
        {
            Guard.ArgumentNotNull(publishedFundingContentsGeneratorResolver, nameof(publishedFundingContentsGeneratorResolver));
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));

            _publishedFundingContentsGeneratorResolver = publishedFundingContentsGeneratorResolver;
            _blobClient = blobClient;
            _publishedFundingRepositoryPolicy = publishingResiliencePolicies.PublishedFundingBlobRepository;
        }

        public async Task SavePublishedFundingContents(IEnumerable<PublishedFundingVersion> publishedFundingToSave, TemplateMetadataContents templateMetadataContents)
        {
            IPublishedFundingContentsGenerator generator = _publishedFundingContentsGeneratorResolver.GetService(templateMetadataContents.SchemaVersion);

            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: 30);
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

                            await _publishedFundingRepositoryPolicy.ExecuteAsync(() => _blobClient.UploadFileAsync(GetBlobName(publishedFundingVersion), contents));
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
            return $"{publishedFundingVersion.FundingStreamId}-{publishedFundingVersion.FundingPeriod.Id}-{publishedFundingVersion.GroupingReason.ToString()}-{publishedFundingVersion.OrganisationGroupTypeCode}-{publishedFundingVersion.OrganisationGroupIdentifierValue}-{publishedFundingVersion.MajorVersion}_{publishedFundingVersion.MinorVersion}.json";
        }
    }
}
