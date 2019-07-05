using CalculateFunding.Common.Caching;
using CalculateFunding.Models.Policy;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Policy.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Policy.UnitTests
{
    [TestClass]
    public class FundingPeriodServiceTests
    {
        private const string yamlFile = "12345.yaml";

        [TestMethod]
        public async Task GetFundingPeriods_GivenNullOrEmptyPeriodsReturned_LogsAndReturnsOKWithEmptyList()
        {
            // Arrange
            ILogger logger = CreateLogger();

            IEnumerable<Period> Periods = null;

            IPolicyRepository policyRepository = CreatePolicyRepository();
            policyRepository
                .GetFundingPeriods()
                .Returns(Periods);

            FundingPeriodService fundingPeriodService = CreateFundingPeriodService(logger: logger, policyRepository: policyRepository);

            // Act
            IActionResult result = await fundingPeriodService.GetFundingPeriods();

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult objectResult = result as OkObjectResult;

            IEnumerable<Period> values = objectResult.Value as IEnumerable<Period>;

            values
                .Should()
                .NotBeNull();

            logger
                .Received(1)
                .Error(Arg.Is("No funding periods were returned"));
        }

        [TestMethod]
        public async Task GetFundingPeriods_GivenPeriodsAlreadyInCache_ReturnsOKWithResultsFromCache()
        {
            // Arrange
            ILogger logger = CreateLogger();

            IEnumerable<Period> Periods = new[]
            {
                new Period(),
                new Period()
            };

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<Period[]>(Arg.Is(CacheKeys.FundingPeriods))
                .Returns(Periods.ToArray());

            IPolicyRepository policyRepository = CreatePolicyRepository();

            FundingPeriodService PeriodsService = CreateFundingPeriodService(logger, cacheProvider, policyRepository);

            // Act
            IActionResult result = await PeriodsService.GetFundingPeriods();

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult objectResult = result as OkObjectResult;

            IEnumerable<Period> values = objectResult.Value as IEnumerable<Period>;

            values
                .Should()
                .HaveCount(2);

            await
            policyRepository
                .DidNotReceive()
                .GetFundingPeriods();
        }

        [TestMethod]
        public async Task GetFundingPeriods_GivenPeriodsReturned_ReturnsOKWithResults()
        {
            // Arrange
            ILogger logger = CreateLogger();

            IEnumerable<Period> Periods = new[]
            {
                new Period(),
                new Period()
            };

            IPolicyRepository policyRepository = CreatePolicyRepository();
            policyRepository
                .GetFundingPeriods()
                .Returns(Periods);

            ICacheProvider cacheProvider = CreateCacheProvider();

            FundingPeriodService fundingPeriodService = CreateFundingPeriodService(logger, cacheProvider, policyRepository);

            // Act
            IActionResult result = await fundingPeriodService.GetFundingPeriods();

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult objectResult = result as OkObjectResult;

            IEnumerable<Period> values = objectResult.Value as IEnumerable<Period>;

            values
                .Should()
                .HaveCount(2);

            await
                cacheProvider
                    .Received(1)
                    .SetAsync<Period[]>(Arg.Is(CacheKeys.FundingPeriods), Arg.Is<Period[]>(m => m.SequenceEqual(Periods)));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public async Task GetFundingPeriodById_GivenFundingStreamIdDoesNotExist_ReturnsBadRequest(string fundingStreamId)
        {
            // Arrange
            ILogger logger = CreateLogger();

            FundingPeriodService fundingPeriodService = CreateFundingPeriodService(logger: logger);

            // Act
            IActionResult result = await fundingPeriodService.GetFundingPeriodById(fundingStreamId);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty funding period id provided");

            logger
                .Received(1)
                .Error(Arg.Is("No funding period id was provided to GetFundingPeriodById"));
        }

        [TestMethod]
        public async Task GetFundingPeriodById_GivenFundingPeriodWasNotFound_ReturnsNotFound()
        {
            // Arrange
            const string fundingPeriodId = "fp-1";

            ILogger logger = CreateLogger();

            IPolicyRepository policyRepository = CreatePolicyRepository();
            policyRepository
                .GetFundingPeriodById(Arg.Is(fundingPeriodId))
                .Returns((Period)null);

            FundingPeriodService fundingPeriodService = CreateFundingPeriodService(logger: logger, policyRepository: policyRepository);

            // Act
            IActionResult result = await fundingPeriodService.GetFundingPeriodById(fundingPeriodId);

            // Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Error(Arg.Is($"No funding period was returned for funding period id: '{fundingPeriodId}'"));
        }

        [TestMethod]
        public async Task GetFundingPeriodById__GivenFundingStreamnWasFound_ReturnsSuccess()
        {
            // Arrange
            const string fundingPeriodId = "fp-1";

            Period fundingPeriod = new Period
            {
                Id = fundingPeriodId
            };

            IPolicyRepository policyRepository = CreatePolicyRepository();
            policyRepository
                .GetFundingPeriodById(Arg.Is(fundingPeriodId))
                .Returns(fundingPeriod);

            FundingPeriodService fundingPeriodService = CreateFundingPeriodService(policyRepository: policyRepository);

            // Act
            IActionResult result = await fundingPeriodService.GetFundingPeriodById(fundingPeriodId);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(fundingPeriod);
        }

        [TestMethod]
        async public Task SaveFundingPeriod_GivenNoYamlWasProvidedWithNoFileName_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            FundingPeriodService fundingPeriodService = CreateFundingPeriodService(logger: logger);

            //Act
            IActionResult result = await fundingPeriodService.SaveFundingPeriods(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is($"Null or empty yaml provided for file: File name not provided"));
        }

        [TestMethod]
        async public Task SaveFundingPeriod_GivenNoYamlWasProvidedButFileNameWas_ReturnsBadRequest()
        {
            //Arrange
            IHeaderDictionary headerDictionary = new HeaderDictionary
            {
                { "yaml-file", new StringValues(yamlFile) }
            };

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Headers
                .Returns(headerDictionary);

            ILogger logger = CreateLogger();

            FundingPeriodService fundingPeriodService = CreateFundingPeriodService(logger: logger);

            //Act
            IActionResult result = await fundingPeriodService.SaveFundingPeriods(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is($"Null or empty yaml provided for file: {yamlFile}"));
        }

        [TestMethod]
        async public Task SaveFundingPeriod_GivenNoYamlWasProvidedButIsInvalid_ReturnsBadRequest()
        {
            //Arrange
            string yaml = "invalid yaml";
            byte[] byteArray = Encoding.UTF8.GetBytes(yaml);
            MemoryStream stream = new MemoryStream(byteArray);

            IHeaderDictionary headerDictionary = new HeaderDictionary
            {
                { "yaml-file", new StringValues(yamlFile) }
            };

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Headers
                .Returns(headerDictionary);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            FundingPeriodService fundingPeriodService = CreateFundingPeriodService(logger: logger);

            //Act
            IActionResult result = await fundingPeriodService.SaveFundingPeriods(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is($"Invalid yaml was provided for file: {yamlFile}"));
        }

        [TestMethod]
        async public Task SaveFundingPeriod_GivenValidYamlButFailedToSaveToDatabase_ReturnsStatusCode()
        {
            //Arrange
            string yaml = CreateRawFundingPeriods();
            byte[] byteArray = Encoding.UTF8.GetBytes(yaml);
            MemoryStream stream = new MemoryStream(byteArray);

            IHeaderDictionary headerDictionary = new HeaderDictionary
            {
                { "yaml-file", new StringValues(yamlFile) }
            };

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Headers
                .Returns(headerDictionary);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            
            IPolicyRepository policyRepository = CreatePolicyRepository();
            policyRepository
                .When(x => x.SaveFundingPeriods(Arg.Any<Period[]>()))
                .Do(x => { throw new Exception(); });


            FundingPeriodService fundingPeriodService = CreateFundingPeriodService(logger: logger, policyRepository: policyRepository);

            string errorMessage = $"Exception occurred writing yaml file: {yamlFile} to cosmos db";

            //Act
            IActionResult result = await fundingPeriodService.SaveFundingPeriods(request);

            //Assert
            result
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be(errorMessage);

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is(errorMessage));
        }

        [TestMethod]
        async public Task SaveFundingStream_GivenValidYamlAndSaveWasSuccesful_ReturnsOK()
        {
            //Arrange
            string yaml = CreateRawFundingPeriods();
            byte[] byteArray = Encoding.UTF8.GetBytes(yaml);
            MemoryStream stream = new MemoryStream(byteArray);

            IHeaderDictionary headerDictionary = new HeaderDictionary
            {
                { "yaml-file", new StringValues(yamlFile) }
            };

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Headers
                .Returns(headerDictionary);

            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            IPolicyRepository policyRepository = CreatePolicyRepository();
           
            FundingPeriodService fundingPeriodService = CreateFundingPeriodService(logger: logger, policyRepository: policyRepository);

            //Act
            IActionResult result = await fundingPeriodService.SaveFundingPeriods(request);

            //Assert
            result
                .Should()
                .BeOfType<OkResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"Successfully saved file: {yamlFile} to cosmos db"));

            await
                policyRepository
                    .Received(1)
                    .SaveFundingPeriods(Arg.Is<Period[]>(m => m.Count() == 4));
        }

        private static FundingPeriodService CreateFundingPeriodService(
           ILogger logger = null,
           ICacheProvider cacheProvider = null,
           IPolicyRepository policyRepository = null)
        {
            return new FundingPeriodService(
                logger ?? CreateLogger(),
                cacheProvider ?? CreateCacheProvider(),
                policyRepository ?? CreatePolicyRepository(),
                PolicyResiliencePoliciesTestHelper.GenerateTestPolicies());
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static IPolicyRepository CreatePolicyRepository()
        {
            return Substitute.For<IPolicyRepository>();
        }

        private static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
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
