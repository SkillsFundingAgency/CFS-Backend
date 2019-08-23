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
        private ICosmosRepository _cosmosRepository;

        private PublishedFundingRepository _repository;

        [TestInitialize]
        public void SetUp()
        {
            _providers = new List<PublishedProvider>();
            _cosmosRepository = Substitute.For<ICosmosRepository>();

            _cosmosRepository.Query<PublishedProvider>(true)
                .Returns(_providers.AsQueryable());

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
                    .WithPublishedProviderStatus(PublishedProviderStatus.Held))));
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

        private void GivenThePublishedProviders(params PublishedProvider[] publishedProviders)
        {
            _providers.AddRange(publishedProviders);
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