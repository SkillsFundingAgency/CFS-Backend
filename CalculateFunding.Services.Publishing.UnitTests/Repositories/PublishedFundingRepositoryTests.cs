using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Repositories;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Publishing.UnitTests.Repositories
{
    [TestClass]
    public class PublishedFundingRepositoryTests
    {
        private List<PublishedProvider> _providers;
        private List<PublishedFunding> _funding;
        private ICosmosRepository _cosmosRepository;

        private PublishedFundingRepository _repository;

        [TestInitialize]
        public void SetUp()
        {
            _providers = new List<PublishedProvider>();
            _funding = new List<PublishedFunding>();
            _cosmosRepository = Substitute.For<ICosmosRepository>();

            _cosmosRepository.Query<PublishedProvider>(true)
                .Returns(_providers.AsQueryable());

            _cosmosRepository.Query<PublishedFunding>(true)
                .Returns(_funding.AsQueryable());

            _repository = new PublishedFundingRepository(_cosmosRepository);
        }

        [TestMethod]
        public async Task IsHealthOkChecksCosmosRepository()
        {
            bool expectedIsOkFlag = new RandomBoolean();
            string expectedMessage = new RandomString();

            GivenTheRepositoryServiceHealth(expectedIsOkFlag, expectedMessage);

            ServiceHealth isHealthOk = await _repository.IsHealthOk();

            isHealthOk
                .Should()
                .NotBeNull();

            isHealthOk
                .Name
                .Should()
                .Be(nameof(PublishedFundingRepository));

            DependencyHealth dependencyHealth = isHealthOk
                .Dependencies
                .FirstOrDefault();

            dependencyHealth
                .Should()
                .Match<DependencyHealth>(_ => _.HealthOk == expectedIsOkFlag &&
                                              _.Message == expectedMessage);
        }

        [TestMethod]
        [Ignore("This method isn't being used since the optimisation")]
        public async Task FetchesPublishedProvidersWithTheSuppliedSpecificationIdAndStatusOfUpdatedOrHeld()
        {
            string specificationId = new RandomString();
            string fundingStreamId = new RandomString();
            string fundingPeriodId = new RandomString();

            PublishedProvider firstExpectedPublishedProvider = NewPublishedProvider(_ =>
                _.WithCurrent(NewPublishedProviderVersion(ppv => ppv.WithSpecificationId(specificationId)
                    .WithPublishedProviderStatus(PublishedProviderStatus.Draft)
                    .WithFundingStreamId(fundingStreamId)
                    .WithFundingPeriodId(fundingPeriodId))));
            PublishedProvider secondExpectedPublishedProvider = NewPublishedProvider(_ =>
                _.WithCurrent(NewPublishedProviderVersion(ppv => ppv.WithSpecificationId(specificationId)
                    .WithPublishedProviderStatus(PublishedProviderStatus.Updated)
                    .WithFundingStreamId(fundingStreamId)
                    .WithFundingPeriodId(fundingPeriodId))));

            GivenThePublishedProviders(NewPublishedProvider(),
                firstExpectedPublishedProvider,
                NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(ppv =>
                    ppv.WithSpecificationId(specificationId)
                        .WithPublishedProviderStatus(PublishedProviderStatus.Approved)))),
                NewPublishedProvider(),
                secondExpectedPublishedProvider,
                NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(ppv =>
                    ppv.WithSpecificationId(specificationId)
                        .WithPublishedProviderStatus(PublishedProviderStatus.Released)))));

            var matches = await _repository.GetPublishedProviderIdsForApproval(fundingStreamId, fundingPeriodId);

            matches
                .Should()
                .BeEquivalentTo(firstExpectedPublishedProvider, secondExpectedPublishedProvider);
        }

        private void GivenThePublishedFunding(params PublishedFunding[] publishedFinding)
        {
            _funding.AddRange(publishedFinding);
        }

        private void GivenThePublishedProviders(params PublishedProvider[] publishedProviders)
        {
            _providers.AddRange(publishedProviders);
        }

        private PublishedFunding NewPublishedFunding(Action<PublishedFundingBuilder> setUp = null)
        {
            PublishedFundingBuilder builder = new PublishedFundingBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private PublishedFundingVersion NewPublishedFundingVersion(
            Action<PublishedFundingVersionBuilder> setUp = null)
        {
            PublishedFundingVersionBuilder builder = new PublishedFundingVersionBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        {
            PublishedProviderBuilder builder = new PublishedProviderBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private PublishedProviderVersion NewPublishedProviderVersion(
            Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder builder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private void GivenTheRepositoryServiceHealth(bool expectedIsOkFlag, string expectedMessage)
        {
            _cosmosRepository.IsHealthOk().Returns((expectedIsOkFlag, expectedMessage));
        }
    }
}