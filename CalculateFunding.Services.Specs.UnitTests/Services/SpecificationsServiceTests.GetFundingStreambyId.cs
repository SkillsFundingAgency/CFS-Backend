using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    public partial class SpecificationsServiceTests
    {
        [TestMethod]
        public async Task GetFundingStreamById_GivenFundingStreamIdDoesNotExist_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.GetFundingStreamById(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No funding stream Id was provided to GetFundingStreamById"));
        }

        [TestMethod]
        public async Task GetFundingStreamById_GivenFundingStreamnWasNotFound_ReturnsNotFound()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "fundingStreamId", new StringValues(FundingStreamId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetFundingStreamById(Arg.Is(FundingStreamId))
                .Returns((FundingStream)null);

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetFundingStreamById(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Error(Arg.Is($"No funding stream was found for funding stream id : {FundingStreamId}"));
        }

        [TestMethod]
        public async Task GetFundingStreamById_GivenFundingStreamnWasFound_ReturnsSuccess()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "fundingStreamId", new StringValues(FundingStreamId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            FundingStream fundingStream = new FundingStream
            {
                Id = FundingStreamId
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetFundingStreamById(Arg.Is(FundingStreamId))
                .Returns(fundingStream);

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetFundingStreamById(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        public async Task GetFundingStreamById_StringParam_GivenFundingStreamIdDoesNotExist_ReturnsBadRequest()
        {
            //Arrange
            string fundingStreamId = string.Empty;

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.GetFundingStreamById(fundingStreamId);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No funding stream Id was provided to GetFundingStreamById"));
        }

        [TestMethod]
        public async Task GetFundingStreamById_StringParam_GivenFundingStreamnWasNotFound_ReturnsNotFound()
        {
            //Arrange
            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetFundingStreamById(Arg.Is(FundingStreamId))
                .Returns((FundingStream)null);

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetFundingStreamById(FundingStreamId);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Error(Arg.Is($"No funding stream was found for funding stream id : {FundingStreamId}"));
        }

        [TestMethod]
        public async Task GetFundingStreamById_StringParam_GivenFundingStreamnWasFound_ReturnsSuccess()
        {
            //Arrange
            ILogger logger = CreateLogger();

            FundingStream fundingStream = new FundingStream
            {
                Id = FundingStreamId
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetFundingStreamById(Arg.Is(FundingStreamId))
                .Returns(fundingStream);

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetFundingStreamById(FundingStreamId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        public async Task GetFundingStreams_GivenNullOrEmptyFundingStreamsReturned_LogsAndReturnsOKWithEmptyList()
        {
            //Arrange
            ILogger logger = CreateLogger();

            IEnumerable<FundingStream> fundingStreams = null;

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetFundingStreams()
                .Returns(fundingStreams);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.GetFundingStreams();

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult objectResult = result as OkObjectResult;

            IEnumerable<FundingStream> values = objectResult.Value as IEnumerable<FundingStream>;

            values
                .Should()
                .NotBeNull();

            logger
                .Received(1)
                .Error(Arg.Is("No funding streams were returned"));
        }

        [TestMethod]
        public async Task GetFundingStreams_GivenFundingStreamsReturned_ReturnsOKWithResults()
        {
            //Arrange
            ILogger logger = CreateLogger();

            IEnumerable<FundingStream> fundingStreams = new[]
            {
                new FundingStream(),
                new FundingStream()
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetFundingStreams()
                .Returns(fundingStreams);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.GetFundingStreams();

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult objectResult = result as OkObjectResult;

            IEnumerable<FundingStream> values = objectResult.Value as IEnumerable<FundingStream>;

            values
                .Count()
                .Should()
                .Be(2);
        }
    }
}
