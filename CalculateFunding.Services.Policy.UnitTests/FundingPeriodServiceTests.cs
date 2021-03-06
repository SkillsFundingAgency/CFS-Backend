﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Models.Policy;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Services.Providers.Validators;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Policy.UnitTests
{
    [TestClass]
    public class FundingPeriodServiceTests
    {

        [TestMethod]
        public async Task GetFundingPeriods_GivenNullOrEmptyPeriodsReturned_LogsAndReturnsOKWithEmptyList()
        {
            // Arrange
            ILogger logger = CreateLogger();

            IEnumerable<FundingPeriod> Periods = null;

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

            IEnumerable<FundingPeriod> values = objectResult.Value as IEnumerable<FundingPeriod>;

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

            IEnumerable<FundingPeriod> Periods = new[]
            {
                new FundingPeriod(),
                new FundingPeriod()
            };

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<FundingPeriod[]>(Arg.Is(CacheKeys.FundingPeriods))
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

            IEnumerable<FundingPeriod> values = objectResult.Value as IEnumerable<FundingPeriod>;

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

            IEnumerable<FundingPeriod> Periods = new[]
            {
                new FundingPeriod(),
                new FundingPeriod()
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

            IEnumerable<FundingPeriod> values = objectResult.Value as IEnumerable<FundingPeriod>;

            values
                .Should()
                .HaveCount(2);

            await
                cacheProvider
                    .Received(1)
                    .SetAsync<FundingPeriod[]>(Arg.Is(CacheKeys.FundingPeriods), Arg.Is<FundingPeriod[]>(m => m.SequenceEqual(Periods)));
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
                .Returns((FundingPeriod)null);

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

            FundingPeriod fundingPeriod = new FundingPeriod
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
        public async Task SaveFundingPeriod_GivenNoJsonWasProvided_ReturnsBadRequest()
        {
            //Arrange
            ILogger logger = CreateLogger();

            FundingPeriodService fundingPeriodService = CreateFundingPeriodService(logger: logger);

            //Act
            IActionResult result = await fundingPeriodService.SaveFundingPeriods(null);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is($"Null or empty json provided for file"));
        }

        [TestMethod]
        public async Task SaveFundingPeriod_GivenValidJsonButFailedToSaveToDatabase_ReturnsStatusCode()
        {
            //Arrange
            ILogger logger = CreateLogger();

            IPolicyRepository policyRepository = CreatePolicyRepository();
            policyRepository
                .When(x => x.SaveFundingPeriods(Arg.Any<FundingPeriod[]>()))
                .Do(x => { throw new Exception(); });

            FundingPeriodService fundingPeriodService = CreateFundingPeriodService(logger: logger, policyRepository: policyRepository);

            string errorMessage = $"Exception occurred writing json file to cosmos db";

            //Act
            IActionResult result = await fundingPeriodService.SaveFundingPeriods(CreateFundingPeriodsJsonModel());

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
        public async Task SaveFundingStream_GivenValidYamlAndSaveWasSuccesful_ReturnsOK()
        {
            //Arrange
            ILogger logger = CreateLogger();

            IPolicyRepository policyRepository = CreatePolicyRepository();

            ICacheProvider cacheProvider = CreateCacheProvider();

            FundingPeriodService fundingPeriodService = CreateFundingPeriodService(logger: logger, policyRepository: policyRepository, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await fundingPeriodService.SaveFundingPeriods(CreateFundingPeriodsJsonModel());

            //Assert
            result
                .Should()
                .BeOfType<OkResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"Successfully saved file to cosmos db"));

            await
                policyRepository
                    .Received(1)
                    .SaveFundingPeriods(Arg.Is<FundingPeriod[]>(m => m.Count() == 4));

            await cacheProvider
                .Received(1)
                .RemoveAsync<FundingPeriod[]>(CacheKeys.FundingPeriods);
        }

        [TestMethod]
        public async Task GetAllFundingPeriods_GivenNullOrEmptyPeriodsReturned_LogsAndReturnsOKWithEmptyList()
        {
            // Arrange
            ILogger logger = CreateLogger();

            IEnumerable<FundingPeriod> Periods = null;

            IPolicyRepository policyRepository = CreatePolicyRepository();
            policyRepository
                .GetFundingPeriods()
                .Returns(Periods);

            FundingPeriodService fundingPeriodService = CreateFundingPeriodService(logger: logger, policyRepository: policyRepository);

            // Act
            IEnumerable<FundingPeriod> result = await fundingPeriodService.GetAllFundingPeriods();

            // Assert
            result
                .Should()
                .BeOfType<FundingPeriod[]>()
                .Which
                .Should()
                .AllBeEquivalentTo(Periods);

            logger
                .Received(1)
                .Error(Arg.Is("No funding periods were returned"));
        }

        [TestMethod]
        public async Task GetAllFundingPeriods_ReturnsSuccess()
        {
            ILogger logger = CreateLogger();

            IEnumerable<FundingPeriod> Periods = new List<FundingPeriod>
            {
                new FundingPeriod(),
                new FundingPeriod()
            };

            IPolicyRepository policyRepository = CreatePolicyRepository();
            policyRepository
                .GetFundingPeriods()
                .Returns(Periods);

            FundingPeriodService fundingPeriodService = CreateFundingPeriodService(logger: logger, policyRepository: policyRepository);

            IEnumerable<FundingPeriod> result = await fundingPeriodService.GetAllFundingPeriods();

            result
                .Should()
                .HaveCount(2);
        }

        private string NewRandomString() => new RandomString();

        private static FundingPeriodService CreateFundingPeriodService(
           ILogger logger = null,
           ICacheProvider cacheProvider = null,
           IPolicyRepository policyRepository = null,
           IValidator<FundingPeriodsJsonModel> fundingPeriodValidator = null)
        {
            return new FundingPeriodService(
                logger ?? CreateLogger(),
                cacheProvider ?? CreateCacheProvider(),
                policyRepository ?? CreatePolicyRepository(),
                PolicyResiliencePoliciesTestHelper.GenerateTestPolicies(),
                fundingPeriodValidator ?? CreateFundingPeriodValidator());
        }

        private static IValidator<FundingPeriodsJsonModel> CreateFundingPeriodValidator()
        {
            ValidationResult validationResult = null;
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<FundingPeriodsJsonModel> validator = Substitute.For<IValidator<FundingPeriodsJsonModel>>();

            validator
               .ValidateAsync(Arg.Any<FundingPeriodsJsonModel>())
               .Returns(validationResult);

            return validator;
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

        private FundingPeriodsJsonModel CreateFundingPeriodsJsonModel()
        {
            List<FundingPeriod> periods = new List<FundingPeriod>
            {
                new FundingPeriod()
                {
                     Id = "AY2017181",
                     Name = "Academic 2017/18",
                     StartDate = DateTimeOffset.Now.Date,
                     EndDate = DateTimeOffset.Now.Date
                },
                new FundingPeriod()
                {
                     Id = "AY2018191",
                     Name = "Academic 2018/19",
                     StartDate = DateTimeOffset.Now.Date,
                     EndDate = DateTimeOffset.Now.Date
                },
                new FundingPeriod()
                {
                     Id = "FY2017181",
                     Name = "Academic 2017/18",
                     StartDate = DateTimeOffset.Now.Date,
                     EndDate = DateTimeOffset.Now.Date
                },
                 new FundingPeriod()
                {
                     Id = "AY2018191",
                     Name = "Academic 2018/19",
                     StartDate = DateTimeOffset.Now.Date,
                     EndDate = DateTimeOffset.Now.Date
                }
            };

            return new FundingPeriodsJsonModel { FundingPeriods = periods.ToArray() };
        }

        private string CreateRawJsonFundingPeriod()
        {
            return JsonConvert.SerializeObject(CreateFundingPeriodsJsonModel());
        }

        private string CreateRawFundingPeriods()
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
