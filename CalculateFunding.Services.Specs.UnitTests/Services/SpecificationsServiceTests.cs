using AutoMapper;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Logging;
using CalculateFunding.Services.Specs.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Serilog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog.Debugging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using System.Collections.Specialized;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;

namespace CalculateFunding.Services.Specs.Services
{
    [TestClass]
    public class SpecificationsServiceTests
    {
        const string SpecificationId = "ffa8ccb3-eb8e-4658-8b3f-f1e4c3a8f313";

        [TestMethod]
        public async Task GetSpecificationById_GivenSpecificationIdDoesNotExist_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.GetSpecificationById(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Any<string>());
        }

        [TestMethod]
        public async Task GetSpecificationById_GivenSpecificationWasNotFound_ReturnsNotFoundt()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns((Specification)null);

            SpecificationsService service = CreateService(specifcationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetSpecificationById(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Warning(Arg.Is($"A specification for id {SpecificationId} could not found"));
        }

        [TestMethod]
        public async Task GetSpecificationById_GivenSpecificationWasFound_ReturnsSuccesst()
        {
            //Arrange
            Specification specification = new Specification();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            SpecificationsService service = CreateService(specifcationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetSpecificationById(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();
        }



        static SpecificationsService CreateService(IMapper mapper = null, ISpecificationsRepository specifcationsRepository = null, 
            ILogger logs = null, IValidator<PolicyCreateModel> policyCreateModelValidator = null,
            IValidator<SpecificationCreateModel> specificationCreateModelvalidator = null, IValidator<CalculationCreateModel> calculationCreateModelValidator = null)
        {
            return new SpecificationsService(mapper ?? CreateMapper(), specifcationsRepository ?? CreateSpecificationsRepository(), logs ?? CreateLogger(), policyCreateModelValidator ?? CreatePolicyValidator(),
                specificationCreateModelvalidator ?? CreateSpecificationValidator(), calculationCreateModelValidator ?? CreateCalculationValidator());
        }

        static IMapper CreateMapper()
        {
            return Substitute.For<IMapper>();
        }

        static ISpecificationsRepository CreateSpecificationsRepository()
        {
            return Substitute.For<ISpecificationsRepository>();
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

       
        static IValidator<PolicyCreateModel> CreatePolicyValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
                validationResult = new ValidationResult();

            IValidator<PolicyCreateModel> validator = Substitute.For<IValidator<PolicyCreateModel>>();

            validator
               .ValidateAsync(Arg.Any<PolicyCreateModel>())
               .Returns(validationResult);

            return validator;
        }

        static IValidator<SpecificationCreateModel> CreateSpecificationValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
                validationResult = new ValidationResult();

            IValidator<SpecificationCreateModel> validator = Substitute.For<IValidator<SpecificationCreateModel>>();

            validator
               .ValidateAsync(Arg.Any<SpecificationCreateModel>())
               .Returns(validationResult);

            return validator;
        }

        static IValidator<CalculationCreateModel> CreateCalculationValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
                validationResult = new ValidationResult();

            IValidator<CalculationCreateModel> validator = Substitute.For<IValidator<CalculationCreateModel>>();

            validator
               .ValidateAsync(Arg.Any<CalculationCreateModel>())
               .Returns(validationResult);

            return validator;
        }
    }

   
}
