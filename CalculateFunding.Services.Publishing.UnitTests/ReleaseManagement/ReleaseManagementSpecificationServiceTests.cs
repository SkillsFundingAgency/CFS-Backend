using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests.ReleaseManagement
{
    [TestClass]
    public class ReleaseManagementSpecificationServiceTests
    {
        private RandomString _specificationId;
        private RandomString _fundingPeriod;
        private RandomString _fundingStreamId;
        private RandomString _specificationName;
        private int _sqlFundingStreamId;
        private int _sqlFundingPeriodId;
        private FundingPeriod _sqlFundingPeriod;
        private FundingStream _sqlFundingStream;
        private Specification _sqlSpecification;
        private SpecificationSummary _specification;
        private Mock<IReleaseToChannelSqlMappingContext> _releaseToChannelSqlMappingContext;
        private Mock<IReleaseManagementRepository> _releaseManagementRepository;
        private Mock<IPoliciesApiClient> _policiesApiClient;
        private Mock<ILogger> _logger;
        private ReleaseManagementSpecificationService _service;

        [TestInitialize]
        public void Initialise()
        {
            _specificationId = new RandomString();
            _fundingPeriod = new RandomString();
            _fundingStreamId = new RandomString();
            _specificationName = new RandomString();
            _sqlFundingStreamId = 1;
            _sqlFundingPeriodId = 2;
            _sqlFundingPeriod = new FundingPeriod
            {
                FundingPeriodId = _sqlFundingPeriodId,
                FundingPeriodCode = new RandomString(),
                FundingPeriodName = new RandomString()
            };
            _sqlFundingStream = new FundingStream
            {
                FundingStreamId = _sqlFundingStreamId,
                FundingStreamCode = new RandomString(),
                FundingStreamName = new RandomString()
            };
            _sqlSpecification = new Specification
            {
                SpecificationId = _specificationId,
                SpecificationName = new RandomString(),
                FundingStreamId = _sqlFundingStreamId,
                FundingPeriodId = _sqlFundingPeriodId
            };

            _releaseToChannelSqlMappingContext = new Mock<IReleaseToChannelSqlMappingContext>();
            _releaseManagementRepository = new Mock<IReleaseManagementRepository>();
            _policiesApiClient = new Mock<IPoliciesApiClient>();
            _logger = new Mock<ILogger>();
            _service = new ReleaseManagementSpecificationService(_releaseManagementRepository.Object,
                _releaseToChannelSqlMappingContext.Object,
                _policiesApiClient.Object,
                PublishingResilienceTestHelper.GenerateTestPolicies(),
                _logger.Object);
        }

        [TestMethod]
        public async Task AddsSpecificationToContext()
        {
            GivenSpecification();
            GivenFundingPeriod();
            GivenFundingStream();
            GivenSqlSpecification();

            await _service.EnsureReleaseManagementSpecification(_specification);

            _releaseToChannelSqlMappingContext.VerifySet(s => s.Specification = _sqlSpecification);
        }

        [TestMethod]
        public async Task CreatesFundingPeriodIfNotExist()
        {
            GivenSpecification();
            GivenNoFundingPeriod();
            GivenFundingStream();
            GivenSqlSpecification();

            await _service.EnsureReleaseManagementSpecification(_specification);

            _releaseManagementRepository.Verify(r => r.CreateFundingPeriodUsingAmbientTransaction(
               It.Is<FundingPeriod>(s => s.FundingPeriodCode == _fundingPeriod)), Times.Once);
        }

        private void GivenNoFundingPeriod()
        {
            _releaseManagementRepository.Setup(r => r.GetFundingPeriodByCode(It.IsAny<string>()))
                            .ReturnsAsync((FundingPeriod)null);
            _policiesApiClient.Setup(p => p.GetFundingPeriods())
                .ReturnsAsync(new ApiResponse<IEnumerable<Common.ApiClient.Policies.Models.FundingPeriod>>(
                    HttpStatusCode.OK, new List<Common.ApiClient.Policies.Models.FundingPeriod>
                    {
                        new Common.ApiClient.Policies.Models.FundingPeriod
                        {
                            Id = _fundingPeriod
                        }
                    }));
        }

        [TestMethod]
        public async Task CreatesFundingStreamIfNotExist()
        {
            GivenSpecification();
            GivenFundingPeriod();
            GivenNoFundingStream();
            GivenSqlSpecification();

            await _service.EnsureReleaseManagementSpecification(_specification);

            _releaseManagementRepository.Verify(r => r.CreateFundingStreamUsingAmbientTransaction(
               It.Is<FundingStream>(s => s.FundingStreamCode == _fundingStreamId)), Times.Once);
        }

        [TestMethod]
        public async Task CreatesSpecificationIfNotExist()
        {
            GivenSpecification();
            GivenFundingPeriod();
            GivenFundingStream();
            GivenNoSqlSpecification();

            await _service.EnsureReleaseManagementSpecification(_specification);

            _releaseManagementRepository.Verify(r => r.CreateSpecificationUsingAmbientTransaction(
                It.Is<Specification>(s => s.SpecificationId == _specificationId)), Times.Once);
        }

        [TestMethod]
        public async Task UpdatesSpecificationNameIfDifferent()
        {
            GivenSpecification();
            GivenFundingPeriod();
            GivenFundingStream();
            GivenOldSqlSpecification();

            await _service.EnsureReleaseManagementSpecification(_specification);

            _releaseManagementRepository.Verify(r => r.UpdateSpecificationUsingAmbientTransaction(
                It.Is<Specification>(s => s.SpecificationId == _specificationId)), Times.Once);
        }

        private void GivenSpecification()
        {
            _specification = new SpecificationSummaryBuilder()
                            .WithFundingPeriodId(_fundingPeriod)
                            .WithFundingStreamIds(new string[] { _fundingStreamId })
                            .WithId(_specificationId)
                            .WithName(_specificationName)
                            .Build();
        }

        private void GivenSqlSpecification()
        {
            _releaseManagementRepository.Setup(r => r.GetSpecificationById(It.IsAny<string>()))
                            .ReturnsAsync(_sqlSpecification);
        }

        private void GivenNoSqlSpecification()
        {
            _releaseManagementRepository.Setup(r => r.GetSpecificationById(It.IsAny<string>()))
                            .ReturnsAsync((Specification)null);
        }

        private void GivenOldSqlSpecification()
        {
            Specification specification = new Specification
            {
                SpecificationId = _sqlSpecification.SpecificationId,
                SpecificationName = new RandomString(),
                FundingPeriodId = _sqlSpecification.FundingPeriodId,
                FundingStreamId = _sqlSpecification.FundingStreamId
            };
            _releaseManagementRepository.Setup(r => r.GetSpecificationById(It.IsAny<string>()))
                .ReturnsAsync(specification);
        }

        private void GivenFundingStream()
        {
            _releaseManagementRepository.Setup(r => r.GetFundingStreamByCode(It.IsAny<string>()))
                            .ReturnsAsync(_sqlFundingStream);
        }

        private void GivenFundingPeriod()
        {
            _releaseManagementRepository.Setup(r => r.GetFundingPeriodByCode(It.IsAny<string>()))
                            .ReturnsAsync(_sqlFundingPeriod);
        }

        private void GivenNoFundingStream()
        {
            _releaseManagementRepository.Setup(r => r.GetFundingStreamByCode(It.IsAny<string>()))
                            .ReturnsAsync((FundingStream)null);
            _policiesApiClient.Setup(p => p.GetFundingStreams())
                .ReturnsAsync(new ApiResponse<IEnumerable<Common.ApiClient.Policies.Models.FundingStream>>(
                    HttpStatusCode.OK, new List<Common.ApiClient.Policies.Models.FundingStream>
                    {
                        new Common.ApiClient.Policies.Models.FundingStream
                        {
                            Id = _fundingStreamId
                        }
                    }));
        }
    }
}
