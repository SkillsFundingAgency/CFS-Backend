using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Specifications;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    [TestClass]
    public class PublishedProviderLookupServiceTests
    {
        private string _specificationId;
        private string[] _pageOne;
        private string[] _pageTwo;
        private string[] _pageThree;
        private string[] _publishedProviderIds;
        private SpecificationSummary _specificationSummary;
        private PublishedProviderLookupService _publishedProviderLookupService;

        private Mock<IPublishedFundingRepository> _publishedFunding;

        [TestInitialize]
        public void SetUp()
        {
            _publishedFunding = new Mock<IPublishedFundingRepository>();

            _specificationId = NewRandomString();
            _specificationSummary = NewSpecificationSummary(_ => _.WithId(_specificationId));

            _pageOne = NewRandomPublishedProviderIdsPage().ToArray();
            _pageTwo = NewRandomPublishedProviderIdsPage().ToArray();
            _pageThree = NewRandomPublishedProviderIdsPage().ToArray();
            _publishedProviderIds = Join(_pageOne, _pageTwo, _pageThree);

            _publishedProviderLookupService = new PublishedProviderLookupService(new ProducerConsumerFactory(),
                _publishedFunding.Object,
                new ResiliencePolicies
                {
                    PublishedFundingRepository = Policy.NoOpAsync()
                },
                Logger.None);
        }

        /// <summary>
        /// 3 Published Providers
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task LookupPublishedProviderSummaries()
        {
            PublishedProviderFundingSummary[] fundings = GenerateFundings();

            IEnumerable<PublishedProviderFundingSummary> publishedProviderFundingSummaries = await WhenTheFundingSummaryAreRequested();

            publishedProviderFundingSummaries.Count().Should().Be(3);
        }

        private PublishedProviderFundingSummary[] GenerateFundings()
        {
            decimal totalFunding1 = NewRandomNumber();
            decimal totalFunding2 = NewRandomNumber();
            decimal totalFunding3 = NewRandomNumber();

            int majorVersionOne = 10;
            int majorVersionTwo = 20;
            int majorVersionThree = 30;

            PublishedProviderFundingSummary fundingOne = NewPublishedProviderFunding(_ => _
                .WithSpecificationId(_specificationId)
                .WithStatus("Approved")
                .WithProvider(
                    NewProvider(p => p.WithProviderType(NewRandomString()).WithProviderSubType(NewRandomString()).WithProviderId(_pageOne.First())))
                .WithIsIndicative(true)
                .WithMajorVersion(majorVersionOne)
                .WithMinorVersion(majorVersionOne)
                .WithTotalFunding(totalFunding1));
            PublishedProviderFundingSummary fundingTwo = NewPublishedProviderFunding(_ => _
                .WithSpecificationId(_specificationId)
                .WithStatus("Released")
                .WithProvider(
                    NewProvider(p => p.WithProviderType(NewRandomString()).WithProviderSubType(NewRandomString()).WithProviderId(_pageTwo.Last())))
                .WithIsIndicative(false)
                .WithMajorVersion(majorVersionTwo)
                .WithMinorVersion(majorVersionTwo)
                .WithTotalFunding(totalFunding2));
            PublishedProviderFundingSummary fundingThree = NewPublishedProviderFunding(_ => _
                .WithSpecificationId(_specificationId)
                .WithStatus("Approved")
                .WithProvider(
                    NewProvider(p => p.WithProviderType(NewRandomString()).WithProviderSubType(NewRandomString()).WithProviderId(_pageThree.Skip(1).First())))
                .WithIsIndicative(false)
                .WithMajorVersion(majorVersionThree)
                .WithMinorVersion(majorVersionThree)
                .WithTotalFunding(totalFunding3));

            GivenThePublishedProvidersFundingSummary(_pageOne, new[] { fundingOne });
            AndThePublishedProviderFundingSummary(_pageTwo, new[] { fundingTwo });
            AndThePublishedProviderFundingSummary(_pageThree, new[] { fundingThree });

            return AsArray(fundingOne, fundingTwo, fundingThree);
        }

        private async Task<IEnumerable<PublishedProviderFundingSummary>> WhenTheFundingSummaryAreRequested()
            => await _publishedProviderLookupService.GetPublishedProviderFundingSummaries(
                 _specificationSummary,
                 new[] { PublishedProviderStatus.Approved, PublishedProviderStatus.Released }, 
                 _publishedProviderIds);

        private void AndThePublishedProviderFundingSummary(IEnumerable<string> publishedProviderIds,
            IEnumerable<PublishedProviderFundingSummary> fundings)
        {
            GivenThePublishedProvidersFundingSummary(publishedProviderIds, fundings);
        }

        private void GivenThePublishedProvidersFundingSummary(IEnumerable<string> publishedProviderIds,
            IEnumerable<PublishedProviderFundingSummary> fundings)
        {
            _publishedFunding.Setup(_ => _.GetReleaseFundingPublishedProviders(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(publishedProviderIds)),
                    It.Is<string>(spec => spec == _specificationId),
                    It.Is<PublishedProviderStatus[]>(sts => sts.SequenceEqual(new List<PublishedProviderStatus> { PublishedProviderStatus.Approved, PublishedProviderStatus.Released }))))
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

        private PublishedProviderFundingSummary NewPublishedProviderFunding(Action<PublishedProviderFundingSummaryBuilder> setUp = null)
        {
            PublishedProviderFundingSummaryBuilder builder = new PublishedProviderFundingSummaryBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private Provider NewProvider(Action<ProviderBuilder> setUp = null)
        {
            ProviderBuilder builder = new ProviderBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private SpecificationSummary NewSpecificationSummary(Action<SpecificationSummaryBuilder> setUp = null)
        {
            SpecificationSummaryBuilder builder = new SpecificationSummaryBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }
    }
}