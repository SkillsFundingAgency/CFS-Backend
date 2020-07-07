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
        public async Task GetApprovedSpecificationsByFundingPeriodIdAndFundingStreamId_GivenFundingPeriodIdDoesNotExist_ReturnsBadRequest()
        {
            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            IActionResult result = await service.GetApprovedSpecificationsByFundingPeriodIdAndFundingStreamId(null, null);

            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Any<string>());
        }

        [TestMethod]
        public async Task GetApprovedSpecificationsByFundingPeriodIdAndFundingStreamId_GivenFundingStreamIdDoesNotExist_ReturnsBadRequest()
        {
            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            IActionResult result = await service.GetApprovedSpecificationsByFundingPeriodIdAndFundingStreamId(NewRandomString(), null);

            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Any<string>());
        }

        [TestMethod]
        public async Task GetApprovedSpecificationsByFundingPeriodIdAndFundingStreamId_GivenFundingStreamIdAndFundingPeriodId_ReturnsSpecificationSummary()
        {
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string specificationId = NewRandomString();

            IMapper mapper = CreateImplementedMapper();
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();

            IEnumerable<Specification> specifications = NewSpecifications(_ => _.WithId(specificationId));

            specificationsRepository
                .GetApprovedOrUpdatedSpecificationsByFundingPeriodAndFundingStream(fundingPeriodId, fundingStreamId)
                .Returns(specifications);

            SpecificationsService service = CreateService(
                mapper: mapper,
                specificationsRepository: specificationsRepository);

            IActionResult result = await service.GetApprovedSpecificationsByFundingPeriodIdAndFundingStreamId(fundingPeriodId, fundingStreamId);

            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<List<SpecificationSummary>>()
                .And
                .NotBeNull();

            List<SpecificationSummary> objContent = (List<SpecificationSummary>)((OkObjectResult)result).Value;
            SpecificationSummary specificationSummary = objContent.FirstOrDefault();

            specificationSummary
                .Should()
                .NotBeNull();

            specificationSummary
                .Id
                .Should()
                .Be(specificationId);
        }
    }
}
