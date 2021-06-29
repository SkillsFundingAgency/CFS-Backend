using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Undo;
using ModelsGroupingReason = CalculateFunding.Models.Publishing.GroupingReason;

namespace CalculateFunding.Services.Publishing.Interfaces.Undo
{
    public interface IPublishedFundingUndoCosmosRepository
    {
        Task<UndoTaskDetails> GetCorrelationIdDetailsForPublishedProviderVersions(string correlationId);
        
        ICosmosDbFeedIterator GetPublishedProviderVersions(string fundingStreamId,
            string fundingPeriodId,
            DateTimeOffset sinceTimeStamp);

        ICosmosDbFeedIterator GetPublishedProviders(string fundingStreamId,
            string fundingPeriodId,
            DateTimeOffset sinceTimeStamp,
            string correlationId);

        ICosmosDbFeedIterator GetPublishedFundingVersions(string fundingStreamId,
            string fundingPeriodId,
            DateTimeOffset sinceTimeStamp);

        ICosmosDbFeedIterator GetPublishedFunding(string fundingStreamId,
            string fundingPeriodId,
            DateTimeOffset sinceTimeStamp);

        Task<PublishedFundingVersion> GetLatestEarlierPublishedFundingVersion(string fundingStreamId,
            string fundingPeriodId,
            DateTimeOffset sinceTimeStamp,
            string groupTypeIdentifier,
            string groupTypeIdentifierValue,
            ModelsGroupingReason groupingReason);

        Task<PublishedProviderVersion> GetLatestEarlierPublishedProviderVersion(string fundingStreamId,
            string fundingPeriodId,
            DateTimeOffset sinceTimeStamp,
            string providerId,
            PublishedProviderStatus? status = null);

        Task BulkDeletePublishedFundingDocuments<TDocument>(IEnumerable<TDocument> documents, 
            Func<TDocument, string> partitionKeyAccessor, 
            bool hardDelete = false)
            where TDocument : IIdentifiable;

        Task BulkUpdatePublishedFundingDocuments<TDocument>(IEnumerable<TDocument> documents, 
            Func<TDocument, string> partitionKeyAccessor)
            where TDocument : IIdentifiable;

        ICosmosDbFeedIterator GetPublishedProviderVersionsFromVersion(string fundingStreamId,
            string fundingPeriodId,
            decimal version);

        ICosmosDbFeedIterator GetPublishedProvidersFromVersion(string fundingStreamId,
            string fundingPeriodId,
            decimal version);

        ICosmosDbFeedIterator GetPublishedFundingFromVersion(string fundingStreamId,
            string fundingPeriodId,
            decimal version);

        ICosmosDbFeedIterator GetPublishedFundingVersionsFromVersion(string fundingStreamId,
            string fundingPeriodId,
            decimal version);

        Task<PublishedFundingVersion> GetLatestEarlierPublishedFundingVersionFromVersion(string fundingStreamId,
            string fundingPeriodId,
            decimal version,
            string groupTypeIdentifier,
            string groupTypeIdentifierValue,
            ModelsGroupingReason groupingReason);

        Task<PublishedProviderVersion> GetLatestEarlierPublishedProviderVersionFromVersion(string fundingStreamId,
            string fundingPeriodId,
            decimal version,
            string providerId,
            PublishedProviderStatus? status = null);
    }
}