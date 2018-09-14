using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Serilog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces;

namespace CalculateFunding.Services.Specs.Services
{
    public partial class SpecificationsServiceTests
    {
        [TestMethod]
        public async Task EditPolicy_GivenNoSpecificationIdWasProvided_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.EditPolicy(request);

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
                .Error(Arg.Is("No specification Id was provided to EditPolicy"));
        }

        [TestMethod]
        public async Task EditPolicy_GivenNoPolicyIdWasProvided_ReturnsBadRequest()
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
            IActionResult result = await service.EditPolicy(request);

            //Arrange
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty policy Id provided");

            logger
                .Received(1)
                .Error(Arg.Is("No policy Id was provided to EditPolicy"));
        }

        [TestMethod]
        public async Task EditPolicy_GivenNullEditModeldWasProvided_ReturnsBadRequest()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
                { "policyId", new StringValues(PolicyId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.EditPolicy(request);

            //Arrange
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null policy edit model provided");

            logger
                .Received(1)
                .Error(Arg.Is("Null edit modeld was provided to EditPolicy"));
        }

        [TestMethod]
        public async Task EditPolicy_WhenInvalidModelProvided_ThenValidationErrorReturned()
        {
            // Arrange
            ValidationResult validationResult = new ValidationResult();
            validationResult.Errors.Add(new ValidationFailure("error", "error"));

            IValidator<PolicyEditModel> validator = CreateEditPolicyValidator(validationResult);

            PolicyEditModel policyEditModel = new PolicyEditModel();

            string json = JsonConvert.SerializeObject(policyEditModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
                { "policyId", new StringValues(PolicyId) },
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

            SpecificationsService specificationsService = CreateService(policyEditModelValidator: validator);

            // Act
            IActionResult result = await specificationsService.EditPolicy(request);

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
        public async Task EditPolicy_WhenValidModelButSpecificationCouldNotBeFound_ThenReturnsPreConditionFailed()
        {
            // Arrange
            PolicyEditModel policyEditModel = new PolicyEditModel
            {
                SpecificationId = SpecificationId
            };

            string json = JsonConvert.SerializeObject(policyEditModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
                { "policyId", new StringValues(PolicyId) },
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
            IActionResult result = await specificationsService.EditPolicy(request);

            // Assert
            result
                .Should()
                .BeOfType<PreconditionFailedResult>()
                .Which
                .Value
                .Should()
                .Be($"Failed to find specification for id: {SpecificationId}");
        }

        [TestMethod]
        public async Task EditPolicy_WhenValidModelButPolicyCouldNotBeFound_ThenReturnsNotFoundResult()
        {
            // Arrange
            PolicyEditModel policyEditModel = new PolicyEditModel
            {
                SpecificationId = SpecificationId
            };

            string json = JsonConvert.SerializeObject(policyEditModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
                { "policyId", new StringValues(PolicyId) },
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
            IActionResult result = await specificationsService.EditPolicy(request);

            // Assert
            result
                .Should()
                .BeOfType<NotFoundObjectResult>()
                .Which
                .Value
                .Should()
                .Be($"Failed to find policy for policy id: {PolicyId}");
        }

        [TestMethod]
        public async Task EditPolicy_WhenValidModelButUpdateCosmosReturnsBadRequest_ReturnsBadRequest()
        {
            // Arrange
            PolicyEditModel policyEditModel = new PolicyEditModel
            {
                SpecificationId = SpecificationId,
                Name = "new policy name"
            };

            string json = JsonConvert.SerializeObject(policyEditModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
                { "policyId", new StringValues(PolicyId) },
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
                .Policies = new[] { new Policy { Id = PolicyId, Name = PolicyName } };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            specificationsRepository
                .UpdateSpecification(Arg.Is(specification))
                .Returns(HttpStatusCode.BadRequest);

            SpecificationsService specificationsService = CreateService(specificationsRepository: specificationsRepository);

            // Act
            IActionResult result = await specificationsService.EditPolicy(request);

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
        public async Task EditPolicy_WhenValidModelAndUpdateCosmos_SendsMessageAndReturnsOK()
        {
            // Arrange
            PolicyEditModel policyEditModel = new PolicyEditModel
            {
                SpecificationId = SpecificationId,
                Name = "new policy name"
            };

            string json = JsonConvert.SerializeObject(policyEditModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
                { "policyId", new StringValues(PolicyId) },
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
                .Policies = new[] { new Policy { Id = PolicyId, Name = PolicyName } };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            specificationsRepository
                .UpdateSpecification(Arg.Is(specification))
                .Returns(HttpStatusCode.OK);

            IMessengerService messengerService = CreateMessengerService();

            SpecificationsService specificationsService = CreateService(specificationsRepository: specificationsRepository, messengerService: messengerService);

            // Act
            IActionResult result = await specificationsService.EditPolicy(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<Policy>();


            await
                messengerService
                    .Received(1)
                    .SendToTopic(Arg.Is(ServiceBusConstants.TopicNames.EditSpecification),
                                Arg.Is<SpecificationVersionComparisonModel>(
                                    m => m.Id == SpecificationId
                                    ), Arg.Any<IDictionary<string, string>>());
        }

        [TestMethod]
        public async Task EditPolicy_WhenValidModelAndUpdateCosmosAndIsASubPolicy_SendsMessageAndReturnsOK()
        {
            // Arrange
            PolicyEditModel policyEditModel = new PolicyEditModel
            {
                SpecificationId = SpecificationId,
                Name = "new policy name",
                Description = "new policy description",
                ParentPolicyId = "parent-policy-id"
            };

            string json = JsonConvert.SerializeObject(policyEditModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
                { "policyId", new StringValues(PolicyId) },
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
                .Policies = new[]
                {
                    new Policy
                        {
                            Id = "parent-policy-id" ,
                            Name = "policy name",
                            SubPolicies = new[] { new Policy { Id = PolicyId, Name =  PolicyName }
                         }
                    }
                };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            specificationsRepository
                .UpdateSpecification(Arg.Is(specification))
                .Returns(HttpStatusCode.OK);

            IMessengerService messengerService = CreateMessengerService();

            SpecificationsService specificationsService = CreateService(specificationsRepository: specificationsRepository, messengerService: messengerService);

            // Act
            IActionResult result = await specificationsService.EditPolicy(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<Policy>()
                .Which
                .Description
                .Should()
                .Be("new policy description");

            await
                messengerService
                    .Received(1)
                    .SendToTopic(Arg.Is(ServiceBusConstants.TopicNames.EditSpecification),
                                Arg.Is<SpecificationVersionComparisonModel>(
                                    m => m.Id == SpecificationId
                                    ), Arg.Any<IDictionary<string, string>>());
        }

        [TestMethod]
        public async Task EditPolicy_WhenPolicyWasASubPolicyButNowTopLevelPolicy_SavesChanges()
        {
            // Arrange
            PolicyEditModel policyEditModel = new PolicyEditModel
            {
                SpecificationId = SpecificationId,
                Name = "new policy name",
                Description = "new policy description",
                ParentPolicyId = null
            };

            string json = JsonConvert.SerializeObject(policyEditModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
                { "policyId", new StringValues(PolicyId) },
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
                .Policies = new[]
                {
                    new Policy
                        {
                            Id = "parent-policy-id" ,
                            Name = "policy name",
                            SubPolicies = new[] { new Policy { Id = PolicyId, Name =  PolicyName }
                         }
                    }
                };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            specificationsRepository
                .UpdateSpecification(Arg.Is(specification))
                .Returns(HttpStatusCode.OK);

            IMessengerService messengerService = CreateMessengerService();

            SpecificationVersion newSpecVersion = specification.Current.Clone() as SpecificationVersion;
            newSpecVersion.Policies = newSpecVersion.Policies.Concat(new[] { specification.Current.Policies.First().SubPolicies.First() });
            newSpecVersion.Policies.First().SubPolicies = Enumerable.Empty<Policy>();
            
            IVersionRepository<SpecificationVersion> versionRepository = CreateVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<SpecificationVersion>(), Arg.Any<SpecificationVersion>())
                .Returns(newSpecVersion);

            SpecificationsService specificationsService = CreateService(specificationsRepository: specificationsRepository, 
                messengerService: messengerService, specificationVersionRepository: versionRepository);

            // Act
            IActionResult result = await specificationsService.EditPolicy(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            specification
                .Current
                .Policies
                .Count()
                .Should()
                .Be(2);

            specification
              .Current
              .Policies
              .First()
              .SubPolicies
              .Any()
              .Should()
              .BeFalse();

            await
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(newSpecVersion));
        }

        [TestMethod]
        public async Task EditPolicy_WhenPolicyASubPolicyForOnePolicyButSubPolicyOfAnotherPolicy_SavesChanges()
        {
            // Arrange
            PolicyEditModel policyEditModel = new PolicyEditModel
            {
                SpecificationId = SpecificationId,
                Name = "new policy name",
                Description = "new policy description",
                ParentPolicyId = "new-parent-policy-id"
            };

            string json = JsonConvert.SerializeObject(policyEditModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
                { "policyId", new StringValues(PolicyId) },
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
                .Policies = new[]
                {
                    new Policy
                        {
                            Id = "parent-policy-id" ,
                            Name = "policy name",
                            SubPolicies = new[] { new Policy { Id = PolicyId, Name =  PolicyName }
                         }
                    },
                    new Policy
                    {
                        Id = "new-parent-policy-id" ,
                        Name = "policy name",
                        SubPolicies = Enumerable.Empty<Policy>()
                    }
                };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            specificationsRepository
                .UpdateSpecification(Arg.Is(specification))
                .Returns(HttpStatusCode.OK);

            IMessengerService messengerService = CreateMessengerService();

            SpecificationVersion newSpecVersion = specification.Current.Clone() as SpecificationVersion;
            newSpecVersion.Policies.Last().SubPolicies = newSpecVersion.Policies.Concat(new[] { specification.Current.Policies.First().SubPolicies.First() });
            newSpecVersion.Policies.First().SubPolicies = Enumerable.Empty<Policy>();

            IVersionRepository<SpecificationVersion> versionRepository = CreateVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<SpecificationVersion>(), Arg.Any<SpecificationVersion>())
                .Returns(newSpecVersion);

            SpecificationsService specificationsService = CreateService(specificationsRepository: specificationsRepository, 
                messengerService: messengerService, specificationVersionRepository: versionRepository);

            // Act
            IActionResult result = await specificationsService.EditPolicy(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            specification
                .Current
                .Policies
                .Count()
                .Should()
                .Be(2);

            specification
               .Current
               .Policies
               .First()
               .SubPolicies
               .Any()
               .Should()
               .BeFalse();

            specification
               .Current
               .Policies
               .Last()
               .SubPolicies
               .Any()
               .Should()
               .BeTrue();
        }
    }
}
