using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Undo;
using CalculateFunding.Services.Publishing.Undo.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;
using ModelGroupingReason = CalculateFunding.Models.Publishing.GroupingReason;

namespace CalculateFunding.Services.Publishing.UnitTests.Undo.Repositories
{
#if !RUN_LOCAL_DEV_TESTS
    [Ignore("For local dev work only. DO NOT include in CI")]
#endif
    [TestClass]
    public class PublishedFundingUndoRepositoryEndToEndTests
    {
        //NB the document counts are only 1 while there's a single MANUAL_CORRELATION_ID per document type
        //as soon as you run more jobs against dev you get younger docs for that stream/period etc.
        
        private IConfiguration _configuration;
        private PublishedFundingUndoCosmosRepository _repository;
        private const string CorrelationId = "MANUAL_CORRELATION_ID";

        [TestInitialize]
        public void SetUp()
        {
            _configuration = new ConfigurationBuilder()
                .AddUserSecrets("df0d69d5-a6db-4598-909f-262fc39cb8c8")
                .Build();

            CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

            _configuration.Bind("CosmosDbSettings", cosmosDbSettings);

            cosmosDbSettings.ContainerName = "publishedfunding-copy";

            _repository = new PublishedFundingUndoCosmosRepository(new ResiliencePolicies
            {
                PublishedFundingRepository = Policy.NoOpAsync()
            }, new CosmosRepository(cosmosDbSettings));
        }

        [TestMethod]
        public async Task GetCorrelationDetailsForPublishedProviders()
        {
            UndoTaskDetails undoTaskDetails = await _repository
                .GetCorrelationDetailsForPublishedProviders(CorrelationId);

            undoTaskDetails
                .Should()
                .BeEquivalentTo(new UndoTaskDetails
                {
                    FundingStreamId = "DSG",
                    FundingPeriodId = "FY-2021-7db621f6-ff28-4910-a3b2-5440c2cd80b0",
                    TimeStamp = 1588682808
                });
        }
        
        [TestMethod]
        public async Task GetPublishedProviders()
        {
            ICosmosDbFeedIterator feed = _repository.GetPublishedProviders("DSG",
                "FY-2021-7db621f6-ff28-4910-a3b2-5440c2cd80b0",
                1588682808);

            feed.HasMoreResults
                .Should()
                .BeTrue();

            IEnumerable<PublishedProvider> documents = await feed.ReadNext<PublishedProvider>();

            documents
                .Should()
                .NotBeEmpty();
        }
        
        [TestMethod]
        public async Task GetPublishedProvidersFromVersion()
        {
            ICosmosDbFeedIterator feed = _repository.GetPublishedProvidersFromVersion("DSG",
                "FY-2021",
                2M);

            feed.HasMoreResults
                .Should()
                .BeTrue();

            IEnumerable<PublishedProvider> documents = await feed.ReadNext<PublishedProvider>();

            documents
                .Should()
                .NotBeEmpty();
        }

        [TestMethod]
        public async Task GetCorrelationIdDetailsForPublishedProviderVersions()
        {
            UndoTaskDetails undoTaskDetails = await _repository
                .GetCorrelationIdDetailsForPublishedProviderVersions(CorrelationId);

            undoTaskDetails
                .Should()
                .BeEquivalentTo(new UndoTaskDetails
                {
                    FundingStreamId = "DSG",
                    FundingPeriodId = "FY-2021",
                    TimeStamp = 1588684299
                });
        }
        
        [TestMethod]
        public async Task GetPublishedProviderVersions()
        {
            ICosmosDbFeedIterator feed = _repository.GetPublishedProviderVersions("DSG",
                "FY-2021",
                1588684299);

            feed.HasMoreResults
                .Should()
                .BeTrue();

            IEnumerable<PublishedProviderVersion> documents = await feed.ReadNext<PublishedProviderVersion>();

            documents
                .Should()
                .NotBeEmpty();
        }
        
        [TestMethod]
        public async Task GetPublishedProviderVersionsFromVersion()
        {
            ICosmosDbFeedIterator feed = _repository.GetPublishedProviderVersionsFromVersion("DSG",
                "FY-2021",
                2M);

            feed.HasMoreResults
                .Should()
                .BeTrue();

            IEnumerable<PublishedProviderVersion> documents = await feed.ReadNext<PublishedProviderVersion>();

            documents
                .Should()
                .NotBeEmpty();
        }
        
