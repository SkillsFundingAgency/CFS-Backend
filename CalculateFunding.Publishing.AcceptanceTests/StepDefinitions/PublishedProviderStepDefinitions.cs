using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Publishing.AcceptanceTests.Contexts;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.Storage.Blob;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace CalculateFunding.Publishing.AcceptanceTests.StepDefinitions
{
    [Binding]
    public class PublishedProviderStepDefinitions
    {
        private readonly IPublishedProviderStepContext _publishedProviderStepContext;
        private readonly ICurrentSpecificationStepContext _currentSpecificationStepContext;

        public PublishedProviderStepDefinitions(
            IPublishedProviderStepContext publishedProviderStepContext,
            ICurrentSpecificationStepContext currentSpecificationStepContext)
        {
            _publishedProviderStepContext = publishedProviderStepContext;
            _currentSpecificationStepContext = currentSpecificationStepContext;
        }

        [Then(@"the published provider document produced is saved to blob storage for following file name")]
        public void ThenThePublishedProviderDocumentProducedIsSavedToBlobStorageForFollowingFileName(Table table)
        {
            _publishedProviderStepContext.Should().NotBeNull();

            var publishedProviders = _publishedProviderStepContext.BlobRepo.GetFiles();

            _publishedProviderStepContext.BlobRepo.Should().NotBeNull();

            for (int i = 0; i < table.Rows.Count; i++)
            {
                string fileName = table.Rows[i][0];
                string expected = GetResourceContent(fileName);

                publishedProviders.TryGetValue(fileName, out string actual);

                actual.Should()
                        .Equals(expected);
            }
        }

        [Then(@"the published provider document produced has following metadata")]
        public void ThenThePublishedProviderDocumentHasMetadata(Table table)
        {
            _publishedProviderStepContext.BlobRepo.Should().NotBeNull();

            for (int i = 0; i < table.Rows.Count; i++)
            {
                string fileName = table.Rows[i][0];
                string metadataKey = table.Rows[i][1];
                string metadataValue = table.Rows[i][2];

                ICloudBlob cloudBlob = _publishedProviderStepContext.BlobRepo.GetBlockBlobReference(fileName);

                cloudBlob.Metadata.TryGetValue(metadataKey, out string actual);
                actual.Should().Equals(metadataValue);
            }
        }

        [Then(@"the following published provider search index items is produced for providerid with '(.*)' and '(.*)'")]
        public void ThenTheFollowingPublishedProviderSearchIndexItemsIsProducedForProviderIdWithAnd(string fundingStreamId, string fundingPeriodId, Table table)
        {
            _publishedProviderStepContext.Should().NotBeNull();

            IEnumerable<PublishedProviderIndex> providers = table.CreateSet<PublishedProviderIndex>();

            foreach (PublishedProviderIndex pubProvider in providers)
            {
                string key = $"{fundingStreamId}-{fundingPeriodId}-{pubProvider.UKPRN}";
                ConcurrentDictionary<string, PublishedProviderIndex> searchIndex = _publishedProviderStepContext.SearchRepo.PublishedProviderIndex;
                searchIndex.TryGetValue(key, out PublishedProviderIndex actual);

                actual
                .Should()
                .BeEquivalentTo(pubProvider);
            }
        }


        private static string GetResourceContent(string fileName)
        {
            Guard.IsNullOrWhiteSpace(fileName, nameof(fileName));

            string resourceName = $"CalculateFunding.Publishing.AcceptanceTests.Resources.PublishedProviders.{fileName}";

            string result = typeof(PublishedFundingStepDefinitions)
                .Assembly
                .GetEmbeddedResourceFileContents(resourceName);

            if (string.IsNullOrWhiteSpace(result))
            {
                throw new InvalidOperationException($"Unable to find resource for filename '{fileName}'");
            }

            return result;
        }
    }
}
