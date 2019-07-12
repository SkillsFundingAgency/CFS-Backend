using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Specs.Interfaces;
using CalculateFunding.Services.Specs.Validators;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Specs.UnitTests.Services
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
                CalculationType = CalculationType.Funding,
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
                CalculationType = CalculationType.Funding,
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

            SpecificationVersion newSpecVersion = specification.Current.Clone() as SpecificationVersion;
            newSpecVersion.Policies.ElementAt(1).Calculations = new[] { new Calculation { Id = CalculationId, Name = "new calc name" } };

            IVersionRepository<SpecificationVersion> versionRepository = CreateVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<SpecificationVersion>(), Arg.Any<SpecificationVersion>())
                .Returns(newSpecVersion);

            ISearchRepository<SpecificationIndex> mockSearchRepository = CreateSearchRepository();
            mockSearchRepository
                .Index(Arg.Any<IEnumerable<SpecificationIndex>>())
                .Returns(new List<IndexError>());

            SpecificationsService specificationsService = CreateService(specificationsRepository: specificationsRepository,
                cacheProvider: cacheProvider, messengerService: messengerService, specificationVersionRepository: versionRepository, searchRepository: mockSearchRepository);

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

            await
                mockSearchRepository
                    .Received(1)
                    .Index(Arg.Is<IEnumerable<SpecificationIndex>>(
                        m => m.First().Id == SpecificationId &&
                             m.First().Status == newSpecVersion.PublishStatus.ToString()
                    ));
        }

        [TestMethod]
        public async Task EditCalculation_WhenCalcInSubPolicyButNotTopLevelPolicyUpdatesCosmos_SendsMessageReturnsOk()
        {
            // Arrange
            CalculationEditModel policyEditModel = new CalculationEditModel
            {
                Name = "new calc name",
                CalculationType = CalculationType.Funding,
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
                    new Policy { Id = PolicyId, Name = PolicyName, SubPolicies = new[] { new Policy { Id = "policy-id-2", Name = "sub-policy", Calculations = new[] { new Calculation { Id = CalculationId, Name = "Old name" } } } }
                } };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            specificationsRepository
                .UpdateSpecification(Arg.Is(specification))
                .Returns(HttpStatusCode.OK);

            ICacheProvider cacheProvider = CreateCacheProvider();

            IMessengerService messengerService = CreateMessengerService();

            SpecificationVersion newSpecVersion = specification.Current.Clone() as SpecificationVersion;
            newSpecVersion.Policies.First().SubPolicies.First().Calculations = new[] { new Calculation { Id = CalculationId, Name = "new calc name" } };

            IVersionRepository<SpecificationVersion> versionRepository = CreateVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<SpecificationVersion>(), Arg.Any<SpecificationVersion>())
                .Returns(newSpecVersion);

            SpecificationsService specificationsService = CreateService(specificationsRepository: specificationsRepository,
                cacheProvider: cacheProvider, messengerService: messengerService, specificationVersionRepository: versionRepository);

            // Act
            IActionResult result = await specificationsService.EditCalculation(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            specification
                .Current
                .Policies
                .First()
                .SubPolicies
                .First()
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

            await
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(newSpecVersion));
        }

        [TestMethod]
        public void EditCalculation_WhenSomethingGoesWrongDuringIndexing_ShouldThrowException()
        {
            // Arrange
            const string errorMessage = "Encountered error 802";

            CalculationEditModel policyEditModel = new CalculationEditModel
            {
                Name = "new calc name",
                CalculationType = CalculationType.Funding,
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

            SpecificationVersion newSpecVersion = specification.Current.Clone() as SpecificationVersion;
            newSpecVersion.Policies.ElementAt(1).Calculations = new[] { new Calculation { Id = CalculationId, Name = "new calc name" } };

            IVersionRepository<SpecificationVersion> versionRepository = CreateVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<SpecificationVersion>(), Arg.Any<SpecificationVersion>())
                .Returns(newSpecVersion);

            ISearchRepository<SpecificationIndex> mockSearchRepository = CreateSearchRepository();
            mockSearchRepository
                .Index(Arg.Any<IEnumerable<SpecificationIndex>>())
                .Returns(new List<IndexError>() { new IndexError() { ErrorMessage = errorMessage } });

            SpecificationsService specificationsService = CreateService(specificationsRepository: specificationsRepository,
                cacheProvider: cacheProvider, messengerService: messengerService, specificationVersionRepository: versionRepository, searchRepository: mockSearchRepository);

            //Act
            Func<Task<IActionResult>> editSpecification = async () => await specificationsService.EditCalculation(request);

            //Assert
            editSpecification
                .Should()
                .Throw<ApplicationException>()
                .Which
                .Message
                .Should()
                .Be($"Could not index specification {specification.Current.Id} because: {errorMessage}");
        }

        [TestMethod]
        public async Task EditCalculation_WhenRenameCalculationToBrandNew_ReturnsOk()
        {
            // Arrange
            CalculationEditModel policyEditModel = new CalculationEditModel
            {
                Name = "new calc name",
                CalculationType = CalculationType.Funding,
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
                    new Policy { Id = PolicyId, Name = PolicyName, SubPolicies = new[] { new Policy { Id = "policy-id-2", Name = "sub-policy", Calculations = new[] { new Calculation { Id = CalculationId, Name = "Old name" } } } }
                } };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            specificationsRepository
                .UpdateSpecification(Arg.Is(specification))
                .Returns(HttpStatusCode.OK);

            SpecificationVersion newSpecVersion = specification.Current.Clone() as SpecificationVersion;
            newSpecVersion.Policies.First().SubPolicies.First().Calculations = new[] { new Calculation { Id = CalculationId, Name = "new calc name" } };

            IVersionRepository<SpecificationVersion> versionRepository = CreateVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<SpecificationVersion>(), Arg.Any<SpecificationVersion>())
                .Returns(newSpecVersion);

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .IsCalculationNameValid(Arg.Is(SpecificationId), Arg.Is("new calc name"), Arg.Is(CalculationId))
                .Returns(true);

            SpecificationsService specificationsService = CreateService(specificationsRepository: specificationsRepository,
                specificationVersionRepository: versionRepository);

            // Act
            IActionResult result = await specificationsService.EditCalculation(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            await
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(newSpecVersion));
        }

        [TestMethod]
        public async Task EditCalculation_WhenRenameCalcToSameAsExisting_ReturnsBadRequest()
        {
            // Arrange
            CalculationEditModel policyEditModel = new CalculationEditModel
            {
                Name = "Another name",
                CalculationType = CalculationType.Funding,
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
                    new Policy
                    {
                        Id = PolicyId,
                        Name = PolicyName,
                        Calculations = new[] 
                        {
                            new Calculation { Id = CalculationId, Name = "Old name" },
                            new Calculation { Id = "calc2", Name = "Another name" }
                        }
                    }
                };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .IsCalculationNameValid(Arg.Is(specification.Id), Arg.Is("Another name"), Arg.Is(CalculationId))
                .Returns(false);

            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();
            IMapper mapper = CreateImplementedMapper();
            IValidator<CalculationEditModel> validator = CreateRealEditCalculationValidator(
                mapper: mapper,
                specificationsRepository: specificationsRepository,
                calculationsRepository: calculationsRepository,
                policiesApiClient: policiesApiClient);

            SpecificationsService specificationsService = CreateService(specificationsRepository: specificationsRepository,
                calculationEditModelValidator: validator);

            // Act
            IActionResult result = await specificationsService.EditCalculation(request);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        private IValidator<CalculationEditModel> CreateRealEditCalculationValidator(
            IMapper mapper,
            ISpecificationsRepository specificationsRepository,
            ICalculationsRepository calculationsRepository,
            IPoliciesApiClient policiesApiClient)
        {
            return new CalculationEditModelValidator(mapper, specificationsRepository, calculationsRepository, policiesApiClient);
        }
    }
}