        [TestMethod]
        public async Task GetCorrelationIdDetailsForPublishedFundingVersions()
        {
            UndoTaskDetails undoTaskDetails = await _repository
                .GetCorrelationIdDetailsForPublishedFundingVersions(CorrelationId);

            undoTaskDetails
                .Should()
                .BeEquivalentTo(new UndoTaskDetails
                {
                    FundingStreamId = "DSG",
                    FundingPeriodId = "FY-2021",
                    TimeStamp = 1588684935
                });
        }
        
        [TestMethod]
        public async Task GetPublishedFundingVersions()
        {
            ICosmosDbFeedIterator feed = _repository.GetPublishedFundingVersions("DSG",
                "FY-2021",
                1588684299);

            feed.HasMoreResults
                .Should()
                .BeTrue();

            IEnumerable<PublishedFundingVersion> documents = await feed.ReadNext<PublishedFundingVersion>();

            documents
                .Should()
                .NotBeEmpty();
        }
        
        [TestMethod]
        public async Task GetPublishedFundingVersionsFromVersion()
        {
            ICosmosDbFeedIterator feed = _repository.GetPublishedFundingVersionsFromVersion("DSG",
                "FY-2021",
                2M);

            feed.HasMoreResults
                .Should()
                .BeTrue();

            IEnumerable<PublishedFundingVersion> documents = await feed.ReadNext<PublishedFundingVersion>();

            documents
                .Should()
                .NotBeEmpty();
        }
        
        [TestMethod]
        public async Task GetCorrelationIdDetailsForPublishedFunding()
        {
            UndoTaskDetails undoTaskDetails = await _repository
                .GetCorrelationIdDetailsForPublishedFunding(CorrelationId);

            undoTaskDetails
                .Should()
                .BeEquivalentTo(new UndoTaskDetails
                {
                    FundingStreamId = "DSG",
                    FundingPeriodId = "FY-2021",
                    TimeStamp = 1588685609
                });
        }
        
        [TestMethod]
        public async Task GetPublishedFunding()
        {
            ICosmosDbFeedIterator feed = _repository.GetPublishedFunding("DSG",
                "FY-2021",
                1588685609);

            while (feed.HasMoreResults)
            {
                IEnumerable<PublishedFunding> documents = await feed.ReadNext<PublishedFunding>();    
                
                documents
                    .Should()
                    .NotBeNull()
                    .And
                    .NotBeEmpty();
            }
        }
        
        [TestMethod]
        public async Task GetPublishedFundingFromVersion()
        {
            ICosmosDbFeedIterator feed = _repository.GetPublishedFundingFromVersion("DSG",
                "FY-2021",
                4M);

            feed.HasMoreResults
                .Should()
                .BeTrue();
            
            IEnumerable<PublishedFunding> documents = await feed.ReadNext<PublishedFunding>();

            documents
                .Should()
                .NotBeEmpty();
        }
        
        [TestMethod]
        public async Task GetLatestEarlierPublishedFundingVersion()
        {
            PublishedFundingVersion document = await _repository.GetLatestEarlierPublishedFundingVersion("DSG",
                "FY-2021",
                1584980585, //timestamp is for version 3 so test should find me version 2
                "LACode",
                "213",
                ModelGroupingReason.Information);

            document?
                .MajorVersion
                .Should()
                .Be(2);
        }
        
        [TestMethod]
        public async Task GetLatestEarlierPublishedFundingVersionFromVersion()
        {
            PublishedFundingVersion document = await _repository.GetLatestEarlierPublishedFundingVersionFromVersion("DSG",
                "FY-2021",
                4M, 
                "LACode",
                "213",
                ModelGroupingReason.Information);

            document?
                .MajorVersion
                .Should()
                .Be(3);
        }
        
        [TestMethod]
        public async Task GetLatestEarlierPublishedProviderVersion()
        {
            PublishedProviderVersion document = await _repository.GetLatestEarlierPublishedProviderVersion("DSG",
                "FY-2021",
                1584355885, //timestamp is v3 so should yield v2
                "10007322");

            document?
                .Version
                .Should()
                .Be(2);
        }
        
        [TestMethod]
        public async Task GetLatestEarlierPublishedProviderVersionFromVersion()
        {
            PublishedProviderVersion document = await _repository.GetLatestEarlierPublishedProviderVersionFromVersion("DSG",
                "FY-2021",
                2, //timestamp is v3 so should yield v2
                "10007322");

            document?
                .MajorVersion
                .Should()
                .Be(1);
        }
    }
}