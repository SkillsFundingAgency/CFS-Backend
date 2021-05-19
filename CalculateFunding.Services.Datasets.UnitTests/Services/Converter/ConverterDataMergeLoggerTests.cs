using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Services.Datasets.Converter;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace CalculateFunding.Services.Datasets.Services.Converter
{
    [TestClass]
    public class ConverterDataMergeLoggerTests
    {
        private Mock<IDatasetRepository> datasets;

        private ConverterDataMergeLogger _logger;
        
        [TestInitialize]
        public void SetUp()
        {
            datasets = new Mock<IDatasetRepository>();

            _logger = new ConverterDataMergeLogger(datasets.Object,
                new DatasetsResiliencePolicies
                {
                    DatasetRepository = Policy.NoOpAsync()
                });
        }

        [TestMethod]
        public async Task CreatesNewLogEntryAndSavesToDatasetsRepository()
        {
            int expectedVersion = NewRandomVersion();
            IEnumerable<RowCopyResult> expectedResults = AsArray(NewRowCopyResult(),
                NewRowCopyResult(),
                NewRowCopyResult());
            ConverterMergeRequest expectedRequest = NewConverterMergeRequest();
            string expectedJobId = NewRandomString();

            await WhenTheOutcomeIsLogged(expectedResults,
                expectedRequest,
                expectedJobId,
                expectedVersion);
            
            datasets.Verify(_ => _.SaveConverterDataMergeLog(It.Is<ConverterDataMergeLog>(log
                => log.Results.SequenceEqual(expectedResults) &&
                   log.JobId.Equals(expectedJobId) &&
                   ReferenceEquals(log.Request, expectedRequest) &&
                   log.DatasetVersionCreated == expectedVersion)), Times.Once);
        }

        private async Task WhenTheOutcomeIsLogged(IEnumerable<RowCopyResult> results,
            ConverterMergeRequest request,
            string jobId,
            int versionCreated)
            => await _logger.SaveLogs(results, request, jobId, versionCreated);

        private int NewRandomVersion() => new RandomNumberBetween(1, int.MaxValue);
        
        private string NewRandomString() => new RandomString();

        private RowCopyResult NewRowCopyResult() => new RowCopyResultBuilder().Build();

        private ConverterMergeRequest NewConverterMergeRequest() => new ConverterMergeRequestBuilder().Build();

        private RowCopyResult[] AsArray(params RowCopyResult[] results) => results;
    }
}