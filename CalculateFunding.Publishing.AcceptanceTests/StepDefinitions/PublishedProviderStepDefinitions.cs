using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Publishing.AcceptanceTests.Contexts;
using CalculateFunding.Publishing.AcceptanceTests.Properties;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
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
                var content = Resources.ResourceManager.GetObject(fileName, Resources.Culture);
                string expected = GetResourceContent(fileName);

                publishedProviders.TryGetValue(fileName, out string acutal);
                
                acutal.Should()
                        .Equals(expected);
            }
            
        }

        [Then(@"the following published provider search index items is produced for providerid with '(.*)' and '(.*)'")]
        public void ThenTheFollowingPublishedProviderSearchIndexItemsIsProducedForProvideridWithAnd(string fundingStreamid, string fundingPeriodId, Table table)
        {
            _publishedProviderStepContext.Should().NotBeNull();

            IEnumerable<PublishedProviderIndex> providers = table.CreateSet<PublishedProviderIndex>();

            foreach(var pubProvider in providers)
            {
                string key = $"{pubProvider.UKPRN}-{fundingPeriodId}-{fundingStreamid}";
                var searchIndex = _publishedProviderStepContext.SearchRepo.PublishedProviderIndex;
                searchIndex.TryGetValue(key, out PublishedProviderIndex acutal);

                acutal
                .Should()
                .BeEquivalentTo(pubProvider);
            }
        }


        private static string GetResourceContent(string fileName)
        {
            byte[] result = null;

            switch (fileName)
            {
                case "PSG-AY-1920-1000000-1_0.json":
                    result = Resources.PSG_AY_1920_1000000_1_0;
                    break;
                case "PSG-AY-1920-1000002-1_0.json":
                    result = Resources.PSG_AY_1920_1000002_1_0;
                    break;
                case "PSG-AY-1920-1000101-1_0.json":
                    result = Resources.PSG_AY_1920_1000101_1_0;
                    break;
                case "PSG-AY-1920-1000102-1_0.json":
                    result = Resources.PSG_AY_1920_1000102_1_0;
                    break;
                case "PSG-AY-1920-1000201-1_0.json":
                    result = Resources.PSG_AY_1920_1000201_1_0;
                    break;
                case "PSG-AY-1920-1000202-1_0.json":
                    result = Resources.PSG_AY_1920_1000202_1_0;
                    break;
            }

            return Encoding.UTF8.GetString(result, 0, result.Length);
        }
    }
}
