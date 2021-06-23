using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using Polly;
using ModelsGroupingReason = CalculateFunding.Models.Publishing.GroupingReason;

namespace CalculateFunding.Services.Publishing.Undo.Repositories
{
    public class PublishedFundingUndoCosmosRepository : IPublishedFundingUndoCosmosRepository
    {
        private readonly AsyncPolicy _resilience;
        private readonly ICosmosRepository _cosmos;

        public PublishedFundingUndoCosmosRepository(IPublishingResiliencePolicies resiliencePolicies,
            ICosmosRepository cosmos)
        {
            Guard.ArgumentNotNull(resiliencePolicies?.PublishedFundingRepository, nameof(resiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(cosmos, nameof(cosmos));

            _resilience = resiliencePolicies.PublishedFundingRepository;
            _cosmos = cosmos;
        }

        public async Task<UndoTaskDetails> GetCorrelationDetailsForPublishedProviders(string correlationId) =>
            await GetDocumentCorrelationIdDetails(
                @"SELECT
                              MIN(p._ts) AS timeStamp,
                              p.content.current.fundingStreamId,
                              p.content.current.fundingPeriodId
                        FROM publishedProvider p
                        WHERE p.documentType = 'PublishedProvider'
                        AND p.content.current.correlationId = @correlationId
                        AND p.deleted = false
                        GROUP BY p.content.current.fundingStreamId,
                        p.content.current.fundingPeriodId",
                correlationId);

        public async Task<UndoTaskDetails> GetCorrelationIdDetailsForPublishedProviderVersions(string correlationId) =>
            await GetDocumentCorrelationIdDetails(
                @"SELECT
                              MIN(p._ts) AS timeStamp,
                              p.content.fundingStreamId,
                              p.content.fundingPeriodId
                        FROM publishedProviderVersion p
                        WHERE p.documentType = 'PublishedProviderVersion'
                        AND p.content.correlationId = @correlationId
                        AND p.deleted = false
                        GROUP BY p.content.fundingStreamId,
                        p.content.fundingPeriodId",
                correlationId);


        public async Task<UndoTaskDetails> GetCorrelationIdDetailsForPublishedFundingVersions(string correlationId) =>
            await GetDocumentCorrelationIdDetails(
                @"SELECT
                              MIN(p._ts) AS timeStamp,
                              p.content.fundingStreamId,
                              p.content.fundingPeriod.id AS fundingPeriodId
                        FROM publishedFundingVersion p
                        WHERE p.documentType = 'PublishedFundingVersion'
                        AND p.content.correlationId = @correlationId
                        AND p.deleted = false
                        GROUP BY p.content.fundingStreamId,
                        p.content.fundingPeriod.id",
                correlationId);

        public async Task<UndoTaskDetails> GetCorrelationIdDetailsForPublishedFunding(string correlationId) =>
            await GetDocumentCorrelationIdDetails(
                @"SELECT
                              MIN(p._ts) AS timeStamp,
                              p.content.current.fundingStreamId,
                              p.content.current.fundingPeriod.id AS fundingPeriodId
                        FROM publishedFunding p
                        WHERE p.documentType = 'PublishedFunding'
                        AND p.content.current.correlationId = @correlationId
                        AND p.deleted = false
                        GROUP BY p.content.current.fundingStreamId,
                        p.content.current.fundingPeriod.id",
                correlationId);

        private async Task<UndoTaskDetails> GetDocumentCorrelationIdDetails(string sql,
            string correlationId)
        {
            return (await _resilience.ExecuteAsync(() => _cosmos.RawQuery<UndoTaskDetails>(new CosmosDbQuery
                {
                    QueryText = sql,
                    Parameters = Parameters(("@correlationId", correlationId))
                })))
                .SingleOrDefault();
        }

        public ICosmosDbFeedIterator GetPublishedProviderVersionsFromVersion(string fundingStreamId,
            string fundingPeriodId,
            decimal version) =>
            GetDocumentFeedFromVersion<PublishedProviderVersion>(
                @"SELECT
                              *
                        FROM publishedProviderVersion p
                        WHERE p.documentType = 'PublishedProviderVersion'
                        AND StringToNumber(CONCAT(Tostring(p.content.majorVersion), '.', Tostring(p.content.minorVersion))) >= @version
                        AND p.content.fundingStreamId = @fundingStreamId
                        AND p.content.fundingPeriodId = @fundingPeriodId
                        AND p.deleted = false",
                fundingStreamId,
                fundingPeriodId,
                version);

        public ICosmosDbFeedIterator GetPublishedProviderVersions(string fundingStreamId,
            string fundingPeriodId,
            long sinceTimeStamp) =>
            GetDocumentFeed(
                @"SELECT
                              *
                        FROM publishedProviderVersion p
                        WHERE p.documentType = 'PublishedProviderVersion'
                        AND p._ts >= @sinceTimeStamp
                        AND p.content.fundingStreamId = @fundingStreamId
                        AND p.content.fundingPeriodId = @fundingPeriodId
                        AND p.deleted = false",
                fundingStreamId,
                fundingPeriodId,
                sinceTimeStamp);

        public ICosmosDbFeedIterator GetPublishedProvidersFromVersion(string fundingStreamId,
            string fundingPeriodId,
            decimal version) =>
            GetDocumentFeedFromVersion<PublishedProvider>(
                @"SELECT
                              *
                        FROM publishedProvider p
                        WHERE p.documentType = 'PublishedProvider'
                        AND StringToNumber(CONCAT(Tostring(p.content.current.majorVersion), '.', Tostring(p.content.current.minorVersion))) >= @version
                        AND p.content.current.fundingStreamId = @fundingStreamId
                        AND p.content.current.fundingPeriodId = @fundingPeriodId
                        AND p.deleted = false",
                fundingStreamId,
                fundingPeriodId,
                version);

        public ICosmosDbFeedIterator GetPublishedProviders(string fundingStreamId,
            string fundingPeriodId,
            long sinceTimeStamp) =>
            GetDocumentFeed(
                @"SELECT
                              *
                        FROM publishedProvider p
                        WHERE p.documentType = 'PublishedProvider'
                        AND p._ts >= @sinceTimeStamp
                        AND p.content.current.fundingStreamId = @fundingStreamId
                        AND p.content.current.fundingPeriodId = @fundingPeriodId
                        AND p.deleted = false",
                fundingStreamId,
                fundingPeriodId,
                sinceTimeStamp);

        public ICosmosDbFeedIterator GetPublishedFundingFromVersion(string fundingStreamId,
            string fundingPeriodId,
            decimal version) =>
            GetDocumentFeedFromVersion<PublishedFunding>(
                @"SELECT
                              *
                        FROM publishedFunding p
                        WHERE p.documentType = 'PublishedFunding'
                        AND StringToNumber(CONCAT(Tostring(p.content.current.majorVersion), '.', Tostring(p.content.current.minorVersion))) >= @version
                        AND p.content.current.fundingStreamId = @fundingStreamId
                        AND p.content.current.fundingPeriod.id = @fundingPeriodId
                        AND p.deleted = false",
                fundingStreamId,
                fundingPeriodId,
                version);

        public ICosmosDbFeedIterator GetPublishedFunding(string fundingStreamId,
            string fundingPeriodId,
            long sinceTimeStamp) =>
            GetDocumentFeed(
                @"SELECT
                              *
                        FROM publishedFunding p
                        WHERE p.documentType = 'PublishedFunding'
                        AND p._ts >= @sinceTimeStamp
                        AND p.content.current.fundingStreamId = @fundingStreamId
                        AND p.content.current.fundingPeriod.id = @fundingPeriodId
                        AND p.deleted = false",
                fundingStreamId,
                fundingPeriodId,
                sinceTimeStamp);

        public ICosmosDbFeedIterator GetPublishedFundingVersionsFromVersion(string fundingStreamId,
            string fundingPeriodId,
            decimal version) =>
            GetDocumentFeedFromVersion<PublishedFundingVersion>(
                @"SELECT
                              *
                        FROM publishedFundingVersion p
                        WHERE p.documentType = 'PublishedFundingVersion'
                        AND StringToNumber(CONCAT(Tostring(p.content.majorVersion), '.', Tostring(p.content.minorVersion))) >= @version
                        AND p.content.fundingStreamId = @fundingStreamId
                        AND p.content.fundingPeriod.id = @fundingPeriodId
                        AND p.deleted = false",
                fundingStreamId,
                fundingPeriodId,
                version);

        public ICosmosDbFeedIterator GetPublishedFundingVersions(string fundingStreamId,
            string fundingPeriodId,
            long sinceTimeStamp) =>
            GetDocumentFeed(
                @"SELECT
                              *
                        FROM publishedFundingVersion p
                        WHERE p.documentType = 'PublishedFundingVersion'
                        AND p._ts >= @sinceTimeStamp
                        AND p.content.fundingStreamId = @fundingStreamId
                        AND p.content.fundingPeriod.id = @fundingPeriodId
                        AND p.deleted = false",
                fundingStreamId,
                fundingPeriodId,
                sinceTimeStamp);

        private ICosmosDbFeedIterator GetDocumentFeed(string sql,
            string fundingStreamId,
            string fundingPeriodId,
            long sinceTimeStamp) =>
            _cosmos.GetFeedIterator(new CosmosDbQuery
                {
                    QueryText = sql,
                    Parameters = Parameters(
                        ("@sinceTimeStamp", sinceTimeStamp),
                        ("@fundingStreamId", fundingStreamId),
                        ("@fundingPeriodId", fundingPeriodId))
                },
                100);

        private ICosmosDbFeedIterator GetDocumentFeedFromVersion<TDocument>(string sql,
            string fundingStreamId,
            string fundingPeriodId,
            decimal version)
            where TDocument : IIdentifiable =>
            _cosmos.GetFeedIterator(new CosmosDbQuery
                {
                    QueryText = sql,
                    Parameters = Parameters(
                        ("@version", version),
                        ("@fundingStreamId", fundingStreamId),
                        ("@fundingPeriodId", fundingPeriodId))
                },
                100);
        
        public async Task<PublishedFundingVersion> GetLatestEarlierPublishedFundingVersionFromVersion(string fundingStreamId,
            string fundingPeriodId,
            decimal version,
            string groupTypeIdentifier,
            string groupTypeIdentifierValue,
            ModelsGroupingReason groupingReason) =>
            await GetLatestEarlierDocumentFromVersion<PublishedFundingVersion>(
                @"SELECT
                              TOP 1 *
                        FROM publishedFundingVersion p
                        WHERE p.documentType = 'PublishedFundingVersion'
                        AND StringToNumber(CONCAT(Tostring(p.content.majorVersion), '.', Tostring(p.content.minorVersion))) < @version
                        AND p.content.fundingStreamId = @fundingStreamId
                        AND p.content.fundingPeriod.id = @fundingPeriodId
                        AND p.content.organisationGroupTypeIdentifier = @groupTypeIdentifier
                        AND p.content.organisationGroupIdentifierValue = @groupTypeIdentifierValue
                        AND p.content.groupingReason = @groupingReason
                        AND p.deleted = false
                        ORDER BY p.content.majorVersion DESC, p.content.minorVersion DESC",
                fundingStreamId,
                fundingPeriodId,
                version,
                Parameters(("@groupTypeIdentifier", groupTypeIdentifier),
                    ("@groupTypeIdentifierValue", groupTypeIdentifierValue),
                    ("@groupingReason", groupingReason.ToString())));

        public async Task<PublishedFundingVersion> GetLatestEarlierPublishedFundingVersion(string fundingStreamId,
            string fundingPeriodId,
            long sinceTimeStamp,
            string groupTypeIdentifier,
            string groupTypeIdentifierValue,
            ModelsGroupingReason groupingReason) =>
            await GetLatestEarlierDocument<PublishedFundingVersion>(
                @"SELECT
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
                fundingStreamId,
                fundingPeriodId,
                sinceTimeStamp,
                Parameters(("@groupTypeIdentifier", groupTypeIdentifier),
                    ("@groupTypeIdentifierValue", groupTypeIdentifierValue),
                    ("@groupingReason", groupingReason.ToString())));
        
        public async Task<PublishedProviderVersion> GetLatestEarlierPublishedProviderVersionFromVersion(string fundingStreamId,
            string fundingPeriodId,
            decimal version,
            string providerId,
            PublishedProviderStatus? status = null) =>
            await GetLatestEarlierDocumentFromVersion<PublishedProviderVersion>(
                @$"SELECT
                              TOP 1 *
                        FROM publishedProviderVersion p
                        WHERE p.documentType = 'PublishedProviderVersion'
                        AND StringToNumber(CONCAT(Tostring(p.content.majorVersion), '.', Tostring(p.content.minorVersion))) < @version
                        AND p.content.fundingStreamId = @fundingStreamId
                        AND p.content.fundingPeriodId = @fundingPeriodId
                        AND p.content.providerId = @providerId
                        {(status.HasValue ? "AND p.content.status = @status" : string.Empty)}
                        AND p.deleted = false
                        ORDER BY p.content.majorVersion DESC, p.content.minorVersion DESC",
                fundingStreamId,
                fundingPeriodId,
                version,
                Parameters(("@providerId", providerId))
                    .Concat(status.HasValue ? Parameters(("@status", status.Value)) : ArraySegment<CosmosDbQueryParameter>.Empty).ToArray());

        public async Task<PublishedProviderVersion> GetLatestEarlierPublishedProviderVersion(string fundingStreamId,
            string fundingPeriodId,
            long sinceTimeStamp,
            string providerId,
            PublishedProviderStatus? status = null) =>
            await GetLatestEarlierDocument<PublishedProviderVersion>(
                @$"SELECT
                              TOP 1 *
                        FROM publishedProviderVersion p
                        WHERE p.documentType = 'PublishedProviderVersion'
                        AND p._ts < @sinceTimeStamp
                        AND p.content.fundingStreamId = @fundingStreamId
                        AND p.content.fundingPeriodId = @fundingPeriodId
                        AND p.content.providerId = @providerId
                        {(status.HasValue ? "AND p.content.status = @status" : string.Empty)}
                        AND p.deleted = false
                        ORDER BY p._ts DESC",
                fundingStreamId,
                fundingPeriodId,
                sinceTimeStamp,
                Parameters(("@providerId", providerId))
                    .Concat(status.HasValue ? Parameters(("@status", status.Value)) : ArraySegment<CosmosDbQueryParameter>.Empty).ToArray());

        private async Task<TDocument> GetLatestEarlierDocumentFromVersion<TDocument>(string sql,
            string fundingStreamId,
            string fundingPeriodId,
            decimal version,
            params CosmosDbQueryParameter[] extraParameters)
            where TDocument : IIdentifiable
        {
            return (await _resilience.ExecuteAsync(() => _cosmos.QuerySql<TDocument>(new CosmosDbQuery
            {
                QueryText = sql,
                Parameters = Parameters(("@version", version),
                        ("@fundingStreamId", fundingStreamId),
                        ("@fundingPeriodId", fundingPeriodId))
                    .Concat(extraParameters)
            }))).SingleOrDefault();
        }
        
        private async Task<TDocument> GetLatestEarlierDocument<TDocument>(string sql,
            string fundingStreamId,
            string fundingPeriodId,
            long sinceTimeStamp,
            params CosmosDbQueryParameter[] extraParameters)
            where TDocument : IIdentifiable
        {
            return (await _resilience.ExecuteAsync(() => _cosmos.QuerySql<TDocument>(new CosmosDbQuery
            {
                QueryText = sql,
                Parameters = Parameters(("@sinceTimeStamp", sinceTimeStamp),
                        ("@fundingStreamId", fundingStreamId),
                        ("@fundingPeriodId", fundingPeriodId))
                    .Concat(extraParameters)
            }))).SingleOrDefault();
        }

        public async Task BulkDeletePublishedFundingDocuments<TDocument>(IEnumerable<TDocument> documents,
            Func<TDocument, string> partitionKeyAccessor,
            bool hardDelete = false)
            where TDocument : IIdentifiable
        {
            await _resilience.ExecuteAsync(() => _cosmos.BulkDeleteAsync(
                documents.ToKeyValuePairs(partitionKeyAccessor),
                hardDelete: hardDelete));
        }

        public async Task BulkUpdatePublishedFundingDocuments<TDocument>(IEnumerable<TDocument> documents,
            Func<TDocument, string> partitionKeyAccessor)
            where TDocument : IIdentifiable
        {
            await _resilience.ExecuteAsync(() => _cosmos.BulkUpsertAsync(
                documents.ToKeyValuePairs(partitionKeyAccessor)));
        }

        private CosmosDbQueryParameter[] Parameters(params (string Name, object Value)[] parameters)
            => parameters.Select(_ => new CosmosDbQueryParameter(_.Name, _.Value)).ToArray();
    }
}