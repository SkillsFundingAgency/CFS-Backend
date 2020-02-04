using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class PublishedProviderReIndexerServiceTests
    {
        private PublishedProviderReIndexerService _service;
        private ISearchRepository<PublishedProviderIndex> _searchRepository;
        private IPublishedFundingRepository _publishedFundingRepository;

        [TestInitialize]
        public void SetUp()
        {
            _searchRepository = Substitute.For<ISearchRepository<PublishedProviderIndex>>();
            _publishedFundingRepository = Substitute.For<IPublishedFundingRepository>();

            _service = new PublishedProviderReIndexerService(_searchRepository,
                new ResiliencePolicies
                {
                    PublishedProviderSearchRepository = Policy.NoOpAsync(),
                    PublishedFundingRepository = Policy.NoOpAsync()
                },
                _publishedFundingRepository,
                Substitute.For<ILogger>());
        }

        [TestMethod]
        public async Task DeletesThenReIndexesAllPublishedProviders()
        {
            await _service.Run(new Message());

            await _searchRepository
                .Received(1)
                .DeleteIndex();

            await _publishedFundingRepository
                .Received(1)
                .AllPublishedProviderBatchProcessing(Arg.Any<Func<List<PublishedProvider>, Task>>(), Arg.Is(1000));
        }
    }
}