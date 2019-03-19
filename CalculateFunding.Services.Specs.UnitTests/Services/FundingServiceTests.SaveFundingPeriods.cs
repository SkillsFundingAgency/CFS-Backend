using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Specs.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    public partial class FundingServiceTests
    {
        [TestMethod]
        async public Task SaveFundingPeriods_GivenNoYamlWasProvidedWithNoFileName_ReturnsBadRequest()
        {
            // Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            IFundingService fundingService = CreateService(logger: logger);

            // Act
            IActionResult result = await fundingService.SaveFundingPeriods(request);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is($"Null or empty yaml provided for file: File name not provided"));
        }

        [TestMethod]
        async public Task SaveFundingPeriods_GivenNoYamlWasProvidedButFileNameWas_ReturnsBadRequest()
        {
            // Arrange
            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary
                .Add("yaml-file", new StringValues(yamlFile));

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Headers
                .Returns(headerDictionary);

            ILogger logger = CreateLogger();

            IFundingService fundingService = CreateService(logger: logger);

            // Act
            IActionResult result = await fundingService.SaveFundingPeriods(request);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is($"Null or empty yaml provided for file: {yamlFile}"));
        }

        [TestMethod]
        async public Task SaveFundingPeriods_GivenNoYamlWasProvidedButIsInvalid_ReturnsBadRequest()
        {
            // Arrange
            string yaml = "invalid yaml";
            byte[] byteArray = Encoding.UTF8.GetBytes(yaml);
            MemoryStream stream = new MemoryStream(byteArray);

            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary
                .Add("yaml-file", new StringValues(yamlFile));

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Headers
                .Returns(headerDictionary);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            IFundingService fundingService = CreateService(logger: logger);

            // Act
            IActionResult result = await fundingService.SaveFundingPeriods(request);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is($"Invalid yaml was provided for file: {yamlFile}"));
        }

        [TestMethod]
        async public Task SaveFundingPeriods_GivenValidYamlButSavingToDatabaseThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            string yaml = CreateRawFundingPeriods();
            byte[] byteArray = Encoding.UTF8.GetBytes(yaml);
            MemoryStream stream = new MemoryStream(byteArray);

            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary
                .Add("yaml-file", new StringValues(yamlFile));

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Headers
                .Returns(headerDictionary);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .When(x => x.SavePeriods(Arg.Any<Period[]>()))
                .Do(x => { throw new Exception(); });

            IFundingService fundingService = CreateService(logger: logger, specificationsRepository: specificationsRepository);

            // Act
            IActionResult result = await fundingService.SaveFundingPeriods(request);

            // Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = (StatusCodeResult)result;
            statusCodeResult
                .StatusCode
                .Should()
                .Be(500);

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is($"Exception occurred writing to yaml file: {yamlFile} to cosmos db"));
        }

        [TestMethod]
        async public Task SaveFundingPeriods_GivenValidYamlAndSaveWasSuccesful_ReturnsOK()
        {
            // Arrange
            string yaml = CreateRawFundingPeriods();
            byte[] byteArray = Encoding.UTF8.GetBytes(yaml);
            MemoryStream stream = new MemoryStream(byteArray);

            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary
                .Add("yaml-file", new StringValues(yamlFile));

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Headers
                .Returns(headerDictionary);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();

            IFundingService fundingService = CreateService(logger: logger, specificationsRepository: specificationsRepository, cacheProvider: cacheProvider);

            // Act
            IActionResult result = await fundingService.SaveFundingPeriods(request);

            // Assert
            result
                .Should()
                .BeOfType<OkResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"Successfully saved file: {yamlFile} to cosmos db"));

            await
                cacheProvider
                .Received(1)
                .SetAsync<Period[]>(Arg.Is(CacheKeys.FundingPeriods), Arg.Any<Period[]>(), Arg.Any<TimeSpan>(), Arg.Is(true));
        }

        protected string CreateRawFundingPeriods()
        {
            StringBuilder yaml = new StringBuilder();

            yaml.AppendLine(@"fundingPeriods:");
            yaml.AppendLine(@"- id: AY2017181");
            yaml.AppendLine(@"  name: Academic 2017/18");
            yaml.AppendLine(@"  startDate: 09/01/2017 00:00:00");
            yaml.AppendLine(@"  endDate: 08/31/2018 00:00:00");
            yaml.AppendLine(@"- id: AY2018191");
            yaml.AppendLine(@"  name: Academic 2018/19");
            yaml.AppendLine(@"  startDate: 09/01/2018 00:00:00");
            yaml.AppendLine(@"  endDate: 08/31/2019 00:00:00");
            yaml.AppendLine(@"- id: FY2017181");
            yaml.AppendLine(@"  name: Financial 2017/18");
            yaml.AppendLine(@"  startDate: 04/01/2017 00:00:00");
            yaml.AppendLine(@"  endDate: 03/31/2018 00:00:00");
            yaml.AppendLine(@"- id: AY2018191");
            yaml.AppendLine(@"  name: Financial 2018/19");
            yaml.AppendLine(@"  startDate: 04/01/2018 00:00:00");
            yaml.AppendLine(@"  endDate: 03/31/2019 00:00:00");

            return yaml.ToString();
        }
    }
}
