using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.SqlExport;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.SqlExport
{
    public class SqlImporter : ISqlImporter
    {
        private readonly IProducerConsumerFactory _producerConsumerFactory;
        private readonly ISqlImportContextBuilder _sqlImportContextBuilder;
        private readonly IDataTableImporter _dataTableImporter;
        private readonly ILogger _logger;

        public SqlImporter(IProducerConsumerFactory producerConsumerFactory,
            ISqlImportContextBuilder sqlImportContextBuilder,
            IDataTableImporter dataTableImporter,
            ILogger logger)
        {
            Guard.ArgumentNotNull(producerConsumerFactory, nameof(producerConsumerFactory));
            Guard.ArgumentNotNull(sqlImportContextBuilder, nameof(sqlImportContextBuilder));
            Guard.ArgumentNotNull(dataTableImporter, nameof(dataTableImporter));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _producerConsumerFactory = producerConsumerFactory;
            _sqlImportContextBuilder = sqlImportContextBuilder;
            _logger = logger;
            _dataTableImporter = dataTableImporter;
        }

        public async Task ImportData(HashSet<string> providers, string specificationId)
        {
            ISqlImportContext importContext = await _sqlImportContextBuilder.CreateImportContext(specificationId, providers);

            IProducerConsumer producerConsumer = _producerConsumerFactory.CreateProducerConsumer(ProduceProviderResults,
                PopulateDataTables,
                7,
                5,
                _logger);

            await producerConsumer.Run(importContext);
            await RunBulkImports(importContext);
        }

        private async Task RunBulkImports(ISqlImportContext importContext)
        {
            await _dataTableImporter.ImportDataTable(importContext.CalculationRuns);
            await _dataTableImporter.ImportDataTable(importContext.ProviderSummaries);
            await _dataTableImporter.ImportDataTable(importContext.TemplateCalculations);
            await _dataTableImporter.ImportDataTable(importContext.AdditionalCalculations);
            await _dataTableImporter.ImportDataTable(importContext.PaymentFundingLines);
            await _dataTableImporter.ImportDataTable(importContext.InformationFundingLines);
        }

        private async Task<(bool isComplete, IEnumerable<ProviderResult> items)> ProduceProviderResults(CancellationToken cancellationToken, dynamic context)
        {
            try
            {
                ICosmosDbFeedIterator feed = ((ISqlImportContext)context).Documents;

                if (!feed.HasMoreResults)
                {
                    return (true, ArraySegment<ProviderResult>.Empty);
                }

                IEnumerable<ProviderResult> documents = await feed.ReadNext<ProviderResult>(cancellationToken);

                while (documents.IsNullOrEmpty() && feed.HasMoreResults)
                {
                    documents = await feed.ReadNext<ProviderResult>(cancellationToken);
                }

                if (documents.IsNullOrEmpty() && !feed.HasMoreResults)
                {
                    return (true, ArraySegment<ProviderResult>.Empty);
                }

                return (false, documents.ToArray());
            }
            catch
            {
                return (true, ArraySegment<ProviderResult>.Empty);
            }
        }

        protected Task PopulateDataTables(CancellationToken cancellationToken, dynamic context, IEnumerable<ProviderResult> providerResults)
        {
            ISqlImportContext importContext = (ISqlImportContext)context;

            foreach (ProviderResult providerResult in providerResults.Where(_ => importContext.Providers.Contains(_.Provider.Id)))
            {
                importContext.AddRows(providerResult);
            }

            return Task.CompletedTask;
        }

    }
}
