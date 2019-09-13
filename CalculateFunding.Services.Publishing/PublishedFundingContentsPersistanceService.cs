using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedFundingContentsPersistanceService : IPublishedFundingContentsPersistanceService
    {
        private readonly IPublishedFundingContentsGeneratorResolver _publishedFundingContentsGeneratorResolver;
        private readonly IBlobClient _blobClient;
        private readonly Policy _publishedFundingRepositoryPolicy;
        private readonly ISearchRepository<PublishedFundingIndex> _searchRepository;

        public PublishedFundingContentsPersistanceService(IPublishedFundingContentsGeneratorResolver publishedFundingContentsGeneratorResolver,
            IBlobClient blobClient,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            ISearchRepository<PublishedFundingIndex> searchRepository)
        {
            Guard.ArgumentNotNull(publishedFundingContentsGeneratorResolver, nameof(publishedFundingContentsGeneratorResolver));
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));

            _publishedFundingContentsGeneratorResolver = publishedFundingContentsGeneratorResolver;
            _blobClient = blobClient;
            _publishedFundingRepositoryPolicy = publishingResiliencePolicies.PublishedFundingBlobRepository;
            _searchRepository = searchRepository;
        }

        public async Task SavePublishedFundingContents(IEnumerable<PublishedFundingVersion> publishedFundingToSave, TemplateMetadataContents templateMetadataContents)
        {
            IPublishedFundingContentsGenerator generator = _publishedFundingContentsGeneratorResolver.GetService(templateMetadataContents.SchemaVersion);

            foreach (PublishedFundingVersion publishedFundingVersion in publishedFundingToSave)
            {
                string contents = generator.GenerateContents(publishedFundingVersion, templateMetadataContents);

                if (string.IsNullOrWhiteSpace(contents))
                {
                    throw new RetriableException($"Generator failed to generate content for published provider version with id: '{publishedFundingVersion.Id}'");
                }

                await _publishedFundingRepositoryPolicy.ExecuteAsync(() => _blobClient.UploadFileAsync(GetBlobName(publishedFundingVersion), contents));

                await _searchRepository.RunIndexer();
            }
        }

        private string GetBlobName(PublishedFundingVersion publishedFundingVersion)
        {
            return $"{publishedFundingVersion.FundingStreamId}-{publishedFundingVersion.FundingPeriod.Id}-{publishedFundingVersion.GroupingReason.ToString()}-{publishedFundingVersion.OrganisationGroupTypeIdentifier}-{publishedFundingVersion.OrganisationGroupIdentifierValue}-{publishedFundingVersion.MajorVersion}-{publishedFundingVersion.MinorVersion}";
        }
    }
}
