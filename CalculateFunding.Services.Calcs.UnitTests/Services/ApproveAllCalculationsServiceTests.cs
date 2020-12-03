using CalculateFunding.Services.Calcs.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Tests.Common.Helpers;
using CalculateFunding.Services.Calcs.Services;
using System;
using Microsoft.Azure.ServiceBus;
using CalculateFunding.Tests.Common.Builders;
using System.Collections.Generic;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Results;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Common.Caching;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using System.Net;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Common.ApiClient.Results.Models;

namespace CalculateFunding.Services.Calcs.UnitTests.Services
{
    [TestClass]
    public class ApproveAllCalculationsServiceTests
    {
        private Mock<ICalculationsRepository> _calculationsRepositoryMock;
        private Mock<ISpecificationsApiClient> _specificationsApiClientMock;
        private Mock<IResultsApiClient> _resultsApiClientMock;
        private Mock<ISearchRepository<CalculationIndex>> _searchRepositoryMock;
        private Mock<ICacheProvider> _cacheProviderMock;

        private Mock<ILogger> _logger;
        private Mock<IJobManagement> _jobManagementMock;

        private string _specificationId;
        private string _fundingStreamId;
        private string _correlationId;
        private string _userId;

        private Message _message;

        private const string SpecificationId = "specification-id";
        private const string CorrelationId = "sfa-correlationId";
        private const string UserId = "user-id";

        [TestInitialize]
        public void Setup()
        {
            _calculationsRepositoryMock = new Mock<ICalculationsRepository>();
            _specificationsApiClientMock = new Mock<ISpecificationsApiClient>();
            _resultsApiClientMock = new Mock<IResultsApiClient>();
            _searchRepositoryMock = new Mock<ISearchRepository<CalculationIndex>>();
            _cacheProviderMock = new Mock<ICacheProvider>();
            _logger = new Mock<ILogger>();
            _jobManagementMock = new Mock<IJobManagement>();

            _specificationId = $"{NewRandomString()}_specificationId";
            _fundingStreamId = $"{NewRandomString()}_fundingStreamId";
            _correlationId = $"{NewRandomString()}_correlationId";
            _userId = $"{NewRandomString()}_userId";
        }

        [TestMethod]
        public async Task ApproveAllCalculations_CalcNotExistsOnGivenSpecification_CompletesJob()
        {
            ApproveAllCalculationsService approveAllCalculationsService = BuildApproveAllCalculationsService();

            GivenTheOtherwiseValidMessage();

            _calculationsRepositoryMock
                .Setup(_ => _.GetCalculationsBySpecificationId(_specificationId))
                .ReturnsAsync(Enumerable.Empty<Calculation>());

            await approveAllCalculationsService.Process(_message);
        }

