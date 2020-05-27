using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Services.Publishing.Undo;
using ModelsGroupingReason = CalculateFunding.Models.Publishing.GroupingReason;

namespace CalculateFunding.Services.Publishing.Interfaces.Undo
{
    public interface IPublishedFundingUndoCosmosRepository
    {
        Task<CorrelationIdDetails> GetCorrelationDetailsForPublishedProviders(string correlationId);
        
        Task<CorrelationIdDetails> GetCorrelationIdDetailsForPublishedProviderVersions(string correlationId);
        
        Task<CorrelationIdDetails> GetCorrelationIdDetailsForPublishedFundingVersions(string correlationId);

        ICosmosDbFeedIterator<PublishedProviderVersion> GetPublishedProviderVersions(string fundingStreamId,
            string fundingPeriodId,
            long sinceTimeStamp);

        ICosmosDbFeedIterator<PublishedProvider> GetPublishedProviders(string fundingStreamId,
            string fundingPeriodId,
            long sinceTimeStamp);

        ICosmosDbFeedIterator<PublishedFundingVersion> GetPublishedFundingVersions(string fundingStreamId,
            string fundingPeriodId,
            long sinceTimeStamp);

        ICosmosDbFeedIterator<PublishedFunding> GetPublishedFunding(string fundingStreamId,
            string fundingPeriodId,
            long sinceTimeStamp);

        Task<CorrelationIdDetails> GetCorrelationIdDetailsForPublishedFunding(string correlationId);

        Task<PublishedFundingVersion> GetLatestEarlierPublishedFundingVersion(string fundingStreamId,
            string fundingPeriodId,
            long sinceTimeStamp,
            string groupTypeIdentifier,
            string groupTypeIdentifierValue,
            ModelsGroupingReason groupingReason);

        Task<PublishedProviderVersion> GetLatestEarlierPublishedProviderVersion(string fundingStreamId,
            string fundingPeriodId,
            long sinceTimeStamp,
            string providerId,
            PublishedProviderStatus? status = null);

        Task BulkDeletePublishedFundingDocuments<TDocument>(IEnumerable<TDocument> documents, 
            Func<TDocument, string> partitionKeyAccessor, 
            bool hardDelete = false)
            where TDocument : IIdentifiable;

        Task BulkUpdatePublishedFundingDocuments<TDocument>(IEnumerable<TDocument> documents, 
            Func<TDocument, string> partitionKeyAccessor)
            where TDocument : IIdentifiable;
    }
}