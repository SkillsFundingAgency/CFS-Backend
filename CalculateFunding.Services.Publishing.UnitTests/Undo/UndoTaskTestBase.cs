using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces.Undo;
using CalculateFunding.Services.Publishing.Undo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests.Undo
{
    public abstract class UndoTaskTestBase : PublishedFundingUndoTestBase
    {
        protected IPublishedFundingUndoJobTask Task;

        protected Mock<IPublishedFundingUndoCosmosRepository> Cosmos;
        protected Mock<IPublishedFundingUndoBlobStoreRepository> BlobStore;
        protected ProducerConsumerFactory ProducerConsumerFactory;
        protected Mock<IJobTracker> JobTracker;
        protected ILogger Logger;
        protected PublishedFundingUndoTaskContext TaskContext;
        protected PublishedFundingUndoJobParameters Parameters;
        protected CorrelationIdDetails TaskDetails;

        [TestInitialize]
        public void UndoTaskTestBaseSetUp()
        {
            Cosmos = new Mock<IPublishedFundingUndoCosmosRepository>();
            BlobStore = new Mock<IPublishedFundingUndoBlobStoreRepository>();
            ProducerConsumerFactory = new ProducerConsumerFactory();
            JobTracker = new Mock<IJobTracker>();
            Logger = Serilog.Core.Logger.None;
        }

        protected async Task WhenTheTaskIsRun()
        {
            await Task.Run(TaskContext);
        }

        protected PublishedFundingUndoTaskContext NewPublishedFundingUndoTaskContext(Action<PublishedFundingUndoTaskContextBuilder> setUp = null)
        {
            PublishedFundingUndoTaskContextBuilder taskContextBuilder = new PublishedFundingUndoTaskContextBuilder();

            setUp?.Invoke(taskContextBuilder);

            return taskContextBuilder.Build();
        }

        protected PublishedFundingUndoJobParameters NewPublishedFundingUndoJobParameters(Action<PublishedFundingUndoJobParametersBuilder> setUp = null)
        {
            PublishedFundingUndoJobParametersBuilder parametersBuilder = new PublishedFundingUndoJobParametersBuilder();

            setUp?.Invoke(parametersBuilder);

            return parametersBuilder.Build();
        }

        protected void AndNothingWasUpdated<TDocument>()
            where TDocument : IIdentifiable
        {
            Cosmos.Verify(_ => _.BulkUpdatePublishedFundingDocuments(It.IsAny<IEnumerable<TDocument>>(),
                    It.IsAny<Func<TDocument, string>>()),
                Times.Never);
        }

        protected void AndTheDocumentsWereDeleted<TDocument>(IEnumerable<TDocument> documents,
            IEnumerable<string> expectedPartitionKeys,
            bool hardDelete)
            where TDocument : IIdentifiable
        {
            ThenTheDocumentsWereDeleted(documents, expectedPartitionKeys, hardDelete);
        }

        protected void ThenNothingWasDeleted<TDocument>()
            where TDocument : IIdentifiable
        {
            Cosmos.Verify(_ => _.BulkDeletePublishedFundingDocuments(It.IsAny<IEnumerable<TDocument>>(),
                    It.IsAny<Func<TDocument, string>>(),
                    It.IsAny<bool>()),
                Times.Never);
        }

        protected void ThenTheDocumentsWereDeleted<TDocument>(IEnumerable<TDocument> documents,
            IEnumerable<string> expectedPartitionKeys,
            bool hardDelete)
            where TDocument : IIdentifiable
        {
            Cosmos.Verify(_ => _.BulkDeletePublishedFundingDocuments(It.Is<IEnumerable<TDocument>>(docs =>
                        docs.SequenceEqual(documents)),
                    It.Is<Func<TDocument, string>>(accessor =>
                        MatchingPartitionKeyAccessor(accessor, documents, expectedPartitionKeys)),
                    hardDelete),
                Times.Once);
        }

        protected void AndTheDocumentsWereUpdated<TDocument>(IEnumerable<TDocument> documents,
            IEnumerable<string> expectedPartitionKeys)
            where TDocument : IIdentifiable
        {
            Cosmos.Verify(_ => _.BulkUpdatePublishedFundingDocuments(It.Is<IEnumerable<TDocument>>(docs =>
                        docs.SequenceEqual(documents)),
                    It.Is<Func<TDocument, string>>(accessor =>
                        MatchingPartitionKeyAccessor(accessor, documents, expectedPartitionKeys))),
                Times.Once);
        }

        protected void AndThePublishedFundingVersionBlobDocumentsWereRemoved(params PublishedFundingVersion[] publishedFundingVersions)
        {
            foreach (PublishedFundingVersion publishedFundingVersion in publishedFundingVersions)
            {
                BlobStore.Verify(_ => _.RemovePublishedFundingVersionBlob(publishedFundingVersion),
                    Times.Once);   
            }
        }
        
        protected void AndThePublishedProviderVersionBlobDocumentsWereRemoved(params PublishedProviderVersion[] publishedProviderVersions)
        {
            foreach (PublishedProviderVersion publishedProviderVersion in publishedProviderVersions)
            {
                BlobStore.Verify(_ => _.RemovePublishedProviderVersionBlob(publishedProviderVersion),
                    Times.Once);   
            }
        }

        private bool MatchingPartitionKeyAccessor<TDocument>(Func<TDocument, string> accessor, IEnumerable<TDocument> documents,
            IEnumerable<string> expectedPartitionKeys)
        {
            return documents
                .Select(accessor)
                .SequenceEqual(expectedPartitionKeys);
        }
    }
}