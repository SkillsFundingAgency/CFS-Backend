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
        public async Task FetchesPublishedProvidersWithTheSuppliedSpecificationIdAndStatusOfUpdatedOrHeld()
        {
            string specificationId = new RandomString();

            PublishedProvider firstExpectedPublishedProvider = NewPublishedProvider(_ =>
                _.WithCurrent(NewPublishedProviderVersion(ppv => ppv.WithSpecificationId(specificationId)
                    .WithPublishedProviderStatus(PublishedProviderStatus.Draft))));
            PublishedProvider secondExpectedPublishedProvider = NewPublishedProvider(_ =>
                _.WithCurrent(NewPublishedProviderVersion(ppv => ppv.WithSpecificationId(specificationId)
                    .WithPublishedProviderStatus(PublishedProviderStatus.Updated))));

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

            IEnumerable<PublishedProvider> matches = await _repository.GetPublishedProvidersForApproval(specificationId);

            matches
                .Should()
                .BeEquivalentTo(firstExpectedPublishedProvider, secondExpectedPublishedProvider);
        }

        [TestMethod]
        public async Task FetchesPublishedProvidersWithTheSuppliedSpecificationId()
        {
            string specificationId = new RandomString();

            PublishedProvider firstExpectedPublishedProvider = NewPublishedProvider(_ =>
                _.WithCurrent(NewPublishedProviderVersion(ppv => ppv.WithSpecificationId(specificationId))));
            PublishedProvider secondExpectedPublishedProvider = NewPublishedProvider(_ =>
                _.WithCurrent(NewPublishedProviderVersion(ppv => ppv.WithSpecificationId(specificationId))));

            GivenThePublishedProviders(NewPublishedProvider(),
                firstExpectedPublishedProvider,
                NewPublishedProvider(),
                NewPublishedProvider(),
                secondExpectedPublishedProvider,
                NewPublishedProvider());

            IEnumerable<PublishedProvider> matches = await _repository.GetLatestPublishedProvidersBySpecification(specificationId);

            matches
                .Should()
                .BeEquivalentTo(firstExpectedPublishedProvider, secondExpectedPublishedProvider);
        }

        [TestMethod]
        public async Task FetchesPublishedFundingWithTheSuppliedSpecificationId()
        {
            string specificationId = new RandomString();

            PublishedFunding firstExpectedPublishedFunding = NewPublishedFunding(_ =>
                _.WithCurrent(NewPublishedFundingVersion(ppv => ppv.WithSpecificationId(specificationId))));
            PublishedFunding secondExpectedPublishedFunding = NewPublishedFunding(_ =>
                _.WithCurrent(NewPublishedFundingVersion(ppv => ppv.WithSpecificationId(specificationId))));

            GivenThePublishedFunding(NewPublishedFunding(),
                firstExpectedPublishedFunding,
                NewPublishedFunding(),
                NewPublishedFunding(),
                secondExpectedPublishedFunding,
                NewPublishedFunding());

            IEnumerable<PublishedFunding> matches = await _repository.GetLatestPublishedFundingBySpecification(specificationId);

            matches
                .Should()
                .BeEquivalentTo(firstExpectedPublishedFunding, secondExpectedPublishedFunding);
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