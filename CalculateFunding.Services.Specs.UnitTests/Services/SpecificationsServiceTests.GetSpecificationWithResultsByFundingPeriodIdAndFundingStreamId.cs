using AutoMapper;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    public partial class SpecificationsServiceTests
    {
        [TestMethod]
        public async Task GetSpecificationWithResultsByFundingPeriodIdAndFundingStreamId_GivenFundingPeriodIdDoesNotExist_ReturnsBadRequest()
        {
            //Arrange
            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.GetSpecificationWithResultsByFundingPeriodIdAndFundingStreamId(null, null);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Any<string>());
        }

        [TestMethod]
        public async Task GetSpecificationWithResultsByFundingPeriodIdAndFundingStreamId_GivenFundingStreamIdDoesNotExist_ReturnsBadRequest()
        {
            //Arrange
            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.GetSpecificationWithResultsByFundingPeriodIdAndFundingStreamId(NewRandomString(), null);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Any<string>());
        }

        [TestMethod]
        public async Task GetSpecificationWithResultsByFundingPeriodIdAndFundingStreamId_GivenResultsReturnedButOnlyOneHasProviderResults_ReturnsOKObjectWithOneSummary()
        {
            //Arrange
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string specificationId1 = NewRandomString();
            string specificationId2 = NewRandomString();

            IMapper mapper = CreateImplementedMapper();

            IEnumerable<Specification> specifications = NewSpecifications(
                _ => _.WithId(specificationId1),
                _ => _.WithId(specificationId2));

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationsByFundingPeriodAndFundingStream(Arg.Is(FundingPeriodId), Arg.Is(FundingStreamId))
                .Returns(specifications);

            IResultsRepository resultsRepository = CreateResultsRepository();
            resultsRepository
                .SpecificationHasResults(Arg.Is(specificationId1))
                .Returns(true);

            resultsRepository
                .SpecificationHasResults(Arg.Is(specificationId2))
                .Returns(false);

            SpecificationsService service = CreateService(
                mapper: mapper,
                logs: logger,
                specificationsRepository: specificationsRepository,
                resultsRepository: resultsRepository);

            //Act
            IActionResult result = await service.GetSpecificationWithResultsByFundingPeriodIdAndFundingStreamId(FundingPeriodId, FundingStreamId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okObjectResult = result as OkObjectResult;

            IEnumerable<SpecificationSummary> summaries = okObjectResult.Value as IEnumerable<SpecificationSummary>;

            summaries
                .Count()
                .Should()
                .Be(1);
        }
    }
}
