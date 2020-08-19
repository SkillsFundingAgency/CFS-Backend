using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Specifications;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog.Core;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    [TestClass]
    public class PublishedProviderFundingCountProcessorTests
    {
        private Mock<IPublishedFundingRepository> _publishedFunding;

        private PublishedProviderFundingCountProcessor _countProcessor;

        [TestInitialize]
        public void SetUp()
        {
            _publishedFunding = new Mock<IPublishedFundingRepository>();

            _countProcessor = new PublishedProviderFundingCountProcessor(new ProducerConsumerFactory(),
                _publishedFunding.Object,
                new ResiliencePolicies
                {
                    PublishedFundingRepository = Policy.NoOpAsync()
                },
                Logger.None);
        }

        [TestMethod]
        public async Task SumsCountAndTotalFundingFromTheCountsFromEachPublishedProviderIdBatch()
        {
            string[] pageOne = NewRandomPublishedProviderIdsPage().ToArray();
            string[] pageTwo = NewRandomPublishedProviderIdsPage().ToArray();
            string[] pageThree = NewRandomPublishedProviderIdsPage().ToArray();
            string[] publishedProviderIds = Join(pageOne, pageTwo, pageThree);

            string specificationId = NewRandomString();
            
            PublishedProviderStatus[] statuses = AsArray(NewRandomStatus(),
                NewRandomStatus());

            PublishedProviderFundingCount countOne = NewPublishedProviderFundingCount();
            PublishedProviderFundingCount countTwo = NewPublishedProviderFundingCount();
            PublishedProviderFundingCount countThree = NewPublishedProviderFundingCount();

            PublishedProviderFundingCount[] fundingCounts = AsArray(countOne, countTwo, countThree);
            
            PublishedProviderFundingCount expectedTotalCount = new PublishedProviderFundingCount
            {
                Count = fundingCounts.Sum(_ => _.Count),
                TotalFunding = fundingCounts.Sum(_ => _.TotalFunding)
            };

            GivenThePublishedProviderFundingCount(pageOne, specificationId, statuses, countOne);
            AndThePublishedProviderFundingCount(pageTwo, specificationId, statuses, countTwo);
            AndThePublishedProviderFundingCount(pageThree, specificationId, statuses, countThree);

            PublishedProviderFundingCount actualFundingCount = await WhenTheTotalFundingCountIsProcessed(publishedProviderIds, specificationId, statuses);
            
            actualFundingCount
                .Should()
                .BeEquivalentTo(expectedTotalCount);
        }

        private async Task<PublishedProviderFundingCount> WhenTheTotalFundingCountIsProcessed(IEnumerable<string> publishedProviderIds,
            string specificationId,
            PublishedProviderStatus[] statuses)
            => await _countProcessor.GetFundingCount(publishedProviderIds, specificationId, statuses);

        private void AndThePublishedProviderFundingCount(IEnumerable<string> publishedProviderIds,
            string specificationId,
            PublishedProviderStatus[] statuses,
            PublishedProviderFundingCount count)
        {
            GivenThePublishedProviderFundingCount(publishedProviderIds, specificationId, statuses, count);
        }

        private void GivenThePublishedProviderFundingCount(IEnumerable<string> publishedProviderIds,
            string specificationId,
            PublishedProviderStatus[] statuses,
            PublishedProviderFundingCount count)
        {
            _publishedFunding.Setup(_ => _.GetPublishedProviderStatusCount(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(publishedProviderIds)),
                    It.Is<string>(spec => spec == specificationId),
                    It.Is<PublishedProviderStatus[]>(sts => sts.SequenceEqual(statuses))))
                .ReturnsAsync(count);
        }

        private string[] Join(params string[][] pages) => pages.SelectMany(_ => _).ToArray();

        private IEnumerable<string> NewRandomPublishedProviderIdsPage()
        {
            for (int id = 0; id < 100; id++)
            {
                yield return NewRandomString();
            }
        }
        
        private string NewRandomString() => new RandomString();

        private TItem[] AsArray<TItem>(params TItem[] items) => items;
        
        private PublishedProviderStatus NewRandomStatus() => new RandomEnum<PublishedProviderStatus>();
        
        private PublishedProviderFundingCount NewPublishedProviderFundingCount() => new PublishedProviderFundingCountBuilder()
            .Build();
    }
}