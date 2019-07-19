using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Repositories;
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

            var matches = await _repository.GetLatestPublishedProvidersBySpecification(specificationId);

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
    }
}