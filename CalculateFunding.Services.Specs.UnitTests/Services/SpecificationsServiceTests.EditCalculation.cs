using System;
using AutoMapper;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Serilog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using Microsoft.Extensions.Primitives;
using System.Linq.Expressions;
using Newtonsoft.Json;
using System.IO;
using CalculateFunding.Models;
using System.Net;
using System.Security.Claims;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Extensions;
using Microsoft.AspNetCore.Http.Internal;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Caching;
using System.Linq;
using CalculateFunding.Services.Core.Constants;

namespace CalculateFunding.Services.Specs.Services
{
    public partial class SpecificationsServiceTests
    {
        [TestMethod]
        public async Task EditCalculation_GivenNoSpecificationIdWasProvided_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.EditCalculation(request);

            //Arrange
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty specification Id provided");

            logger
                .Received(1)
                .Error(Arg.Is("No specification Id was provided to EditCalculation"));
        }

        [TestMethod]
        public async Task EditCalculation_GivenNoCalculationIdWasProvided_ReturnsBadRequest()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.EditCalculation(request);

            //Arrange
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty calculation Id provided");

            logger
                .Received(1)
                .Error(Arg.Is("No calculation Id was provided to EditCalculation"));
        }

        [TestMethod]
        public async Task EditCalculation_GivenNullEditModeldWasProvided_ReturnsBadRequest()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
                { "calculationId", new StringValues(PolicyId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.EditCalculation(request);

            //Arrange
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null calculation edit model provided");

            logger
                .Received(1)
                .Error(Arg.Is("Null calculation edit model provided to EditCalculation"));
        }

        [TestMethod]
        public async Task EditCalculation_WhenInvalidModelProvided_ThenValidationErrorReturned()
        {
            // Arrange
            ValidationResult validationResult = new ValidationResult();
            validationResult.Errors.Add(new ValidationFailure("error", "error"));

            IValidator<CalculationEditModel> validator = CreateEditCalculationValidator(validationResult);

            CalculationEditModel calculationEditModel = new CalculationEditModel();

            string json = JsonConvert.SerializeObject(calculationEditModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
                { "calculationId", new StringValues(CalculationId) },
            });

            request
                .Query
                .Returns(queryStringValues);
            request
                .Body
                .Returns(stream);

            request
                .HttpContext
                .Returns(context);

            SpecificationsService specificationsService = CreateService(calculationEditModelValidator: validator);

            // Act
            IActionResult result = await specificationsService.EditCalculation(request);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<SerializableError>()
                .Which
                .Should()
                .HaveCount(1);
        }

        [TestMethod]
        public async Task EditCalculation_WhenValidModelButSpecificationCouldNotBeFound_ThenReturnsPreConditionFailed()
        {
            // Arrange
            CalculationEditModel policyEditModel = new CalculationEditModel();

            string json = JsonConvert.SerializeObject(policyEditModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
                { "calculationId", new StringValues(CalculationId) },
            });

            request
                .Query
                .Returns(queryStringValues);
            request
                .Body
                .Returns(stream);

            request
                .HttpContext
                .Returns(context);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns((Specification)null);

            SpecificationsService specificationsService = CreateService(specificationsRepository: specificationsRepository);

            // Act
            IActionResult result = await specificationsService.EditCalculation(request);

            // Assert
            result
                .Should()
                .BeOfType<PreconditionFailedResult>()
                .Which
                .Value
                .Should()
                .Be($"Specification not found for specification id {SpecificationId}");
        }

        [TestMethod]
        public async Task EditCalculation_WhenValidModelButCalculationCouldNotBeFound_ThenReturnsNotFoundResult()
        {
            // Arrange
            CalculationEditModel policyEditModel = new CalculationEditModel();

            string json = JsonConvert.SerializeObject(policyEditModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
                { "calculationId", new StringValues(CalculationId) },
            });

            request
                .Query
                .Returns(queryStringValues);
            request
                .Body
                .Returns(stream);

            request
                .HttpContext
                .Returns(context);

            Specification specification = CreateSpecification();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            SpecificationsService specificationsService = CreateService(specificationsRepository: specificationsRepository);

            // Act
            IActionResult result = await specificationsService.EditCalculation(request);

            // Assert
            result
                .Should()
                .BeOfType<NotFoundObjectResult>()
                .Which
                .Value
                .Should()
                .Be($"Calculation not found for calculation id '{CalculationId}'");
        }

        [TestMethod]
        public async Task EditCalculation_WhenValidModelButUpdateCosmosReturnsBadRequest_ReturnsBadRequest()
        {
            // Arrange
            CalculationEditModel policyEditModel = new CalculationEditModel
            {
                Name = "new calc name",
                CalculationType = "Funding",
                Description = "test description",
                PolicyId = "policy-id-2"
            };

            string json = JsonConvert.SerializeObject(policyEditModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
                { "calculationId", new StringValues(CalculationId) },
            });

            request
                .Query
                .Returns(queryStringValues);
            request
                .Body
                .Returns(stream);

            request
                .HttpContext
                .Returns(context);

            Specification specification = CreateSpecification();
            specification
                .Current
                .Policies = new[] {
                    new Policy { Id = PolicyId, Name = PolicyName, Calculations = new[] { new Calculation { Id = CalculationId, Name = "Old name" } } },
                    new Policy { Id = "policy-id-2", Name = PolicyName }
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            specificationsRepository
                .UpdateSpecification(Arg.Is(specification))
                .Returns(HttpStatusCode.BadRequest);

            SpecificationsService specificationsService = CreateService(specificationsRepository: specificationsRepository);

            // Act
            IActionResult result = await specificationsService.EditCalculation(request);

            // Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>()
                .Which
                .StatusCode
                .Should()
                .Be(400);
        }

        [TestMethod]
        public async Task EditCalculation_WhenUpdatesCosmos_SendsMessageReturnsOk()
        {
            // Arrange
            CalculationEditModel policyEditModel = new CalculationEditModel
            {
                Name = "new calc name",
                CalculationType = "Funding",
                Description = "test description",
                PolicyId = "policy-id-2"
            };

            string json = JsonConvert.SerializeObject(policyEditModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
                { "calculationId", new StringValues(CalculationId) },
            });

            request
                .Query
                .Returns(queryStringValues);
            request
                .Body
                .Returns(stream);

            request
                .HttpContext
                .Returns(context);

            Specification specification = CreateSpecification();
            specification
                .Current
                .Policies = new[] {
                    new Policy { Id = PolicyId, Name = PolicyName, Calculations = new[] { new Calculation { Id = CalculationId, Name = "Old name" } } },
                    new Policy { Id = "policy-id-2", Name = PolicyName }
                };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            specificationsRepository
                .UpdateSpecification(Arg.Is(specification))
                .Returns(HttpStatusCode.OK);

            ICacheProvider cacheProvider = CreateCacheProvider();

            IMessengerService messengerService = CreateMessengerService();

            SpecificationsService specificationsService = CreateService(specificationsRepository: specificationsRepository, cacheProvider: cacheProvider, messengerService: messengerService);

            // Act
            IActionResult result = await specificationsService.EditCalculation(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            specification
                .Current
                .Policies
                .ElementAt(1)
                .Calculations
                .First()
                .Name
                .Should()
                .Be("new calc name");

            await
               messengerService
                   .Received(1)
                   .SendToTopic(Arg.Is(ServiceBusConstants.TopicNames.EditCalculation),
                               Arg.Is<CalculationVersionComparisonModel>(
                                   m => m.CalculationId == CalculationId &&
                                        m.SpecificationId == SpecificationId
                                   ), Arg.Any<IDictionary<string, string>>());
        }
    }
}