        [TestMethod]
        public async Task ApproveAllCalculations_CalcExistsOnGivenSpecification_UpdateBulkPublishStatus()
        {
            ApproveAllCalculationsService approveAllCalculationsService = BuildApproveAllCalculationsService();

            GivenTheOtherwiseValidMessage();

            IEnumerable<Calculation> calculations = new List<Calculation> {
                NewCalculation(c => c.WithId("calc_1").WithFundingStreamId(_fundingStreamId).WithCurrentVersion(NewCalculationVersion(cv => cv.WithPublishStatus(PublishStatus.Draft)))),
                NewCalculation(c => c.WithId("calc_2").WithFundingStreamId(_fundingStreamId).WithCurrentVersion(NewCalculationVersion(cv => cv.WithPublishStatus(PublishStatus.Updated))))
            };

            _calculationsRepositoryMock
                .Setup(_ => _.GetCalculationsBySpecificationId(_specificationId))
                .ReturnsAsync(calculations);

            SpecModel.SpecificationSummary specificationSummary = new SpecModel.SpecificationSummary()
            {
                Id = _specificationId,
                Name = "spec name",
                FundingStreams = new[]
                {
                    new Reference(_fundingStreamId, "funding stream name")
                }
            };

            _specificationsApiClientMock
                .Setup(_ => _.GetSpecificationSummaryById(_specificationId))
                .ReturnsAsync(new Common.ApiClient.Models.ApiResponse<SpecModel.SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

            await approveAllCalculationsService.Process(_message);

            _cacheProviderMock.Verify(_ => _.KeyDeleteAsync<List<CalculationSummaryModel>>($"{CacheKeys.CalculationsSummariesForSpecification}{_specificationId}")
                ,Times.Once);

            _cacheProviderMock.Verify(_ => _.KeyDeleteAsync<List<CalculationResponseModel>>($"{CacheKeys.CurrentCalculationsForSpecification}{_specificationId}")
                , Times.Once);

            _cacheProviderMock.Verify(_ => _.KeyDeleteAsync<List<CalculationResponseModel>>($"{CacheKeys.CalculationsMetadataForSpecification}{_specificationId}")
                , Times.Once);

            _cacheProviderMock.Verify(_ => _.SetAsync($"{CacheKeys.CurrentCalculation}calc_1", It.IsAny<CalculationResponseModel>(), TimeSpan.FromDays(7), true, null)
                , Times.Once);

            _cacheProviderMock.Verify(_ => _.SetAsync($"{CacheKeys.CurrentCalculation}calc_2", It.IsAny<CalculationResponseModel>(), TimeSpan.FromDays(7), true, null)
                , Times.Once);

            _resultsApiClientMock.Verify(_ => _.UpdateFundingStructureLastModified(It.IsAny<UpdateFundingStructureLastModifiedRequest>()));

            _searchRepositoryMock.Verify(_ => _.Index(It.Is<IEnumerable<CalculationIndex>>(c =>
                c.FirstOrDefault() != null && c.FirstOrDefault().Status == PublishStatus.Approved.ToString() &&
                c.LastOrDefault() != null && c.LastOrDefault().Status == PublishStatus.Approved.ToString()))
                ,Times.Once);

            _calculationsRepositoryMock
                .Verify(_ => _.UpdateCalculations(
                    It.Is<IEnumerable<Calculation>>(c =>
                        c.FirstOrDefault() != null && c.FirstOrDefault().Current.PublishStatus == PublishStatus.Approved &&
                        c.LastOrDefault() != null && c.LastOrDefault().Current.PublishStatus == PublishStatus.Approved)), Times.Once);
        }

        private void GivenTheOtherwiseValidMessage(Action<MessageBuilder> overrides = null)
        {
            MessageBuilder messageBuilder = new MessageBuilder()
                .WithUserProperty(SpecificationId, _specificationId)
                .WithUserProperty(CorrelationId, _correlationId)
                .WithUserProperty(UserId, _userId);

            overrides?.Invoke(messageBuilder);

            _message = messageBuilder.Build();
        }

        private ApproveAllCalculationsService BuildApproveAllCalculationsService()
        {
            return new ApproveAllCalculationsService(
                _calculationsRepositoryMock.Object,
                CalcsResilienceTestHelper.GenerateTestPolicies(),
                _specificationsApiClientMock.Object,
                _resultsApiClientMock.Object,
                _searchRepositoryMock.Object,
                _cacheProviderMock.Object,
                _logger.Object,
                _jobManagementMock.Object);
        }

        protected string NewRandomString() => new RandomString();

        protected static Calculation NewCalculation(Action<CalculationBuilder> setUp = null)
        {
            CalculationBuilder calculationBuilder = new CalculationBuilder();

            setUp?.Invoke(calculationBuilder);

            return calculationBuilder.Build();
        }
        protected static CalculationVersion NewCalculationVersion(Action<CalculationVersionBuilder> setUp = null)
        {
            CalculationVersionBuilder calculationVersionBuilder = new CalculationVersionBuilder();

            setUp?.Invoke(calculationVersionBuilder);

            return calculationVersionBuilder.Build();
        }

    }
}
