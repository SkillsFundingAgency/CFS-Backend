using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Publishing.SqlExport;
using CalculateFunding.Services.SqlExport;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Language;
using Serilog.Core;

namespace CalculateFunding.Services.Publishing.UnitTests.SqlExport
{
    [TestClass]
    public class SqlImporterTests
    {
        private Mock<ICosmosDbFeedIterator> _cosmosFeed;
        private Mock<ISqlImportContext> _importContext;
        private Mock<ISqlImportContextBuilder> _importContextBuilder;
        private Mock<IDataTableImporter> _dataTableImporter;

        private Mock<IDataTableBuilder<PublishedProviderVersion>> _paymentFundingLineDataTableBuilder;
        private Mock<IDataTableBuilder<PublishedProviderVersion>> _informationFundingLineDataTableBuilder;
        private Mock<IDataTableBuilder<PublishedProviderVersion>> _publishedProviderVersionDataTableBuilder;
        private Mock<IDataTableBuilder<PublishedProviderVersion>> _fundingLineOneProfilingDataTableBuilder;
        private Mock<IDataTableBuilder<PublishedProviderVersion>> _fundingLineTwoProfilingDataTableBuilder;
        private Mock<IDataTableBuilder<PublishedProviderVersion>> _calculationDataTableBuilder;
        private Mock<IDataTableBuilder<PublishedProviderVersion>> _fundingDataTableBuilder;
        
        private SqlImporter _sqlImporter;

        [TestInitialize]
        public void SetUp()
        {
            _cosmosFeed = new Mock<ICosmosDbFeedIterator>();
            _importContext = new Mock<ISqlImportContext>();
            _importContextBuilder = new Mock<ISqlImportContextBuilder>();
            _dataTableImporter = new Mock<IDataTableImporter>();
            
            _fundingLineOneProfilingDataTableBuilder = NewDataTableBuilder();
            _fundingLineTwoProfilingDataTableBuilder = NewDataTableBuilder();
            _paymentFundingLineDataTableBuilder = NewDataTableBuilder();
            _informationFundingLineDataTableBuilder = NewDataTableBuilder();
            _calculationDataTableBuilder = NewDataTableBuilder();
            _publishedProviderVersionDataTableBuilder = NewDataTableBuilder();
            _fundingDataTableBuilder = NewDataTableBuilder();

            _importContext.Setup(_ => _.Providers)
                .Returns(_publishedProviderVersionDataTableBuilder.Object);
            _importContext.Setup(_ => _.Calculations)
                .Returns(_calculationDataTableBuilder.Object);
            _importContext.Setup(_ => _.Funding)
                .Returns(_fundingDataTableBuilder.Object);
            _importContext.Setup(_ => _.Profiling)
                .Returns(new Dictionary<uint, IDataTableBuilder<PublishedProviderVersion>>
                {
                    {
                        (uint) NewRandomNumber(), _fundingLineOneProfilingDataTableBuilder.Object
                    },
                    {
                        (uint) NewRandomNumber(), _fundingLineTwoProfilingDataTableBuilder.Object
                    },
                });
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
            PublishedProvider[] pageOne = new[]
            {
                NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion())),
                NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion())),
                NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion()))
            };
            PublishedProvider[] pageTwo = new[]
            {
                NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion())),
                NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion())),
                NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion()))
            };
            PublishedProvider[] pageThree = new[]
            {
                NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion())),
                NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion())),
                NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion()))
            };

            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();

            GivenTheImportContextIsCreatedForTheFundingInformation(specificationId, fundingStreamId);
            AndThePagesOfPublishedProviders(pageOne, pageTwo, pageThree);

            await WhenTheSqlImportRuns(specificationId, fundingStreamId);
            
            ThenThePublishedProviderVersionsWereAddedToTheImportContextRows(pageOne);
            AndThePublishedProviderVersionsWereAddedToTheImportContextRows(pageTwo);
            AndThePublishedProviderVersionsWereAddedToTheImportContextRows(pageThree);
            AndTheImportContextWasBulkInsertedIntoSqlServer();
        }

        private void GivenTheImportContextIsCreatedForTheFundingInformation(string specificationId,
            string fundingStreamId)
            => _importContextBuilder.Setup(_ => _.CreateImportContext(specificationId, fundingStreamId, null))
                .ReturnsAsync(_importContext.Object);
        
        private void AndTheImportContextWasBulkInsertedIntoSqlServer()
        {
            _dataTableImporter.Verify(_ => _.ImportDataTable(_calculationDataTableBuilder.Object));
            _dataTableImporter.Verify(_ => _.ImportDataTable(_informationFundingLineDataTableBuilder.Object));
            _dataTableImporter.Verify(_ => _.ImportDataTable(_paymentFundingLineDataTableBuilder.Object));
            _dataTableImporter.Verify(_ => _.ImportDataTable(_fundingDataTableBuilder.Object));
            _dataTableImporter.Verify(_ => _.ImportDataTable(_fundingLineOneProfilingDataTableBuilder.Object));
            _dataTableImporter.Verify(_ => _.ImportDataTable(_fundingLineTwoProfilingDataTableBuilder.Object));
            _dataTableImporter.Verify(_ => _.ImportDataTable(_publishedProviderVersionDataTableBuilder.Object));
        }
        
        private void ThenThePublishedProviderVersionsWereAddedToTheImportContextRows(IEnumerable<PublishedProvider> publishedProviders)
        {
            foreach (PublishedProvider publishedProvider in publishedProviders)
            {
                _importContext.Verify(_ => _.AddRows(publishedProvider.Current),
                    Times.Once);
            }
        }

        private void AndThePublishedProviderVersionsWereAddedToTheImportContextRows(IEnumerable<PublishedProvider> publishedProviders)
            => ThenThePublishedProviderVersionsWereAddedToTheImportContextRows(publishedProviders);
        
        private async Task WhenTheSqlImportRuns(string specificationId,
            string fundingStreamId)
            => await _sqlImporter.ImportData(specificationId, fundingStreamId, null);

        private void AndThePagesOfPublishedProviders(params IEnumerable<PublishedProvider>[] pages)
        {
            ISetupSequentialResult<Task<IEnumerable<PublishedProvider>>> reads = _cosmosFeed.SetupSequence(_ => 
                _.ReadNext<PublishedProvider>(It.IsAny<CancellationToken>()));
            ISetupSequentialResult<bool> hasRecords = _cosmosFeed.SetupSequence(_ => _.HasMoreResults);
            
            foreach (IEnumerable<PublishedProvider> page in pages)
            {
                reads.ReturnsAsync(page);
                hasRecords.Returns(true);
            }
        }

        private PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        {
            PublishedProviderBuilder publishedProviderBuilder = new PublishedProviderBuilder();

            setUp?.Invoke(publishedProviderBuilder);
            
            return publishedProviderBuilder.Build();
        }

        private PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(publishedProviderVersionBuilder);
            
            return publishedProviderVersionBuilder.Build();
        }
        
        private static Mock<IDataTableBuilder<PublishedProviderVersion>> NewDataTableBuilder() => new Mock<IDataTableBuilder<PublishedProviderVersion>>();

        private static int NewRandomNumber() => new RandomNumberBetween(1, int.MaxValue);
        
        private static string NewRandomString() => new RandomString();
    }
}