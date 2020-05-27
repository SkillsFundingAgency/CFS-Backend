using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Undo;
using CalculateFunding.Services.Publishing.Undo.Repositories;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using ModelsGroupingReason = CalculateFunding.Models.Publishing.GroupingReason;

namespace CalculateFunding.Services.Publishing.UnitTests.Undo.Repositories
{
    [TestClass]
    public class PublishedFundingUndoCosmosRepositoryTests : PublishedFundingUndoTestBase
    {
        private PublishedFundingUndoCosmosRepository _repository;
        private Mock<ICosmosRepository> _cosmosRepository;

        [TestInitialize]
        public void SetUp()
        {
            _cosmosRepository = new Mock<ICosmosRepository>();

            _repository = new PublishedFundingUndoCosmosRepository(new ResiliencePolicies
                {
                    PublishedFundingRepository = Policy.NoOpAsync()
                },
                _cosmosRepository.Object);
        }

        [TestMethod]
        public async Task GetCorrelationDetailsForPublishedProviders()
        {
            CorrelationIdDetails expectedDetails = NewCorrelationIdDetails();
            string correlationId = NewRandomString();
            
            GivenTheCorrelationIdDetailsForCosmosQuery(@"SELECT
                              MIN(p._ts) AS timeStamp,
                              p.content.current.fundingStreamId,
                              p.content.current.fundingPeriodId
                        FROM publishedProvider p
                        WHERE p.documentType = 'PublishedProvider'
                        AND p.content.current.correlationId = @correlationId
                        AND p.deleted = false
                        GROUP BY p.content.current.fundingStreamId,
                        p.content.current.fundingPeriodId",
                correlationId,
                expectedDetails);

            CorrelationIdDetails actualDetails = await _repository.GetCorrelationDetailsForPublishedProviders(correlationId);

            actualDetails
                .Should()
                .BeSameAs(expectedDetails);
        }
        
        [TestMethod]
        public async Task GetCorrelationIdDetailsForPublishedProviderVersions()
        {
            CorrelationIdDetails expectedDetails = NewCorrelationIdDetails();
            string correlationId = NewRandomString();
            
            GivenTheCorrelationIdDetailsForCosmosQuery(@"SELECT
                              MIN(p._ts) AS timeStamp,
                              p.content.fundingStreamId,
                              p.content.fundingPeriodId
                        FROM publishedProviderVersion p
                        WHERE p.documentType = 'PublishedProviderVersion'
                        AND p.content.correlationId = @correlationId
                        AND p.deleted = false
                        GROUP BY p.content.fundingStreamId,
                        p.content.fundingPeriodId",
                correlationId,
                expectedDetails);

            CorrelationIdDetails actualDetails = await _repository.GetCorrelationIdDetailsForPublishedProviderVersions(correlationId);

            actualDetails
                .Should()
                .BeSameAs(expectedDetails);
        }
        
        [TestMethod]
        public async Task GetCorrelationIdDetailsForPublishedFundingVersions()
        {
            CorrelationIdDetails expectedDetails = NewCorrelationIdDetails();
            string correlationId = NewRandomString();
            
            GivenTheCorrelationIdDetailsForCosmosQuery(@"SELECT
                              MIN(p._ts) AS timeStamp,
                              p.content.fundingStreamId,
                              p.content.fundingPeriod.id AS fundingPeriodId
                        FROM publishedFundingVersion p
                        WHERE p.documentType = 'PublishedFundingVersion'
                        AND p.content.correlationId = @correlationId
                        AND p.deleted = false
                        GROUP BY p.content.fundingStreamId,
                        p.content.fundingPeriod.id",
                correlationId,
                expectedDetails);

            CorrelationIdDetails actualDetails = await _repository.GetCorrelationIdDetailsForPublishedFundingVersions(correlationId);

            actualDetails
                .Should()
                .BeSameAs(expectedDetails);
        }
        
        [TestMethod]
        public async Task GetCorrelationIdDetailsForPublishedFunding()
        {
            CorrelationIdDetails expectedDetails = NewCorrelationIdDetails();
            string correlationId = NewRandomString();
            
            GivenTheCorrelationIdDetailsForCosmosQuery(@"SELECT
                              MIN(p._ts) AS timeStamp,
                              p.content.current.fundingStreamId,
                              p.content.current.fundingPeriod.id AS fundingPeriodId
                        FROM publishedFunding p
                        WHERE p.documentType = 'PublishedFunding'
                        AND p.content.current.correlationId = @correlationId
                        AND p.deleted = false
                        GROUP BY p.content.current.fundingStreamId,
                        p.content.current.fundingPeriod.id",
                correlationId,
                expectedDetails);

            CorrelationIdDetails actualDetails = await _repository.GetCorrelationIdDetailsForPublishedFunding(correlationId);

            actualDetails
                .Should()
                .BeSameAs(expectedDetails);
        }

