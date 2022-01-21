using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.SqlExport;
using Serilog;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class SqlImporter : ISqlImporter
    {
        private readonly IProducerConsumerFactory _producerConsumerFactory;
        private readonly ISqlImportContextBuilder _sqlImportContextBuilder;
        private readonly IPublishingDataTableImporterLocator _dataTableImporterLocator;
        private readonly ILogger _logger;

        public SqlImporter(IProducerConsumerFactory producerConsumerFactory,
            ISqlImportContextBuilder sqlImportContextBuilder,
            IPublishingDataTableImporterLocator dataTableImporterLocator,
            ILogger logger)
        {
            Guard.ArgumentNotNull(producerConsumerFactory, nameof(producerConsumerFactory));
            Guard.ArgumentNotNull(sqlImportContextBuilder, nameof(sqlImportContextBuilder));
            Guard.ArgumentNotNull(dataTableImporterLocator, nameof(dataTableImporterLocator));
            Guard.ArgumentNotNull(logger, nameof(logger));
            
            _producerConsumerFactory = producerConsumerFactory;
            _sqlImportContextBuilder = sqlImportContextBuilder;
            _logger = logger;
            _dataTableImporterLocator = dataTableImporterLocator;
        }

        public async Task ImportData(
            string specificationId,
            string fundingStreamId,
            SchemaContext schemaContext,
            SqlExportSource sqlExportSource)
        {
            ISqlImportContext importContext = await _sqlImportContextBuilder.CreateImportContext(specificationId, fundingStreamId, schemaContext, sqlExportSource);

            IProducerConsumer producerConsumer = _producerConsumerFactory.CreateProducerConsumer(ProducePublishedProviders,
                PopulateDataTables,
                7,
                5,
                _logger);

            await producerConsumer.Run(importContext);
            await RunBulkImports(importContext, sqlExportSource);
        }

        private async Task RunBulkImports(
            ISqlImportContext importContext,
            SqlExportSource sqlExportSource)
        {
            IDataTableImporter dataTableImporter = _dataTableImporterLocator.GetService(sqlExportSource);

            await dataTableImporter.ImportDataTable(importContext.Funding);
            await dataTableImporter.ImportDataTable(importContext.Providers);
            await dataTableImporter.ImportDataTable(importContext.Calculations);
            await dataTableImporter.ImportDataTable(importContext.PaymentFundingLines);
            await dataTableImporter.ImportDataTable(importContext.InformationFundingLines);

            foreach (IDataTableBuilder<PublishedProviderVersion> paymentFundingLine in importContext.Profiling?.Values ?? ArraySegment<IDataTableBuilder<PublishedProviderVersion>>.Empty)
            {
                await dataTableImporter.ImportDataTable(paymentFundingLine);
            }
        }

        private async Task<(bool isComplete, IEnumerable<PublishedProvider> items)> ProducePublishedProviders(CancellationToken cancellationToken,
            dynamic context)
        {
            try
            {
                ICosmosDbFeedIterator feed = ((ISqlImportContext) context).Documents;

                if (!feed.HasMoreResults)
                {
                    return (true, ArraySegment<PublishedProvider>.Empty);
                }

                IEnumerable<PublishedProvider> documents = await feed.ReadNext<PublishedProvider>(cancellationToken);

                while (documents.IsNullOrEmpty() && feed.HasMoreResults)
                {
                    documents = await feed.ReadNext<PublishedProvider>(cancellationToken);
                }

                if (documents.IsNullOrEmpty() && !feed.HasMoreResults)
                {
                    return (true, ArraySegment<PublishedProvider>.Empty);
                }

                return (false, documents.ToArray());
            }
            catch
            {
                return (true, ArraySegment<PublishedProvider>.Empty);
            }
        }

        protected Task PopulateDataTables(CancellationToken cancellationToken,
            dynamic context,
            IEnumerable<PublishedProvider> publishedProviders)
        {
            ISqlImportContext importContext = (ISqlImportContext) context;

            foreach (PublishedProvider publishedProvider in publishedProviders)
            {
                importContext.AddRows(
                    importContext.SqlExportSource == SqlExportSource.CurrentPublishedProviderVersion ? 
                    publishedProvider.Current : 
                    publishedProvider.Released);
            }

            return Task.CompletedTask;
        }
    }
}