using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests.Services
{
    [TestClass]
    public class SpecificationFundingStatusServiceTests
    {
        private const string specificationId = "spec-id";
        private const string fundingPeriodId = "fp-id";

        [TestMethod]
        public void CheckChooseForFundingStatus_GivenSpecificationSummaryNotFound_ThrowsEntityNotFoundException()
        {
            //Arrange
            SpecificationSummary specificationSummary = null;

            string errorMessage = $"Failed to find specification with for specification Id '{specificationId}'";

            ILogger logger = CreateLogger();

            ISpecificationService specificationService = CreateSpecificationService();
            specificationService
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specificationSummary);

            SpecificationFundingStatusService specificationFundingStatusService = CreateSpecificationFundingStatusService(logger, specificationService);

            //Act
            Func<Task> test = async () => await specificationFundingStatusService.CheckChooseForFundingStatus(specificationId);

            //Assert
            test
                .Should()
                .ThrowExactly<EntityNotFoundException>()
                .Which
                .Message
                .Should()
                .Be(errorMessage);

            logger
                .Received(1)
                .Error(Arg.Is(errorMessage));
        }

        [TestMethod]
        public async Task CheckChooseForFundingStatus_GivenSpecificationSummaryButAlreadyChosen_ReturnsAlreadyChosen()
        {
            //Arrange
            SpecificationSummary specificationSummary = new SpecificationSummary
            {
                IsSelectedForFunding = true
            };

            ISpecificationService specificationService = CreateSpecificationService();
            specificationService
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specificationSummary);

            SpecificationFundingStatusService specificationFundingStatusService = CreateSpecificationFundingStatusService(specificationService: specificationService);

            //Act
            SpecificationFundingStatus status = await specificationFundingStatusService.CheckChooseForFundingStatus(specificationId);

            //Assert
            status
                .Should()
                .Be(SpecificationFundingStatus.AlreadyChosen);
        }

        [TestMethod]
        public async Task CheckChooseForFundingStatus_GivenNoSpecSummariesFoundForFundingPeriodId_ReturnsCanChoose()
        {
            //Arrange
            SpecificationSummary specificationSummary = new SpecificationSummary
            {
                FundingPeriod = new Reference
                {
                    Id = fundingPeriodId
                }
            };

            ISpecificationService specificationService = CreateSpecificationService();

            specificationService
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specificationSummary);

            specificationService
                .GetSpecificationsSelectedForFundingByPeriod(Arg.Is(fundingPeriodId))
                .Returns((IEnumerable<SpecificationSummary>)null);

            SpecificationFundingStatusService specificationFundingStatusService = CreateSpecificationFundingStatusService(specificationService: specificationService);

            //Act
            SpecificationFundingStatus status = await specificationFundingStatusService.CheckChooseForFundingStatus(specificationId);

            //Assert
            status
                .Should()
                .Be(SpecificationFundingStatus.CanChoose);
        }

        [TestMethod]
        public async Task CheckChooseForFundingStatus_GivenSpecsReturnedForFundingPeriodWithChosenFundingStreams_ReturnsSharesAlreadyChoseFundingStream()
        {
            //Arrange
            SpecificationSummary specificationSummary = new SpecificationSummary
            {
                FundingPeriod = new Reference
                {
                    Id = fundingPeriodId
                },
                FundingStreams = new[]
                {
                    new Reference("fs-2", "fs2")
                }
            };

            IEnumerable<SpecificationSummary> specificationSummaries = new[]
            {
                new SpecificationSummary
                {
                    FundingStreams = new[]
                    {
                        new Reference("fs-1", "fs1"),
                        new Reference("fs-2", "fs2"),
                    }
                },
                new SpecificationSummary
                {
                    FundingStreams = new[]
                    {
                        new Reference("fs-3", "fs3"),
                        new Reference("fs-4", "fs4"),
                    }
                }
            };

            ISpecificationService specificationService = CreateSpecificationService();

            specificationService
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specificationSummary);

            specificationService
                .GetSpecificationsSelectedForFundingByPeriod(Arg.Is(fundingPeriodId))
                .Returns(specificationSummaries);

            SpecificationFundingStatusService specificationFundingStatusService = CreateSpecificationFundingStatusService(specificationService: specificationService);

            //Act
            SpecificationFundingStatus status = await specificationFundingStatusService.CheckChooseForFundingStatus(specificationId);

            //Assert
            status
                .Should()
                .Be(SpecificationFundingStatus.SharesAlreadyChosenFundingStream);
        }

        [TestMethod]
        public async Task CheckChooseForFundingStatus_GivenSpecsReturnedForFundingPeriodWithDiffrentFundingStreams_ReturnsCanChoose()
        {
            //Arrange
            SpecificationSummary specificationSummary = new SpecificationSummary
            {
                FundingPeriod = new Reference
                {
                    Id = fundingPeriodId
                },
                FundingStreams = new[]
                {
                    new Reference("fs-2", "fs2")
                }
            };

            IEnumerable<SpecificationSummary> specificationSummaries = new[]
            {
                new SpecificationSummary
                {
                    FundingStreams = new[]
                    {
                        new Reference("fs-1", "fs1"),
                        new Reference("fs-5", "fs5"),
                    }
                },
                new SpecificationSummary
                {
                    FundingStreams = new[]
                    {
                        new Reference("fs-3", "fs3"),
                        new Reference("fs-4", "fs4"),
                    }
                }
            };

            ISpecificationService specificationService = CreateSpecificationService();

            specificationService
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specificationSummary);

            specificationService
                .GetSpecificationsSelectedForFundingByPeriod(Arg.Is(fundingPeriodId))
                .Returns(specificationSummaries);

            SpecificationFundingStatusService specificationFundingStatusService = CreateSpecificationFundingStatusService(specificationService: specificationService);

            //Act
            SpecificationFundingStatus status = await specificationFundingStatusService.CheckChooseForFundingStatus(specificationId);

            //Assert
            status
                .Should()
                .Be(SpecificationFundingStatus.CanChoose);
        }

        [TestMethod]
        public async Task CheckChooseForFundingStatus_GivenSpecWIthMultipleFundingStreamsAndSpecsReturnedForFundingPeriodWithSameFundingStream_ReturnsSharesAlreadyChoseFundingStream()
        {
            //Arrange
            SpecificationSummary specificationSummary = new SpecificationSummary
            {
                FundingPeriod = new Reference
                {
                    Id = fundingPeriodId
                },
                FundingStreams = new[]
                {
                    new Reference("fs-2", "fs2"),
                    new Reference("fs-4", "fs4")
                }
            };

            IEnumerable<SpecificationSummary> specificationSummaries = new[]
            {
                new SpecificationSummary
                {
                    FundingStreams = new[]
                    {
                        new Reference("fs-1", "fs1"),
                        new Reference("fs-5", "fs5"),
                    }
                },
                new SpecificationSummary
                {
                    FundingStreams = new[]
                    {
                        new Reference("fs-3", "fs3"),
                        new Reference("fs-4", "fs4"),
                    }
                }
            };

            ISpecificationService specificationService = CreateSpecificationService();

            specificationService
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specificationSummary);

            specificationService
                .GetSpecificationsSelectedForFundingByPeriod(Arg.Is(fundingPeriodId))
                .Returns(specificationSummaries);

            SpecificationFundingStatusService specificationFundingStatusService = CreateSpecificationFundingStatusService(specificationService: specificationService);

            //Act
            SpecificationFundingStatus status = await specificationFundingStatusService.CheckChooseForFundingStatus(specificationId);

            //Assert
            status
                .Should()
                .Be(SpecificationFundingStatus.SharesAlreadyChosenFundingStream);
        }

        private static SpecificationFundingStatusService CreateSpecificationFundingStatusService(
            ILogger logger = null,
            ISpecificationService specificationService = null)
        {
            return new SpecificationFundingStatusService(
                logger ?? CreateLogger(),
                specificationService ?? CreateSpecificationService());
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static ISpecificationService CreateSpecificationService()
        {
            return Substitute.For<ISpecificationService>();
        }
    }
}