        [TestMethod]
        public void GetPublishedProviderVersions()
        {
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            long timeStamp = NewRandomTimeStamp();

            ICosmosDbFeedIterator<PublishedProviderVersion> expectedFeed = NewFeedIterator<PublishedProviderVersion>();
            
            GivenTheFeedIterator(@"SELECT
                              *
                        FROM publishedProviderVersion p
                        WHERE p.documentType = 'PublishedProviderVersion'
                        AND p._ts >= @sinceTimeStamp
                        AND p.content.fundingStreamId = @fundingStreamId
                        AND p.content.fundingPeriodId = @fundingPeriodId
                        AND p.deleted = false",
                expectedFeed,
                ("@fundingPeriodId", fundingPeriodId),
                ("@fundingStreamId", fundingStreamId),
                ("@sinceTimeStamp", timeStamp));

            ICosmosDbFeedIterator<PublishedProviderVersion> actualFeedIterator = _repository.GetPublishedProviderVersions(fundingStreamId,
                fundingPeriodId,
                timeStamp);

            actualFeedIterator
                .Should()
                .BeSameAs(expectedFeed);
        }
        
        [TestMethod]
        public void GetPublishedProviders()
        {
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            long timeStamp = NewRandomTimeStamp();

            ICosmosDbFeedIterator<PublishedProvider> expectedFeed = NewFeedIterator<PublishedProvider>();
            
            GivenTheFeedIterator(@"SELECT
                              *
                        FROM publishedProvider p
                        WHERE p.documentType = 'PublishedProvider'
                        AND p._ts >= @sinceTimeStamp
                        AND p.content.current.fundingStreamId = @fundingStreamId
                        AND p.content.current.fundingPeriodId = @fundingPeriodId
                        AND p.deleted = false",
                expectedFeed,
                ("@fundingPeriodId", fundingPeriodId),
                ("@fundingStreamId", fundingStreamId),
                ("@sinceTimeStamp", timeStamp));

            ICosmosDbFeedIterator<PublishedProvider> actualFeedIterator = _repository.GetPublishedProviders(fundingStreamId,
                fundingPeriodId,
                timeStamp);

            actualFeedIterator
                .Should()
                .BeSameAs(expectedFeed);
        }
        
        [TestMethod]
        public void GetPublishedFunding()
        {
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            long timeStamp = NewRandomTimeStamp();

            ICosmosDbFeedIterator<PublishedFunding> expectedFeed = NewFeedIterator<PublishedFunding>();
            
            GivenTheFeedIterator(@"SELECT
                              *
                        FROM publishedFunding p
                        WHERE p.documentType = 'PublishedFunding'
                        AND p._ts >= @sinceTimeStamp
                        AND p.content.current.fundingStreamId = @fundingStreamId
                        AND p.content.current.fundingPeriod.id = @fundingPeriodId
                        AND p.deleted = false",
                expectedFeed,
                ("@fundingPeriodId", fundingPeriodId),
                ("@fundingStreamId", fundingStreamId),
                ("@sinceTimeStamp", timeStamp));

            ICosmosDbFeedIterator<PublishedFunding> actualFeedIterator = _repository.GetPublishedFunding(fundingStreamId,
                fundingPeriodId,
                timeStamp);

            actualFeedIterator
                .Should()
                .BeSameAs(expectedFeed);
        }
        
        [TestMethod]
        public void GetPublishedFundingVersions()
        {
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            long timeStamp = NewRandomTimeStamp();

            ICosmosDbFeedIterator<PublishedFundingVersion> expectedFeed = NewFeedIterator<PublishedFundingVersion>();
            
            GivenTheFeedIterator(@"SELECT
                              *
                        FROM publishedFundingVersion p
                        WHERE p.documentType = 'PublishedFundingVersion'
                        AND p._ts >= @sinceTimeStamp
                        AND p.content.fundingStreamId = @fundingStreamId
                        AND p.content.fundingPeriod.id = @fundingPeriodId
                        AND p.deleted = false",
                expectedFeed,
                ("@fundingPeriodId", fundingPeriodId),
                ("@fundingStreamId", fundingStreamId),
                ("@sinceTimeStamp", timeStamp));

            ICosmosDbFeedIterator<PublishedFundingVersion> actualFeedIterator = _repository.GetPublishedFundingVersions(fundingStreamId,
                fundingPeriodId,
                timeStamp);

            actualFeedIterator
                .Should()
                .BeSameAs(expectedFeed);
        }

