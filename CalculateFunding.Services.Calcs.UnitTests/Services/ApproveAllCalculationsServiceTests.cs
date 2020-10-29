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

namespace CalculateFunding.Services.Calcs.UnitTests.Services
{
    [TestClass]
    public class ApproveAllCalculationsServiceTests
    {
        private Mock<ICalculationsRepository> _calculationsRepositoryMock;
        private Mock<ILogger> _logger;
        private Mock<IJobManagement> _jobManagementMock;

        private string _specificationId;
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
            _logger = new Mock<ILogger>();
            _jobManagementMock = new Mock<IJobManagement>();

            _specificationId = $"{NewRandomString()}_specificationId";
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
                NewCalculation(c => c.WithCurrentVersion(NewCalculationVersion(cv => cv.WithPublishStatus(PublishStatus.Draft)))),
                NewCalculation(c => c.WithCurrentVersion(NewCalculationVersion(cv => cv.WithPublishStatus(PublishStatus.Updated))))
            };

            _calculationsRepositoryMock
                .Setup(_ => _.GetCalculationsBySpecificationId(_specificationId))
                .ReturnsAsync(calculations);

            await approveAllCalculationsService.Process(_message);

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
