using AutoMapper;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Policy.FundingPolicy;
using CalculateFunding.Models.Policy.FundingPolicy.ViewModels;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Services.Policy.MappingProfiles;
using CalculateFunding.Services.Policy.UnitTests;
using CalculateFunding.Services.Policy.Validators;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Policy
{
    [TestClass]
    public class FundingDateServiceTests
    {
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private string _fundingLineId;

        [TestInitialize]
        public void Initialize()
        {
            _fundingStreamId = NewRandomString();
            _fundingPeriodId = NewRandomString();
            _fundingLineId = NewRandomString();
        }

        [TestMethod]
        public async Task GetFundingDate_GivenEmptyFundingStreamId_ReturnsBadRequestResult()
        {
            // Arrange
            ILogger logger = CreateLogger();

            FundingDateService fundingDateService = CreateFundingDateService(logger: logger);

            // Act
            IActionResult result = await fundingDateService.GetFundingDate(null, _fundingPeriodId, _fundingLineId);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty funding stream Id provided");

            logger
                .Received(1)
                .Error(Arg.Is("No funding stream Id was provided to GetFundingDate"));
        }

        [TestMethod]
        public async Task GetFundingDate_GivenEmptyFundingPeriodId_ReturnsBadRequestResult()
        {
            // Arrange
            ILogger logger = CreateLogger();

            FundingDateService fundingDateService = CreateFundingDateService(logger: logger);

            // Act
            IActionResult result = await fundingDateService.GetFundingDate(_fundingStreamId, null, _fundingLineId);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty funding period Id provided");

            logger
                .Received(1)
                .Error(Arg.Is("No funding period Id was provided to GetFundingDate"));
        }

        [TestMethod]
        public async Task GetFundingDate_GivenEmptyFundingLineId_ReturnsBadRequestResult()
        {
            // Arrange
            ILogger logger = CreateLogger();

            FundingDateService fundingDateService = CreateFundingDateService(logger: logger);

            // Act
            IActionResult result = await fundingDateService.GetFundingDate(_fundingStreamId, _fundingPeriodId, null);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty funding line Id provided");

            logger
                .Received(1)
                .Error(Arg.Is("No funding line Id was provided to GetFundingDate"));
        }

        [TestMethod]
        public async Task GetFundingDate_GivenFundingDateNotFound_ReturnsNotFoundResult()
        {
            // Arrange
            ILogger logger = CreateLogger();

            FundingDateService fundingDateService = CreateFundingDateService(logger: logger);

            // Act
            IActionResult result = await fundingDateService.GetFundingDate(_fundingStreamId, _fundingPeriodId, _fundingLineId);

            // Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Error(Arg.Is($"No funding Dates were found for funding stream id : {_fundingStreamId}"));
        }

        [TestMethod]
        public async Task GetFundingDate_GivenFundingDate_ReturnsOkResult()
        {
            // Arrange
            ILogger logger = CreateLogger();

            string fundingDateId = $"fundingdate-{_fundingStreamId}-{_fundingPeriodId}-{_fundingLineId}";

            IPolicyRepository policyRepository = CreatePolicyRepository();
            policyRepository
                .GetFundingDate(fundingDateId)
                .Returns(NewFundingDate(_ => _.WithFundingStreamId(_fundingStreamId)));

            FundingDateService fundingDateService = CreateFundingDateService(logger: logger, policyRepository: policyRepository);

            // Act
            IActionResult result = await fundingDateService.GetFundingDate(_fundingStreamId, _fundingPeriodId, _fundingLineId);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<FundingDate>()
                .And
                .Should()
                .NotBeNull();

            FundingDate fundingDate = (result as OkObjectResult).Value as FundingDate;

            fundingDate
                .FundingStreamId
                .Should()
                .Be(_fundingStreamId);
        }

        [TestMethod]
        public async Task SaveFundingDate_GivenEmptyFundingStreamId_ReturnsBadRequestResult()
        {
            // Arrange
            ILogger logger = CreateLogger();

            FundingDateService fundingDateService = CreateFundingDateService(logger: logger);

            // Act
            IActionResult result = await fundingDateService.SaveFundingDate(null, null, null, _fundingPeriodId, _fundingLineId, null);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty funding stream Id provided");

            logger
                .Received(1)
                .Error(Arg.Is("No funding stream Id was provided to SaveFundingDate"));
        }

        [TestMethod]
        public async Task SaveFundingDate_GivenEmptyFundingPeriodId_ReturnsBadRequestResult()
        {
            // Arrange
            ILogger logger = CreateLogger();

            FundingDateService fundingDateService = CreateFundingDateService(logger: logger);

            // Act
            IActionResult result = await fundingDateService.SaveFundingDate(null, null, _fundingStreamId, null, _fundingLineId, null);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty funding period Id provided");

            logger
                .Received(1)
                .Error(Arg.Is("No funding period Id was provided to SaveFundingDate"));
        }

        [TestMethod]
        public async Task SaveFundingDate_GivenEmptyFundingLineId_ReturnsBadRequestResult()
        {
            // Arrange
            ILogger logger = CreateLogger();

            FundingDateService fundingDateService = CreateFundingDateService(logger: logger);

            // Act
            IActionResult result = await fundingDateService.SaveFundingDate(null, null, _fundingStreamId, _fundingPeriodId, null, null);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty funding line Id provided");

            logger
                .Received(1)
                .Error(Arg.Is("No funding line Id was provided to SaveFundingDate"));
        }

        [TestMethod]
        public async Task SaveFundingDate_GivenEmptyFundingDateViewModel_ReturnsBadRequestResult()
        {
            // Arrange
            ILogger logger = CreateLogger();

            FundingDateService fundingDateService = CreateFundingDateService(logger: logger);

            // Act
            IActionResult result = await fundingDateService.SaveFundingDate(null, null, _fundingStreamId, _fundingPeriodId, _fundingLineId, null);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty funding date view model provided");

            logger
                .Received(1)
                .Error(Arg.Is("No funding date view model was provided to SaveFundingDate"));
        }

        [TestMethod]
        public async Task SaveFundingDate_GivenInvalidViewModel_ReturnsBadRequestResult()
        {
            // Arrange
            ILogger logger = CreateLogger();

            ValidationResult validationResult = new ValidationResult(
                new List<ValidationFailure>
                {
                    new ValidationFailure("FundingStreamId", "error")
                });
            IValidator<FundingDate> validator = CreateValidator(validationResult);

            FundingDateService fundingDateService = CreateFundingDateService(logger: logger, validator: validator);

            FundingDateViewModel fundingDateViewModel = CreateFundingDateViewModel();

            // Act
            IActionResult result = await fundingDateService.SaveFundingDate(
                null, null, _fundingStreamId, _fundingPeriodId, _fundingLineId, fundingDateViewModel);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task SaveFundingDate_GivenInvalidSaveFundingDateResult_ReturnsSaveFundingDateResult()
        {
            // Arrange
            ILogger logger = CreateLogger();

            HttpStatusCode statusCode = HttpStatusCode.InternalServerError;

            IPolicyRepository policyRepository = CreatePolicyRepository();
            policyRepository
                .SaveFundingDate(
                    Arg.Is<FundingDate>(_ => _
                        .FundingStreamId == _fundingStreamId))
                .Returns(statusCode);

            FundingDateService fundingDateService = CreateFundingDateService(logger: logger, policyRepository: policyRepository);

            FundingDateViewModel fundingDateViewModel = CreateFundingDateViewModel();

            // Act
            IActionResult result = await fundingDateService.SaveFundingDate(
                null, null, _fundingStreamId, _fundingPeriodId, _fundingLineId, fundingDateViewModel);

            // Assert
            result
                .Should()
                .BeOfType<InternalServerErrorResult>();

            logger
            .Received(1)
            .Error(Arg.Is($"Failed to save funding date for funding stream id: {_fundingStreamId} and period id: {_fundingPeriodId} and funding line id: {_fundingLineId} to cosmos db with status {(int)statusCode}"));
        }

        [TestMethod]
        public async Task SaveFundingDate_GivenValidViewModel_ReturnsOkResult()
        {
            // Arrange
            ILogger logger = CreateLogger();

            HttpStatusCode statusCode = HttpStatusCode.OK;

            IPolicyRepository policyRepository = CreatePolicyRepository();
            policyRepository
                .SaveFundingDate(
                    Arg.Is<FundingDate>(_ => _
                        .FundingStreamId == _fundingStreamId))
                .Returns(statusCode);

            FundingDateService fundingDateService = CreateFundingDateService(logger: logger, policyRepository: policyRepository);

            FundingDateViewModel fundingDateViewModel = CreateFundingDateViewModel();

            // Act
            IActionResult result = await fundingDateService.SaveFundingDate(
                null, null, _fundingStreamId, _fundingPeriodId, _fundingLineId, fundingDateViewModel);

            // Assert
            result
                .Should()
                .BeOfType<CreatedAtActionResult>()
                .Which
                .Should()
                .NotBeNull();

            logger
            .Received(1)
            .Information(Arg.Is($"Successfully saved funding date for funding stream id: {_fundingStreamId} and period id: {_fundingPeriodId} and funding line id: {_fundingLineId} to cosmos db"));
        }

        private static FundingDateViewModel CreateFundingDateViewModel()
        {
            return new FundingDateViewModel
            {
                Patterns = new List<FundingDatePattern>()
            };
        }

        private static FundingDateService CreateFundingDateService(
            IPolicyRepository policyRepository = null,
            ILogger logger = null,
            IValidator<FundingDate> validator = null,
            IMapper mapper = null)
        {
            return new FundingDateService(
                policyRepository ?? CreatePolicyRepository(),
                PolicyResiliencePoliciesTestHelper.GenerateTestPolicies(),
                logger ?? CreateLogger(),
                validator ?? CreateValidator(),
                mapper ?? CreateMapper());
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static IPolicyRepository CreatePolicyRepository()
        {
            return Substitute.For<IPolicyRepository>();
        }

        private static IMapper CreateMapper()
        {
            MapperConfiguration config = new MapperConfiguration(c =>
            {
                c.AddProfile<FundingConfigurationMappingProfile>();
            });

            return new Mapper(config);
        }

        private static IValidator<FundingDate> CreateValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<FundingDate> validator = Substitute.For<IValidator<FundingDate>>();

            validator
               .ValidateAsync(Arg.Any<FundingDate>())
               .Returns(validationResult);

            return validator;
        }

        protected FundingPeriod NewFundingPeriod(Action<FundingPeriodBuilder> setUp = null)
        {
            FundingPeriodBuilder fundingPeriodBuilder = new FundingPeriodBuilder();

            setUp?.Invoke(fundingPeriodBuilder);

            return fundingPeriodBuilder.Build();
        }

        protected FundingStream NewFundingStream(Action<FundingStreamBuilder> setUp = null)
        {
            FundingStreamBuilder fundingStreamBuilder = new FundingStreamBuilder();

            setUp?.Invoke(fundingStreamBuilder);

            return fundingStreamBuilder.Build();
        }

        protected FundingDate NewFundingDate(Action<FundingDateBuilder> setUp = null)
        {
            FundingDateBuilder fundingDateBuilder = new FundingDateBuilder();

            setUp?.Invoke(fundingDateBuilder);

            return fundingDateBuilder.Build();
        }

        private string NewRandomString() => new RandomString();
    }
}