        [TestMethod]
        public async Task GetLatestEarlierPublishedFundingVersion()
        {
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            long timeStamp = NewRandomTimeStamp();
            string groupTypeIdentifier = NewRandomString();
            string groupTypeIdentifierValue = NewRandomString();
            ModelsGroupingReason groupingReason = NewRandomGroupingReason();

            PublishedFundingVersion expectedLatestEarlierDocument = NewPublishedFundingVersion();
            
            GivenTheLatestEarlierDocument(@"SELECT
                              TOP 1 *
                        FROM publishedFundingVersion p
                        WHERE p.documentType = 'PublishedFundingVersion'
                        AND p._ts < @sinceTimeStamp                        
                        AND p.content.fundingStreamId = @fundingStreamId
                        AND p.content.fundingPeriod.id = @fundingPeriodId
                        AND p.content.organisationGroupTypeIdentifier = @groupTypeIdentifier
                        AND p.content.organisationGroupIdentifierValue = @groupTypeIdentifierValue
                        AND p.content.groupingReason = @groupingReason
                        AND p.deleted = false
                        ORDER BY p._ts DESC",
                expectedLatestEarlierDocument,
                ("@fundingPeriodId", fundingPeriodId),
                ("@fundingStreamId", fundingStreamId),
                ("@sinceTimeStamp", timeStamp),
                ("@groupTypeIdentifier", groupTypeIdentifier),
                ("@groupTypeIdentifierValue", groupTypeIdentifierValue),
                ("@groupingReason", groupingReason.ToString()));

            PublishedFundingVersion actualLatestEarlierDocument = await _repository.GetLatestEarlierPublishedFundingVersion(fundingStreamId,
                fundingPeriodId,
                timeStamp,
                groupTypeIdentifier,
                groupTypeIdentifierValue,
                groupingReason);

            actualLatestEarlierDocument
                .Should()
                .BeSameAs(expectedLatestEarlierDocument);
        }
        
        [TestMethod]
        public async Task GetLatestEarlierPublishedProviderVersionByStatus()
        {
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            long timeStamp = NewRandomTimeStamp();
            string providerId = NewRandomString();
            PublishedProviderStatus status = new RandomEnum<PublishedProviderStatus>();

            PublishedProviderVersion expectedLatestEarlierDocument = NewPublishedProviderVersion();
            
            GivenTheLatestEarlierDocument(@$"SELECT
                              TOP 1 *
                        FROM publishedProviderVersion p
                        WHERE p.documentType = 'PublishedProviderVersion'
                        AND p._ts < @sinceTimeStamp
                        AND p.content.fundingStreamId = @fundingStreamId
                        AND p.content.fundingPeriodId = @fundingPeriodId
                        AND p.content.providerId = @providerId
                        AND p.content.status = @status
                        AND p.deleted = false
                        ORDER BY p._ts DESC",
                expectedLatestEarlierDocument,
                ("@fundingPeriodId", fundingPeriodId),
                ("@fundingStreamId", fundingStreamId),
                ("@sinceTimeStamp", timeStamp),
                ("@providerId", providerId),
                ("@status", status));

            PublishedProviderVersion actualLatestEarlierDocument = await _repository.GetLatestEarlierPublishedProviderVersion(fundingStreamId,
                fundingPeriodId,
                timeStamp,
                providerId,
                status);

            actualLatestEarlierDocument
                .Should()
                .BeSameAs(expectedLatestEarlierDocument);
        }
        
