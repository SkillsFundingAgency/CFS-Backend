using System;
using System.Collections.Generic;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Publishing.AcceptanceTests.Contexts;
using CalculateFunding.Publishing.AcceptanceTests.Properties;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
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

        [Then(@"the following published provider search index items is produced for providerid with '(.*)' and '(.*)'")]
        public void ThenTheFollowingPublishedProviderSearchIndexItemsIsProducedForProvideridWithAnd(string fundingStreamid, string fundingPeriodId, Table table)
        {
            _publishedProviderStepContext.Should().NotBeNull();

            IEnumerable<PublishedProviderIndex> providers = table.CreateSet<PublishedProviderIndex>();

            foreach (var pubProvider in providers)
            {
                string key = $"{pubProvider.UKPRN}-{fundingPeriodId}-{fundingStreamid}";
                var searchIndex = _publishedProviderStepContext.SearchRepo.PublishedProviderIndex;
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
