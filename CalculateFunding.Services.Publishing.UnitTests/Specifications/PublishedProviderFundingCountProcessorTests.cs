using System;
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
            string fundingStreamId1 = NewRandomString();
            string fundingStreamId2 = NewRandomString();
            string providerType1 = NewRandomString();
            string providerSubType1 = NewRandomString();
            string providerSubType2 = NewRandomString();
            string laCode1 = NewRandomString();
            string laCode2 = NewRandomString();

            decimal totalFunding1 = NewRandomNumber();
            decimal totalFunding2 = NewRandomNumber();
            decimal totalFunding3 = NewRandomNumber();

            PublishedProviderStatus[] statuses = AsArray(NewRandomStatus(), NewRandomStatus());

            PublishedProviderFunding fundingOne = NewPublishedProviderFunding(_ => _.WithSpecificationId(specificationId)
                                                                                    .WithPublishedProviderId(pageOne.First())
                                                                                    .WithFundingStreamId(fundingStreamId1)
                                                                                    .WithProviderType(providerType1)
                                                                                    .WithProviderSubType(providerSubType1)
                                                                                    .WithTotalFunding(totalFunding1)
                                                                                    .WithLaCode(laCode1));
            PublishedProviderFunding fundingTwo = NewPublishedProviderFunding(_ => _.WithSpecificationId(specificationId)
                                                                                    .WithPublishedProviderId(pageTwo.Last())
                                                                                    .WithFundingStreamId(fundingStreamId2)
                                                                                    .WithProviderType(providerType1)
                                                                                    .WithProviderSubType(providerSubType2)
                                                                                    .WithTotalFunding(totalFunding2)
                                                                                    .WithLaCode(laCode2));
            PublishedProviderFunding fundingThree = NewPublishedProviderFunding(_ => _.WithSpecificationId(specificationId)
                                                                                      .WithPublishedProviderId(pageThree.Skip(1).First())
                                                                                      .WithFundingStreamId(fundingStreamId1)
                                                                                      .WithProviderType(providerType1)
                                                                                      .WithProviderSubType(providerSubType1)
                                                                                      .WithTotalFunding(totalFunding3)
                                                                                      .WithLaCode(laCode1));

            PublishedProviderFunding[] fundings = AsArray(fundingOne, fundingTwo, fundingThree);

            PublishedProviderFundingCount expectedTotalCount = new PublishedProviderFundingCount
            {
                Count = fundings.Count(),
                TotalFunding = fundings.Sum(_ => _.TotalFunding)
            };

            GivenThePublishedProvidersFunding(pageOne, specificationId, statuses, new[] { fundingOne });
            AndThePublishedProviderFundingCount(pageTwo, specificationId, statuses, new[] { fundingTwo });
            AndThePublishedProviderFundingCount(pageThree, specificationId, statuses, new[] { fundingThree });

            PublishedProviderFundingCount actualFundingCount = await WhenTheTotalFundingCountIsProcessed(publishedProviderIds, specificationId, statuses);

            actualFundingCount
                .Count
                .Should()
                .Be(fundings.Length);

            actualFundingCount
                .ProviderTypesCount
                .Should()
                .Be(2);

            actualFundingCount
                .ProviderTypes
                .Should()
                .BeEquivalentTo(new[]
                {
                    new ProviderTypeSubType() {ProviderType = providerType1, ProviderSubType = providerSubType1 },
                    new ProviderTypeSubType() {ProviderType = providerType1, ProviderSubType = providerSubType2 }
                });

            actualFundingCount
                .LocalAuthoritiesCount
                .Should()
                .Be(2);

            actualFundingCount
                .LocalAuthorities
                .Should()
                .BeEquivalentTo(new[] { laCode1, laCode2 });

            actualFundingCount
                .FundingStreamsFundings
                .Should()
                .BeEquivalentTo(new[]
                {
                    new PublishedProivderFundingStreamFunding() { FundingStreamId = fundingStreamId1, TotalFunding = totalFunding1 + totalFunding3},
                    new PublishedProivderFundingStreamFunding() { FundingStreamId = fundingStreamId2, TotalFunding = totalFunding2}
                });

            actualFundingCount
                .TotalFunding
                .Should()
                .Be(totalFunding1 + totalFunding2 + totalFunding3);
        }

        private async Task<PublishedProviderFundingCount> WhenTheTotalFundingCountIsProcessed(IEnumerable<string> publishedProviderIds,
            string specificationId,
            PublishedProviderStatus[] statuses)
            => await _countProcessor.GetFundingCount(publishedProviderIds, specificationId, statuses);

        private void AndThePublishedProviderFundingCount(IEnumerable<string> publishedProviderIds,
            string specificationId,
            PublishedProviderStatus[] statuses,
            IEnumerable<PublishedProviderFunding> fundings)
        {
            GivenThePublishedProvidersFunding(publishedProviderIds, specificationId, statuses, fundings);
        }

        private void GivenThePublishedProvidersFunding(IEnumerable<string> publishedProviderIds,
            string specificationId,
            PublishedProviderStatus[] statuses,
            IEnumerable<PublishedProviderFunding> fundings)
        {
            _publishedFunding.Setup(_ => _.GetPublishedProvidersFunding(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(publishedProviderIds)),
                    It.Is<string>(spec => spec == specificationId),
                    It.Is<PublishedProviderStatus[]>(sts => sts.SequenceEqual(statuses))))
                .ReturnsAsync(fundings);
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
        private decimal NewRandomNumber() => new RandomNumberBetween(0, int.MaxValue);

        private TItem[] AsArray<TItem>(params TItem[] items) => items;

        private PublishedProviderStatus NewRandomStatus() => new RandomEnum<PublishedProviderStatus>();

        private PublishedProviderFunding NewPublishedProviderFunding(Action<PublishedProviderFundingBuilder> setUp = null)
        {
            PublishedProviderFundingBuilder builder = new PublishedProviderFundingBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }
    }
}