        [TestMethod]
        public async Task GetLatestEarlierPublishedProviderVersion()
        {
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            long timeStamp = NewRandomTimeStamp();
            string providerId = NewRandomString();

            PublishedProviderVersion expectedLatestEarlierDocument = NewPublishedProviderVersion();
            
            GivenTheLatestEarlierDocument(@$"SELECT
                              TOP 1 *
                        FROM publishedProviderVersion p
                        WHERE p.documentType = 'PublishedProviderVersion'
                        AND p._ts < @sinceTimeStamp
                        AND p.content.fundingStreamId = @fundingStreamId
                        AND p.content.fundingPeriodId = @fundingPeriodId
                        AND p.content.providerId = @providerId
                        {string.Empty}
                        AND p.deleted = false
                        ORDER BY p._ts DESC",
                expectedLatestEarlierDocument,
                ("@fundingPeriodId", fundingPeriodId),
                ("@fundingStreamId", fundingStreamId),
                ("@sinceTimeStamp", timeStamp),
                ("@providerId", providerId));

            PublishedProviderVersion actualLatestEarlierDocument = await _repository.GetLatestEarlierPublishedProviderVersion(fundingStreamId,
                fundingPeriodId,
                timeStamp,
                providerId);

            actualLatestEarlierDocument
                .Should()
                .BeSameAs(expectedLatestEarlierDocument);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task BulkDeletePublishedFundingDocuments(bool hardDelete)
        {
            IEnumerable<PublishedProviderVersion> documentsToDelete = Page(NewPublishedProviderVersion(),
                NewPublishedProviderVersion(),
                NewPublishedProviderVersion());

            await _repository.BulkDeletePublishedFundingDocuments(documentsToDelete, _ => _.PartitionKey, hardDelete);
            
            _cosmosRepository.Verify(_ => _.BulkDeleteAsync(
                It.Is<IEnumerable<KeyValuePair<string, PublishedProviderVersion>>>(docs
            => docs.SequenceEqual(documentsToDelete.ToDictionary(doc => doc.PartitionKey))), 
                5, 
                hardDelete),
                Times.Once);
        }
        
        [TestMethod]
        public async Task BulkUpdatePublishedFundingDocuments()
        {
            IEnumerable<PublishedProviderVersion> documentsToUpdate = Page(NewPublishedProviderVersion(),
                NewPublishedProviderVersion(),
                NewPublishedProviderVersion());

            await _repository.BulkUpdatePublishedFundingDocuments(documentsToUpdate, _ => _.PartitionKey);
            
            _cosmosRepository.Verify(_ => _.BulkUpsertAsync(
                    It.Is<IEnumerable<KeyValuePair<string, PublishedProviderVersion>>>(docs
                        => docs.SequenceEqual(documentsToUpdate.ToDictionary(doc => doc.PartitionKey))), 
                    5,
                    true,
                    false),
                Times.Once);
        }

        private void GivenTheLatestEarlierDocument<TDocument>(string sql,
            TDocument document,
            params (string name, object value)[] parameters)
            where TDocument : IIdentifiable
        {
            _cosmosRepository.Setup(_ => _.QuerySql<TDocument>(It.Is(QueryMatching(sql, parameters)),
                    -1,
                    null))
                .ReturnsAsync(new[] {document});
        }

        private void GivenTheFeedIterator<TDocument>(string sql, 
            ICosmosDbFeedIterator<TDocument> feedIterator, 
            params (string name, object value)[] parameters)
        where TDocument : IIdentifiable
        {
            _cosmosRepository.Setup(_ => _.GetFeedIterator<TDocument>(It.Is(QueryMatching(sql, parameters)), 
                    100, 
                    null))
                .Returns(feedIterator);
        }

        private Expression<Func<CosmosDbQuery, bool>> QueryMatching(string sql, (string name, object value)[] parameters)
        {
            return query =>
                query.QueryText == sql &&
                HasParameters(query, parameters.Select(prm => NewParameter(prm.name, prm.value)).ToArray());
        }

        private void GivenTheCorrelationIdDetailsForCosmosQuery(string sql, string correlationId, CorrelationIdDetails details)
        {
            _cosmosRepository.Setup(_ => _.RawQuery<CorrelationIdDetails>(It.Is<CosmosDbQuery>(query =>
                        query.QueryText == sql &&
                        HasParameters(query, NewParameter("@correlationId", correlationId))),
                    -1, null))
                .ReturnsAsync(new[] {details});
        }

        private CosmosDbQueryParameter NewParameter(string name, object value) => new CosmosDbQueryParameter(name, value);

        private bool HasParameters(CosmosDbQuery query, params CosmosDbQueryParameter[] parameters)
        {
            return query.Parameters?.All(_ => parameters.Count(prm =>
                prm.Name == _.Name &&
                prm.Value.Equals(_.Value)) == 1) == true;
        }
    }
}