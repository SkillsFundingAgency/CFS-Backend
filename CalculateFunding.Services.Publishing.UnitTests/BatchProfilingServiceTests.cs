using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Language;
using Polly;
using Serilog.Core;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class BatchProfilingServiceTests
    {
        private Mock<IProfilingApiClient> _profiling;
        private Mock<IBatchProfilingOptions> _options;
        private Mock<IBatchProfilingContext> _context;

        private BatchProfilingService _service;

        [TestInitialize]
        public void SetUp()
        {
            _profiling = new Mock<IProfilingApiClient>();
            _options = new Mock<IBatchProfilingOptions>();
            _context = new Mock<IBatchProfilingContext>();

            _options.Setup(_ => _.ConsumerCount)
                .Returns(1);

            _service = new BatchProfilingService(_profiling.Object,
                new ProducerConsumerFactory(),
                _options.Object,
                new ResiliencePolicies
                {
                    ProfilingApiClient = Policy.NoOpAsync()
                },
                Logger.None);
        }

        [TestMethod]
        public async Task ProfilesBatchesFromSuppliedContext()
        {
            BatchProfilingRequestModel requestOne = NewBatchProfilingRequestModel();
            BatchProfilingRequestModel requestTwo = NewBatchProfilingRequestModel();
            BatchProfilingRequestModel requestThree = NewBatchProfilingRequestModel();

            BatchProfilingResponseModel responseOne = NewBatchProfilingResponseModel();
            BatchProfilingResponseModel responseTwo = NewBatchProfilingResponseModel();
            BatchProfilingResponseModel responseThree = NewBatchProfilingResponseModel();
            BatchProfilingResponseModel responseFour = NewBatchProfilingResponseModel();
            BatchProfilingResponseModel responseFive = NewBatchProfilingResponseModel();
            BatchProfilingResponseModel responseSix = NewBatchProfilingResponseModel();

            int batchSize = new RandomNumberBetween(1, int.MaxValue);

            GivenThePagesOfRequests(requestOne, requestTwo, requestThree);
            AndTheBatchSize(batchSize);
            AndTheProfilingResponses(requestOne, responseOne, responseTwo);
            AndTheProfilingResponses(requestTwo, responseThree);
            AndTheProfilingResponses(requestThree, responseFour, responseFive, responseSix);

            await WhenTheBatchesAreProfiled();

            ThenTheContextItemsWereInitialisedWithABatchSize(batchSize);
            AndTheResponseWereReconciled(responseOne,
                responseTwo,
                responseThree,
                responseFour,
                responseFive,
                responseSix);
        }

        private async Task WhenTheBatchesAreProfiled()
            => await _service.ProfileBatches(_context.Object);

        private void AndTheBatchSize(int batchSize)
            => _options.Setup(_ => _.BatchSize)
                .Returns(batchSize);

        private void ThenTheContextItemsWereInitialisedWithABatchSize(int expectedBatchSize)
            => _context.Verify(_ => _.InitialiseItems(1, expectedBatchSize));

        private BatchProfilingRequestModel NewBatchProfilingRequestModel()
            => new BatchProfilingRequestModelBuilder().Build();

        private BatchProfilingResponseModel NewBatchProfilingResponseModel()
            => new BatchProfilingResponseModelBuilder().Build();

        private void GivenThePagesOfRequests(params BatchProfilingRequestModel[] requests)
        {
            ISetupSequentialResult<bool> hasPages = _context
                .SetupSequence(_ => _.HasPages);
            ISetupSequentialResult<BatchProfilingRequestModel[]> nextPage = _context
                .SetupSequence(_ => _.NextPage());

            foreach (BatchProfilingRequestModel request in requests)
            {
                hasPages = hasPages.Returns(true);
                nextPage = nextPage.Returns(new[]
                {
                    request
                });
            }
        }

        private void AndTheProfilingResponses(BatchProfilingRequestModel request,
            params BatchProfilingResponseModel[] responses)
            => _profiling.Setup(_ => _.GetBatchProfilePeriods(request))
                .ReturnsAsync(new ValidatedApiResponse<IEnumerable<BatchProfilingResponseModel>>(HttpStatusCode.OK, responses));

        private void AndTheResponseWereReconciled(params BatchProfilingResponseModel[] responses)
        {
            foreach (BatchProfilingResponseModel response in responses)
            {
                _context.Verify(_ => _.ReconcileBatchProfilingResponse(response),
                    Times.Once);
            }
        }
    }
}