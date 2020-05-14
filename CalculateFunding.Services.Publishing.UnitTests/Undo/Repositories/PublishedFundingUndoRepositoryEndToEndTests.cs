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

            cosmosDbSettings.ContainerName = "publishedfunding";

            _repository = new PublishedFundingUndoCosmosRepository(new ResiliencePolicies
            {
                PublishedFundingRepository = Policy.NoOpAsync()
            }, new CosmosRepository(cosmosDbSettings));
        }

        [TestMethod]
        public async Task GetCorrelationDetailsForPublishedProviders()
        {
            CorrelationIdDetails correlationIdDetails = await _repository
                .GetCorrelationDetailsForPublishedProviders(CorrelationId);

            correlationIdDetails
                .Should()
                .BeEquivalentTo(new CorrelationIdDetails
                {
                    FundingStreamId = "DSG",
                    FundingPeriodId = "FY-2021-7db621f6-ff28-4910-a3b2-5440c2cd80b0",
                    TimeStamp = 1588682808
                });
        }
        
        [TestMethod]
        public async Task GetPublishedProviders()
        {
            ICosmosDbFeedIterator<PublishedProvider> feed = _repository.GetPublishedProviders("DSG",
                "FY-2021-7db621f6-ff28-4910-a3b2-5440c2cd80b0",
                1588682808);

            feed.HasMoreResults
                .Should()
                .BeTrue();

            IEnumerable<PublishedProvider> documents = await feed.ReadNext();

            documents
                .Should()
                .NotBeEmpty();
        }

        [TestMethod]
        public async Task GetCorrelationIdDetailsForPublishedProviderVersions()
        {
            CorrelationIdDetails correlationIdDetails = await _repository
                .GetCorrelationIdDetailsForPublishedProviderVersions(CorrelationId);

            correlationIdDetails
                .Should()
                .BeEquivalentTo(new CorrelationIdDetails
                {
                    FundingStreamId = "DSG",
                    FundingPeriodId = "FY-2021",
                    TimeStamp = 1588684299
                });
        }
        
        [TestMethod]
        public async Task GetPublishedProviderVersions()
        {
            ICosmosDbFeedIterator<PublishedProviderVersion> feed = _repository.GetPublishedProviderVersions("DSG",
                "FY-2021",
                1588684299);

            feed.HasMoreResults
                .Should()
                .BeTrue();

            IEnumerable<PublishedProviderVersion> documents = await feed.ReadNext();

            documents
                .Should()
                .NotBeEmpty();
        }
        
        [TestMethod]
        public async Task GetCorrelationIdDetailsForPublishedFundingVersions()
        {
            CorrelationIdDetails correlationIdDetails = await _repository
                .GetCorrelationIdDetailsForPublishedFundingVersions(CorrelationId);

            correlationIdDetails
                .Should()
                .BeEquivalentTo(new CorrelationIdDetails
                {
                    FundingStreamId = "DSG",
                    FundingPeriodId = "FY-2021",
                    TimeStamp = 1588684935
                });
        }
        
        [TestMethod]
        public async Task GetPublishedFundingVersions()
        {
            ICosmosDbFeedIterator<PublishedFundingVersion> feed = _repository.GetPublishedFundingVersions("DSG",
                "FY-2021",
                1588684299);

            feed.HasMoreResults
                .Should()
                .BeTrue();

            IEnumerable<PublishedFundingVersion> documents = await feed.ReadNext();

            documents
                .Should()
                .NotBeEmpty();
        }
        
        [TestMethod]
        public async Task GetCorrelationIdDetailsForPublishedFunding()
        {
            CorrelationIdDetails correlationIdDetails = await _repository
                .GetCorrelationIdDetailsForPublishedFunding(CorrelationId);

            correlationIdDetails
                .Should()
                .BeEquivalentTo(new CorrelationIdDetails
                {
                    FundingStreamId = "DSG",
                    FundingPeriodId = "FY-2021",
                    TimeStamp = 1588685609
                });
        }
        
        [TestMethod]
        public async Task GetPublishedFunding()
        {
            ICosmosDbFeedIterator<PublishedFunding> feed = _repository.GetPublishedFunding("DSG",
                "FY-2021",
                1588685609);

            while (feed.HasMoreResults)
            {
                IEnumerable<PublishedFunding> documents = await feed.ReadNext();    
                
                documents
                    .Should()
                    .NotBeNull()
                    .And
                    .NotBeEmpty();
            }
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
    }
}