using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Specifications;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    [TestClass]
    public class PublishedProviderFundingCsvDataProcessorTests
    {
        private IPublishedFundingRepository _publishedFunding;

        private PublishedProviderFundingCsvDataProcessor _processor;

        [TestInitialize]
        public void SetUp()
        {
            _publishedFunding = Substitute.For<IPublishedFundingRepository>();

            _processor = new PublishedProviderFundingCsvDataProcessor(new ProducerConsumerFactory(),
                _publishedFunding,
                new ResiliencePolicies
                {
                    PublishedFundingRepository = Policy.NoOpAsync()
                },
                Logger.None);
        }

        [TestMethod]
        public async Task ShouldRetrievePublishedFundingCsvDataFromEachPublishedProviderIdBatch()
        {
            string[] pageOne = NewRandomPublishedProviderIdsPage().ToArray();
            string[] pageTwo = NewRandomPublishedProviderIdsPage().ToArray();
            string[] pageThree = NewRandomPublishedProviderIdsPage().ToArray();
            string[] publishedProviderIds = Join(pageOne, pageTwo, pageThree);

            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            string providerName1 = NewRandomString();
            string providerName2 = NewRandomString();
            string providerName3 = NewRandomString();

            decimal totalFunding1 = NewRandomNumber();
            decimal totalFunding2 = NewRandomNumber();
            decimal totalFunding3 = NewRandomNumber();

            PublishedProviderStatus[] statuses = AsArray(NewRandomStatus(), NewRandomStatus());

            PublishedProviderFundingCsvData fundingOne = NewPublishedProviderFundingCsvData(_ => _.WithSpecificationId(specificationId)
                                                                                    .WithFundingStreamId(fundingStreamId)
                                                                                    .WithFundingPeriodId(fundingPeriodId)
                                                                                    .WithTotalFunding(totalFunding1)
                                                                                    .WithProviderName(providerName1));
            PublishedProviderFundingCsvData fundingTwo = NewPublishedProviderFundingCsvData(_ => _.WithSpecificationId(specificationId)
                                                                                    .WithFundingStreamId(fundingStreamId)
                                                                                    .WithFundingPeriodId(fundingPeriodId)
                                                                                    .WithTotalFunding(totalFunding2)
                                                                                    .WithProviderName(providerName2));
            PublishedProviderFundingCsvData fundingThree = NewPublishedProviderFundingCsvData(_ => _.WithSpecificationId(specificationId)
                                                                                    .WithFundingStreamId(fundingStreamId)
                                                                                    .WithFundingPeriodId(fundingPeriodId)
                                                                                    .WithTotalFunding(totalFunding3)
                                                                                    .WithProviderName(providerName3));

            PublishedProviderFundingCsvData[] fundings = AsArray(fundingOne, fundingTwo, fundingThree);

            IEnumerable<PublishedProviderFundingCsvData> expectedFundingsCsvData = new []
            {
                fundingOne, fundingTwo, fundingThree
            };

            GivenThePublishedProvidersFundingCsvData(pageOne, specificationId, statuses, new[] { fundingOne });
            AndThePublishedProvidersFundingCsvData(pageTwo, specificationId, statuses, new[] { fundingTwo });
            AndThePublishedProvidersFundingCsvData(pageThree, specificationId, statuses, new[] { fundingThree });

            IEnumerable<PublishedProviderFundingCsvData> actualFundings = await WhenTheGetFundingDataIsProcessed(publishedProviderIds, specificationId, statuses);

            actualFundings
                .Count()
                .Should()
                .Be(expectedFundingsCsvData.Count());

            actualFundings
                .Should()
                .BeEquivalentTo(expectedFundingsCsvData);
        }

        private async Task<IEnumerable<PublishedProviderFundingCsvData>> WhenTheGetFundingDataIsProcessed(IEnumerable<string> publishedProviderIds,
            string specificationId,
            PublishedProviderStatus[] statuses)
            => await _processor.GetFundingData(publishedProviderIds, specificationId, statuses);

        private void AndThePublishedProvidersFundingCsvData(IEnumerable<string> publishedProviderIds,
            string specificationId,
            PublishedProviderStatus[] statuses,
            IEnumerable<PublishedProviderFundingCsvData> fundings)
        {
            GivenThePublishedProvidersFundingCsvData(publishedProviderIds, specificationId, statuses, fundings);
        }

        private void GivenThePublishedProvidersFundingCsvData(IEnumerable<string> publishedProviderIds,
            string specificationId,
            PublishedProviderStatus[] statuses,
            IEnumerable<PublishedProviderFundingCsvData> fundings)
        {
            _publishedFunding.GetPublishedProvidersFundingDataForCsvReport(Arg.Is<IEnumerable<string>>(ids => ids.SequenceEqual(publishedProviderIds)),
                    Arg.Is<string>(spec => spec == specificationId),
                    Arg.Is<PublishedProviderStatus[]>(sts => sts.SequenceEqual(statuses)))
                .Returns(fundings);
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

        private PublishedProviderFundingCsvData NewPublishedProviderFundingCsvData(Action<PublishedProviderFundingCsvDataBuilder> setUp = null)
        {
            PublishedProviderFundingCsvDataBuilder builder = new PublishedProviderFundingCsvDataBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }
    }
}
