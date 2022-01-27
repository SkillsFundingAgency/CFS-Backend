using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Results.SqlExport;
using CalculateFunding.Services.SqlExport;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Language;
using Serilog.Core;

namespace CalculateFunding.Services.Results.UnitTests.SqlExport
{
    [TestClass]
    public class SqlImporterTests
    {
        private Mock<ICosmosDbFeedIterator> _cosmosFeed;
        private Mock<ISqlImportContext> _importContext;
        private Mock<ISqlImportContextBuilder> _importContextBuilder;
        private Mock<IDataTableImporter> _dataTableImporter;

        private Mock<IDataTableBuilder<ProviderResult>> _calculationRunDataTableBuilder;
        private Mock<IDataTableBuilder<ProviderResult>> _providerSummariesDataTableBuilder;
        private Mock<IDataTableBuilder<ProviderResult>> _templateCalculationsDataTableBuilder;
        private Mock<IDataTableBuilder<ProviderResult>> _additionalCalculationsDataTableBuilder;
        private Mock<IDataTableBuilder<ProviderResult>> _paymentFundingLineDataTableBuilder;
        private Mock<IDataTableBuilder<ProviderResult>> _informationFundingLineDataTableBuilder;

        private SqlImporter _sqlImporter;

        [TestInitialize]
        public void SetUp()
        {
            _cosmosFeed = new Mock<ICosmosDbFeedIterator>();
            _importContext = new Mock<ISqlImportContext>();
            _importContextBuilder = new Mock<ISqlImportContextBuilder>();
            _dataTableImporter = new Mock<IDataTableImporter>();

            _calculationRunDataTableBuilder = NewDataTableBuilder();
            _providerSummariesDataTableBuilder = NewDataTableBuilder();
            _templateCalculationsDataTableBuilder = NewDataTableBuilder();
            _additionalCalculationsDataTableBuilder = NewDataTableBuilder();
            _paymentFundingLineDataTableBuilder = NewDataTableBuilder();
            _informationFundingLineDataTableBuilder = NewDataTableBuilder();

            _importContext.Setup(_ => _.CalculationRuns)
                .Returns(_calculationRunDataTableBuilder.Object);
            _importContext.Setup(_ => _.ProviderSummaries)
                .Returns(_providerSummariesDataTableBuilder.Object);
            _importContext.Setup(_ => _.TemplateCalculations)
                .Returns(_templateCalculationsDataTableBuilder.Object);
            _importContext.Setup(_ => _.AdditionalCalculations)
                .Returns(_additionalCalculationsDataTableBuilder.Object);
            _importContext.Setup(_ => _.PaymentFundingLines)
                .Returns(_paymentFundingLineDataTableBuilder.Object);
            _importContext.Setup(_ => _.InformationFundingLines)
                .Returns(_informationFundingLineDataTableBuilder.Object);
            _importContext.Setup(_ => _.Documents)
                .Returns(_cosmosFeed.Object);

            _sqlImporter = new SqlImporter(new ProducerConsumerFactory(),
                _importContextBuilder.Object,
                _dataTableImporter.Object,
                Logger.None);
        }

        [TestMethod]
        public async Task TransformsPagesOfPublishedProviderDocumentsIntoDataTablesAndBulkCopiesTheseToSqlServer()
        {
            ProviderResult[] pageOne = new[]
            {
                NewProviderResult(),
                NewProviderResult(),
                NewProviderResult()
            };
            ProviderResult[] pageTwo = new[]
            {
                NewProviderResult(),
                NewProviderResult(),
                NewProviderResult()
            };
            ProviderResult[] pageThree = new[]
            {
                NewProviderResult(),
                NewProviderResult(),
                NewProviderResult()
            };

            string specificationId = NewRandomString();

            GivenTheImportContextIsCreatedForTheFundingInformation(specificationId);
            AndThePagesOfProviderResults(pageOne, pageTwo, pageThree);

            await WhenTheSqlImportRuns(specificationId);

            AndTheProviderResultsWereAddedToTheImportContextRows(pageOne);
            AndTheProviderResultsWereAddedToTheImportContextRows(pageTwo);
            AndTheProviderResultsWereAddedToTheImportContextRows(pageThree);
            AndTheImportContextWasBulkInsertedIntoSqlServer();
        }

        private void GivenTheImportContextIsCreatedForTheFundingInformation(string specificationId)
            => _importContextBuilder.Setup(_ => _.CreateImportContext(specificationId))
                .ReturnsAsync(_importContext.Object);

        private void AndTheImportContextWasBulkInsertedIntoSqlServer()
        {
            _dataTableImporter.Verify(_ => _.ImportDataTable(_calculationRunDataTableBuilder.Object, SqlBulkCopyOptions.Default));
            _dataTableImporter.Verify(_ => _.ImportDataTable(_providerSummariesDataTableBuilder.Object, SqlBulkCopyOptions.Default));
            _dataTableImporter.Verify(_ => _.ImportDataTable(_paymentFundingLineDataTableBuilder.Object, SqlBulkCopyOptions.Default));
            _dataTableImporter.Verify(_ => _.ImportDataTable(_informationFundingLineDataTableBuilder.Object, SqlBulkCopyOptions.Default));
            _dataTableImporter.Verify(_ => _.ImportDataTable(_templateCalculationsDataTableBuilder.Object, SqlBulkCopyOptions.Default));
            _dataTableImporter.Verify(_ => _.ImportDataTable(_additionalCalculationsDataTableBuilder.Object, SqlBulkCopyOptions.Default));
        }

        private void ThenTheProviderResultsWereAddedToTheImportContextRows(IEnumerable<ProviderResult> providerResults)
        {
            foreach (ProviderResult providerResult in providerResults)
            {
                _importContext.Verify(_ => _.AddRows(providerResult),
                    Times.Once);
            }
        }

        private void AndTheProviderResultsWereAddedToTheImportContextRows(IEnumerable<ProviderResult> providerResults)
            => ThenTheProviderResultsWereAddedToTheImportContextRows(providerResults);

        private async Task WhenTheSqlImportRuns(string specificationId)
            => await _sqlImporter.ImportData(specificationId);

        private void AndThePagesOfProviderResults(params IEnumerable<ProviderResult>[] pages)
        {
            ISetupSequentialResult<Task<IEnumerable<ProviderResult>>> reads = _cosmosFeed.SetupSequence(_ =>
                _.ReadNext<ProviderResult>(It.IsAny<CancellationToken>()));
            ISetupSequentialResult<bool> hasRecords = _cosmosFeed.SetupSequence(_ => _.HasMoreResults);

            foreach (IEnumerable<ProviderResult> page in pages)
            {
                reads.ReturnsAsync(page);
                hasRecords.Returns(true);
            }
        }

        private ProviderResult NewProviderResult(Action<ProviderResultBuilder> setUp = null)
        {
            ProviderResultBuilder providerResultBuilder = new ProviderResultBuilder();

            setUp?.Invoke(providerResultBuilder);

            return providerResultBuilder.Build();
        }

        private static Mock<IDataTableBuilder<ProviderResult>> NewDataTableBuilder() => new Mock<IDataTableBuilder<ProviderResult>>();

        private static int NewRandomNumber() => new RandomNumberBetween(1, int.MaxValue);

        private static string NewRandomString() => new RandomString();
    }
}
