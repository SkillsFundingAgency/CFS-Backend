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
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;
using System.Linq.Expressions;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Models;
using System.Net;
using System.Security.Claims;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Models.Specs.Messages;
using CalculateFunding.Models.Exceptions;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using Microsoft.Azure.ServiceBus;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Models.MappingProfiles;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CalculateFunding.Services.Specs.Services
{
    [TestClass]
    public class SpecificationsServiceTests
    {
        const string FundingStreamId = "YAPGG";
        const string SpecificationId = "ffa8ccb3-eb8e-4658-8b3f-f1e4c3a8f313";
        const string PolicyId = "dda8ccb3-eb8e-4658-8b3f-f1e4c3a8f322";
        const string AllocationLineId = "02a6eeaf-e1a0-476e-9cf9-8aa5d9129345";
        const string CalculationId = "22a6eeaf-e1a0-476e-9cf9-8aa6c51293433";
        const string FundingPeriodId = "18/19";
        const string SpecificationName = "Test Spec 001";
        const string PolicyName = "Test Policy 001";
        const string CalculationName = "Test Calc 001";
        const string Username = "test-user";
        const string UserId = "33d7a71b-f570-4425-801b-250b9129f3d3";
        const string SfaCorrelationId = "c625c3f9-6ce8-4f1f-a3a3-4611f1dc3881";
        const string RelationshipId = "cca8ccb3-eb8e-4658-8b3f-f1e4c3a8f419";
        const string yamlFile = "12345.yaml";

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
        public async Task GetSpecificationById_GivenSpecificationWasNotFound_ReturnsNotFound()
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

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository, logs: logger);

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
        public async Task GetSpecificationById_GivenSpecificationWasFound_ReturnsSuccess()
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

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetSpecificationById(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        public async Task GetSpecificationSummaryById_GivenSpecificationSummaryWasFoundAndIsInCache_ReturnsObject()
        {
            //Arrange
            SpecificationSummary specification = new SpecificationSummary()
            {
                Id = "spec-id",
                Name = "Spec Name",

                FundingStreams = new List<Reference>()
                    {
                         new Reference("fs1", "Funding Stream 1"),
                         new Reference("fs2", "Funding Stream 2"),
                    },
            
                Description = "Specification Description",
                FundingPeriod = new Reference("FP1", "Funding Period"),
            };

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

            IMapper mapper = CreateImplementedMapper();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<SpecificationSummary>(Arg.Is($"{CacheKeys.SpecificationSummaryById}{SpecificationId}"))
                .Returns(specification);

            SpecificationsService service = CreateService(
                specificationsRepository: specificationsRepository,
                cacheProvider: cacheProvider,
                logs: logger,
                mapper: mapper);

            //Act
            IActionResult result = await service.GetSpecificationSummaryById(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .ShouldBeEquivalentTo(new SpecificationSummary()
                {
                    Id = specification.Id,
                    Name = "Spec Name",
                    FundingStreams = new List<Reference>()
                    {
                         new Reference("fs1", "Funding Stream 1"),
                         new Reference("fs2", "Funding Stream 2"),
                    },
                    Description = "Specification Description",
                    FundingPeriod = new Reference("FP1", "Funding Period"),
                });

            await specificationsRepository
                .Received(0)
                .GetSpecificationById(Arg.Is(SpecificationId));

            await cacheProvider
                .Received(1)
                .GetAsync<SpecificationSummary>(Arg.Is($"{CacheKeys.SpecificationSummaryById}{SpecificationId}"));
        }

        [TestMethod]
        public async Task GetSpecificationSummaryById_GivenSpecificationSummaryWasFoundAndIsNotInCache_ReturnsObject()
        {
            //Arrange
            Specification specification = new Specification()
            {
                Id = "spec-id",
                Name = "Spec Name",
                Current = new SpecificationVersion()
                {
                    Name = "Spec name",
                    FundingStreams = new List<Reference>()
                    {
                         new Reference("fs1", "Funding Stream 1"),
                         new Reference("fs2", "Funding Stream 2"),
                    },
                    Author = new Reference("author@dfe.gov.uk", "Author Name"),
                    DataDefinitionRelationshipIds = new List<string>()
                       {
                           "dr1",
                           "dr2"
                       },
                    Description = "Specification Description",
                    FundingPeriod = new Reference("FP1", "Funding Period"),
                    PublishStatus = Models.Versioning.PublishStatus.Draft,
                    Version = 1,
                }
            };

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

            IMapper mapper = CreateImplementedMapper();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<SpecificationSummary>(Arg.Is($"{CacheKeys.SpecificationSummaryById}{SpecificationId}"))
                .Returns((SpecificationSummary)null);

            SpecificationsService service = CreateService(
                specificationsRepository: specificationsRepository,
                cacheProvider: cacheProvider,
                logs: logger,
                mapper: mapper);

            //Act
            IActionResult result = await service.GetSpecificationSummaryById(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .ShouldBeEquivalentTo(new SpecificationSummary()
                {
                    Id = specification.Id,
                    Name = "Spec Name",
                    FundingStreams = new List<Reference>()
                    {
                         new Reference("fs1", "Funding Stream 1"),
                         new Reference("fs2", "Funding Stream 2"),
                    },
                    Description = "Specification Description",
                    FundingPeriod = new Reference("FP1", "Funding Period"),
                });

            await specificationsRepository
                .Received(1)
                .GetSpecificationById(Arg.Is(SpecificationId));

            await cacheProvider
                .Received(1)
                .GetAsync<SpecificationSummary>(Arg.Is($"{CacheKeys.SpecificationSummaryById}{SpecificationId}"));
        }

        [TestMethod]
        public async Task GetSpecificationSummaryById_GivenSpecificationSummaryWasNotFound_ReturnsNotFoundResult()
        {
            //Arrange
            Specification specification = null;

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

            IMapper mapper = CreateImplementedMapper();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<SpecificationSummary>(Arg.Is($"{CacheKeys.SpecificationSummaryById}{SpecificationId}"))
                .Returns((SpecificationSummary)null);

            SpecificationsService service = CreateService(
                specificationsRepository: specificationsRepository,
                cacheProvider: cacheProvider,
                logs: logger,
                mapper: mapper);

            //Act
            IActionResult result = await service.GetSpecificationSummaryById(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            await specificationsRepository
                .Received(1)
                .GetSpecificationById(Arg.Is(SpecificationId));

            await cacheProvider
                .Received(1)
                .GetAsync<SpecificationSummary>(Arg.Is($"{CacheKeys.SpecificationSummaryById}{SpecificationId}"));
        }

        [TestMethod]
        public async Task GetSpecificationByFundingPeriodId_GivenNoSpecificationsReturned_ReturnsSuccess()
        {
            //Arrange
            IEnumerable<Specification> specs = Enumerable.Empty<Specification>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "fundingPeriodId", new StringValues(FundingPeriodId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationsByQuery(Arg.Any<Expression<Func<Specification, bool>>>())
                .Returns(specs);

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetSpecificationsByFundingPeriodId(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            IEnumerable<SpecificationSummary> objContent = (IEnumerable<SpecificationSummary>)((OkObjectResult)result).Value;

            objContent
                .Count()
                .Should()
                .Be(0);
        }

        [TestMethod]
        public async Task GetSpecificationByFundingPeriodId_GivenSpecificationsReturned_ReturnsSuccess()
        {
            //Arrange
            IEnumerable<Specification> specs = new[]
            {
                new Specification(),
                new Specification()
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "fundingPeriodId", new StringValues(FundingPeriodId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                 .GetSpecificationsByQuery(Arg.Any<Expression<Func<Specification, bool>>>())
                 .Returns(specs);

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetSpecificationsByFundingPeriodId(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            IEnumerable<SpecificationSummary> objContent = (IEnumerable<SpecificationSummary>)((OkObjectResult)result).Value;

            objContent
                .Count()
                .Should()
                .Be(2);

            logger
                .Received(1)
                .Information(Arg.Is($"Found {specs.Count()} specifications for academic year with id {FundingPeriodId}"));
        }

        [TestMethod]
        public async Task GetSpecificationByFundingPeriodId_GivenAcademicYearIdDoesNotExist_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.GetSpecificationsByFundingPeriodId(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Any<string>());
        }

        [TestMethod]
        public async Task GetSpecificationByName_GivenSpecificationNameDoesNotExist_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.GetSpecificationByName(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Any<string>());
        }

        [TestMethod]
        public async Task GetSpecificationByName_GivenSpecificationWasNotFound_ReturnsNotFoundt()
        {
            //Arrange
            IEnumerable<Specification> specs = Enumerable.Empty<Specification>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationName", new StringValues(SpecificationName) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                 .GetSpecificationsByQuery(Arg.Any<Expression<Func<Specification, bool>>>())
                 .Returns(specs);

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetSpecificationByName(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"Specification was not found for name: {SpecificationName}"));
        }

        [TestMethod]
        public async Task GetSpecificationByName_GivenSpecificationReturned_ReturnsSuccess()
        {
            //Arrange
            IEnumerable<Specification> specs = new[]
            {
                new Specification()
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationName", new StringValues(SpecificationName) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                 .GetSpecificationsByQuery(Arg.Any<Expression<Func<Specification, bool>>>())
                 .Returns(specs);

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetSpecificationByName(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            Specification objContent = (Specification)((OkObjectResult)result).Value;

            objContent
                .Should()
                .NotBeNull();

            logger
                .Received(1)
                .Information(Arg.Is($"Specification found for name: {SpecificationName}"));
        }

        [TestMethod]
        public async Task GetPolicyByName_GivenModelDoesNotContainASpecificationId_ReturnsBadRequest()
        {
            //Arrange
            PolicyGetModel model = new PolicyGetModel();
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.GetPolicyByName(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No specification id was provided to GetPolicyByName"));
        }

        [TestMethod]
        public async Task GetPolicyByName_GivenModelDoesNotContainAPolicyName_ReturnsBadRequest()
        {
            //Arrange
            PolicyGetModel model = new PolicyGetModel
            {
                SpecificationId = SpecificationId
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.GetPolicyByName(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No policy name was provided to GetPolicyByName"));
        }

        [TestMethod]
        public async Task GetPolicyByName_GivenSpecificationDoesNotExist_ReturnsPreConditionFailed()
        {
            //Arrange
            PolicyGetModel model = new PolicyGetModel
            {
                SpecificationId = SpecificationId,
                Name = PolicyName
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns((Specification)null);

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetPolicyByName(request);

            //Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = (StatusCodeResult)result;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(412);

            logger
                .Received(1)
                .Error(Arg.Is($"No specification was found for specification id {SpecificationId}"));
        }

        [TestMethod]
        public async Task GetPolicyByName_GivenSpecificationExistsAndPolicyExists_ReturnsSuccess()
        {
            //Arrange
            Specification spec = new Specification
            {
                Current = new SpecificationVersion()
                {
                    Policies = new[]
                    {
                        new Policy{ Name = PolicyName}
                    },
                },
            };

            PolicyGetModel model = new PolicyGetModel
            {
                SpecificationId = SpecificationId,
                Name = PolicyName
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(spec);

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetPolicyByName(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"A policy was found for specification id {SpecificationId} and name {PolicyName}"));
        }

        [TestMethod]
        public async Task GetPolicyByName_GivenSpecificationExistsAndPolicyDoesNotExist_ReturnsNotFound()
        {
            //Arrange
            Specification spec = new Specification()
            {
                Current = new SpecificationVersion(),
            };

            PolicyGetModel model = new PolicyGetModel
            {
                SpecificationId = SpecificationId,
                Name = PolicyName
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(spec);

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetPolicyByName(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"A policy was not found for specification id {SpecificationId} and name {PolicyName}"));
        }

        [TestMethod]
        public async Task GetCalculationByName_GivenModelDoesNotContainASpecificationId_ReturnsBadRequest()
        {
            //Arrange
            CalculationGetModel model = new CalculationGetModel();
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.GetCalculationByName(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No specification id was provided to GetCalculationByName"));
        }

        [TestMethod]
        public async Task GetCalculationByName_GivenModelDoesNotContainAcalculationName_ReturnsBadRequest()
        {
            //Arrange
            CalculationGetModel model = new CalculationGetModel
            {
                SpecificationId = SpecificationId
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.GetCalculationByName(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No calculation name was provided to GetCalculationByName"));
        }

        [TestMethod]
        public async Task GetCalculationByName_GivenSpecificationExistsAndCalculationExists_ReturnsSuccess()
        {
            //Arrange
            Calculation calc = new Calculation { Name = CalculationName };

            CalculationGetModel model = new CalculationGetModel
            {
                SpecificationId = SpecificationId,
                Name = CalculationName
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCalculationBySpecificationIdAndCalculationName(Arg.Is(SpecificationId), Arg.Is(CalculationName))
                .Returns(calc);

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetCalculationByName(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"A calculation was found for specification id {SpecificationId} and name {CalculationName}"));
        }

        [TestMethod]
        public async Task GetCalculationByName_GivenSpecificationExistsAndCalculationDoesNotExist_ReturnsNotFound()
        {
            //Arrange
            CalculationGetModel model = new CalculationGetModel
            {
                SpecificationId = SpecificationId,
                Name = CalculationName
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCalculationBySpecificationIdAndCalculationName(Arg.Is(SpecificationId), Arg.Is(CalculationName))
                .Returns((Calculation)null);

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository, logs: logger);

            //Act
            IActionResult result = await service.GetCalculationByName(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"A calculation was not found for specification id {SpecificationId} and name {CalculationName}"));
        }

        [TestMethod]
        public async Task CreateCalculation_GivenNullModelProvided_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.CreateCalculation(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error("Null calculation create model provided to CreateCalculation");
        }

        [TestMethod]
        public async Task CreateCalculation_GivenModelButModelIsNotValid_ReturnsBadRequest()
        {
            //Arrange
            CalculationCreateModel model = new CalculationCreateModel();

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ValidationResult validationResult = new ValidationResult(new[]{
                    new ValidationFailure("prop1", "any error")
                });

            IValidator<CalculationCreateModel> validator = CreateCalculationValidator(validationResult);

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger, calculationCreateModelValidator: validator);

            //Act
            IActionResult result = await service.CreateCalculation(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error("Invalid data was provided for CreateCalculation");
        }

        [TestMethod]
        public async Task CreateCalculation_GivenValidModelButSpecificationcannotBeFoundd_ReturnsPreconditionFailed()
        {
            //Arrange
            CalculationCreateModel model = new CalculationCreateModel
            {
                SpecificationId = SpecificationId
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns((Specification)null);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.CreateCalculation(request);

            //Assert
            result
                .Should()
                .BeOfType<PreconditionFailedResult>();

            logger
                .Received(1)
                .Warning($"Specification not found for specification id {SpecificationId}");
        }

        [TestMethod]
        public async Task CreateCalculation_GivenValidModelButNoPolicyFound_ReturnsPreconditionFailed()
        {
            //Arrange
            Specification specification = new Specification()
            {
                Current = new SpecificationVersion(),
            };

            CalculationCreateModel model = new CalculationCreateModel
            {
                SpecificationId = SpecificationId,
                PolicyId = PolicyId
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            Calculation calculation = new Calculation();
            IMapper mapper = CreateMapper();
            mapper
                .Map<Calculation>(model)
                .Returns(calculation);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository, mapper: mapper);

            //Act
            IActionResult result = await service.CreateCalculation(request);

            //Assert
            result
                .Should()
                .BeOfType<PreconditionFailedResult>()
                .Which
                .Value
                .Should()
                .Be("Policy not found for policy id 'dda8ccb3-eb8e-4658-8b3f-f1e4c3a8f322'");

            logger
                .Received(1)
                .Warning($"Policy not found for policy id '{PolicyId}'");
        }

        [TestMethod]
        public async Task CreateCalculation_GivenValidModelAndPolicyFoundButAddingCalcCausesBadRequest_AReturnsBadRequest()
        {
            //Arrange
            AllocationLine allocationLine = new AllocationLine
            {
                Id = "02a6eeaf-e1a0-476e-9cf9-8aa5d9129345",
                Name = "test alloctaion"
            };

            List<FundingStream> fundingStreams = new List<FundingStream>();

            FundingStream fundingStream = new FundingStream
            {
                AllocationLines = new List<AllocationLine>
                {
                    allocationLine
                },
                Id = FundingStreamId
            };

            fundingStreams.Add(fundingStream);

            Policy policy = new Policy
            {
                Id = PolicyId,
                Name = PolicyName,
            };

            Specification specification = new Specification
            {
                Current = new SpecificationVersion()
                {
                    Policies = new[]
                    {
                        policy,
                    },
                    FundingStreams = new List<Reference>()
                    {
                        new Reference { Id = FundingStreamId }
                    },
                },
            };

            CalculationCreateModel model = new CalculationCreateModel
            {
                SpecificationId = SpecificationId,
                PolicyId = PolicyId,
                AllocationLineId = AllocationLineId
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            specificationsRepository
               .GetFundingStreams(Arg.Any<Expression<Func<FundingStream, bool>>>())
               .Returns(fundingStreams);

            specificationsRepository
                .UpdateSpecification(Arg.Is(specification))
                .Returns(HttpStatusCode.BadRequest);

            Calculation calculation = new Calculation
            {
                AllocationLine = new Reference()
            };

            IMapper mapper = CreateMapper();
            mapper
                .Map<Calculation>(Arg.Any<CalculationCreateModel>())
                .Returns(calculation);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository, mapper: mapper);

            //Act
            IActionResult result = await service.CreateCalculation(request);

            //Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = (StatusCodeResult)result;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(400);

            logger
                .Received(1)
                .Error($"Failed to update specification when creating a calc with status BadRequest");
        }

        [TestMethod]
        public async Task CreateCalculation_GivenValidModelAndPolicyFoundAndUpdated_ReturnsOK()
        {
            //Arrange
            AllocationLine allocationLine = new AllocationLine
            {
                Id = "02a6eeaf-e1a0-476e-9cf9-8aa5d9129345",
                Name = "test alloctaion"
            };

            List<FundingStream> fundingStreams = new List<FundingStream>();

            FundingStream fundingStream = new FundingStream
            {
                AllocationLines = new List<AllocationLine>
                {
                    allocationLine
                },
                Id = FundingStreamId
            };

            fundingStreams.Add(fundingStream);

            Policy policy = new Policy
            {
                Id = PolicyId,
                Name = PolicyName,
            };

            Specification specification = new Specification
            {
                Current = new SpecificationVersion()
                {
                    Policies = new[]
                    {
                        policy,
                    },
                    FundingStreams = new List<Reference>()
                    {
                        new Reference { Id = FundingStreamId }
                    },
                },
            };

            CalculationCreateModel model = new CalculationCreateModel
            {
                SpecificationId = SpecificationId,
                PolicyId = PolicyId,
                AllocationLineId = AllocationLineId
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            ClaimsPrincipal principle = new ClaimsPrincipal(new[]
            {
                new ClaimsIdentity(new []{ new Claim(ClaimTypes.Sid, UserId), new Claim(ClaimTypes.Name, Username) })
            });

            HttpContext context = Substitute.For<HttpContext>();
            context
                .User
                .Returns(principle);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            request
                .HttpContext
                .Returns(context);

            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary
                .Add("sfa-correlationId", new StringValues(SfaCorrelationId));

            request
                .Headers
                .Returns(headerDictionary);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            specificationsRepository
                .GetFundingStreams(Arg.Any<Expression<Func<FundingStream, bool>>>())
                .Returns(fundingStreams);

            specificationsRepository
                .UpdateSpecification(Arg.Is(specification))
                .Returns(HttpStatusCode.OK);

            Calculation calculation = new Calculation
            {
                AllocationLine = new Reference()
            };

            IMapper mapper = CreateMapper();
            mapper
                .Map<Calculation>(Arg.Any<CalculationCreateModel>())
                .Returns(calculation);

            IMessengerService messengerService = CreateMessengerService();

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository, mapper: mapper, messengerService: messengerService);

            //Act
            IActionResult result = await service.CreateCalculation(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            await
                messengerService
                    .Received(1)
                    .SendToQueue(Arg.Is("calc-events-create-draft"),
                        Arg.Is<Models.Calcs.Calculation>(m =>
                            m.CalculationSpecification.Id == calculation.Id &&
                            m.CalculationSpecification.Name == calculation.Name &&
                            m.Name == calculation.Name &&
                            !string.IsNullOrEmpty(m.Id) &&
                            m.AllocationLine.Id == allocationLine.Id &&
                            m.AllocationLine.Name == allocationLine.Name),
                        Arg.Is<IDictionary<string, string>>(m =>
                            m["user-id"] == UserId &&
                            m["user-name"] == Username &&
                            m["sfa-correlationId"] == SfaCorrelationId));
        }

        [TestMethod]
        public async Task CreateCalculation_GivenValidModelForSubPolicyAndSubPolicyFoundAndUpdated_ReturnsOK()
        {
            //Arrange
            AllocationLine allocationLine = new AllocationLine
            {
                Id = "02a6eeaf-e1a0-476e-9cf9-8aa5d9129345",
                Name = "test alloctaion"
            };

            List<FundingStream> fundingStreams = new List<FundingStream>();

            FundingStream fundingStream = new FundingStream
            {
                AllocationLines = new List<AllocationLine>
                {
                    allocationLine
                },
                Id = FundingStreamId
            };

            fundingStreams.Add(fundingStream);

            Policy policy = new Policy
            {
                Id = PolicyId,
                Name = PolicyName,
            };

            Specification specification = new Specification
            {
                Current = new SpecificationVersion()
                {
                    Policies = new[]
                    {
                        policy,
                    },
                    FundingStreams = new List<Reference>()
                    {
                        new Reference { Id = FundingStreamId }
                    },
                },
            };

            CalculationCreateModel model = new CalculationCreateModel
            {
                SpecificationId = SpecificationId,
                PolicyId = PolicyId,
                AllocationLineId = AllocationLineId
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            ClaimsPrincipal principle = new ClaimsPrincipal(new[]
            {
                new ClaimsIdentity(new []{ new Claim(ClaimTypes.Sid, UserId), new Claim(ClaimTypes.Name, Username) })
            });

            HttpContext context = Substitute.For<HttpContext>();
            context
                .User
                .Returns(principle);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            request
                .HttpContext
                .Returns(context);

            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary
                .Add("sfa-correlationId", new StringValues(SfaCorrelationId));

            request
                .Headers
                .Returns(headerDictionary);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            specificationsRepository
               .GetFundingStreams(Arg.Any<Expression<Func<FundingStream, bool>>>())
               .Returns(fundingStreams);

            specificationsRepository
                .UpdateSpecification(Arg.Is(specification))
                .Returns(HttpStatusCode.OK);

            Calculation calculation = new Calculation
            {
                AllocationLine = new Reference()
            };

            IMapper mapper = CreateMapper();
            mapper
                .Map<Calculation>(Arg.Any<CalculationCreateModel>())
                .Returns(calculation);

            IMessengerService messengerService = CreateMessengerService();

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository, mapper: mapper, messengerService: messengerService);

            //Act
            IActionResult result = await service.CreateCalculation(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            await
                messengerService
                    .Received(1)
                    .SendToQueue(Arg.Is("calc-events-create-draft"),
                        Arg.Is<Models.Calcs.Calculation>(m =>
                            m.CalculationSpecification.Id == calculation.Id &&
                            m.CalculationSpecification.Name == calculation.Name &&
                            m.Name == calculation.Name &&
                            !string.IsNullOrEmpty(m.Id) &&
                            m.AllocationLine.Id == allocationLine.Id &&
                            m.AllocationLine.Name == allocationLine.Name),
                        Arg.Is<IDictionary<string, string>>(m =>
                            m["user-id"] == UserId &&
                            m["user-name"] == Username &&
                            m["sfa-correlationId"] == SfaCorrelationId));
        }

        [TestMethod]
        public async Task GetCalculationsBySpecificationId_GivenSpecificationIdDoesNotExist_ReturnsBadRequest()
        {
            // Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            // Act
            IActionResult result = await service.GetCalculationsBySpecificationId(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No specification Id was provided to GetCalculationsBySpecificationId"));
        }

        [TestMethod]
        public async Task GetCalculationsBySpecificationId_GivenNoCalculationsExist_ReturnsOK()
        {
            // Arrange
            IEnumerable<Calculation> calculations = Enumerable.Empty<Calculation>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            // Act
            IActionResult result = await service.GetCalculationsBySpecificationId(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Verbose(Arg.Is("Calculations were found for specification id {specificationId}"), Arg.Is(SpecificationId));
        }

        [TestMethod]
        public async Task GetCalculationsBySpecificationId_GivenCalculationsExist_ReturnsOK()
        {
            // Arrange
            List<Calculation> calculations = new List<Calculation>();
            Calculation calc = new Calculation()
            {
                Name = "Test Calc 1",
            };

            calculations.Add(calc);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations.AsEnumerable());

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            // Act
            IActionResult result = await service.GetCalculationsBySpecificationId(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which.Value.Should().BeAssignableTo<IEnumerable<Calculation>>()
                .Which.Should().HaveCount(calculations.Count);

            logger
                .Received(1)
                .Verbose(Arg.Is("Calculations were found for specification id {specificationId}"), Arg.Is(SpecificationId));
        }

        [TestMethod]
        public async Task GetCalculationsBySpecificationId_GivenNullResponseFromRepository_ReturnsNotFound()
        {
            // Arrange
            List<Calculation> calculations = null;

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            // Act
            IActionResult result = await service.GetCalculationsBySpecificationId(request);

            // Assert
            result
                .Should()
                .BeOfType<NotFoundObjectResult>()
                .Which.Value.Should().BeAssignableTo<string>()
                .Which.Should().Be("No calculations could be retrieved");

            logger
                .Received(1)
                .Error(Arg.Is("No calculations could be retrieved found for specification id {specificationId}"), Arg.Is(SpecificationId));
        }

        [TestMethod]
        public async Task GetCalculationBySpecificationIdAndCalculationId_GivenSpecificationIdDoesNotExist_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.GetCalculationBySpecificationIdAndCalculationId(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No specification Id was provided to GetCalculationBySpecificationIdAndCalculationId"));
        }

        [TestMethod]
        public async Task GetCalculationBySpecificationIdAndCalculationId_GivenCalculationIdDoesNotExist_ReturnsBadRequest()
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

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.GetCalculationBySpecificationIdAndCalculationId(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No calculation Id was provided to GetCalculationBySpecificationIdAndCalculationId"));
        }

        [TestMethod]
        public async Task GetCalculationBySpecificationIdAndCalculationId_GivenCalculationDoesNotExist_ReturnsNotFound()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
                { "calculationId", new StringValues(CalculationId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCalculationBySpecificationIdAndCalculationId(Arg.Is(SpecificationId), Arg.Is(CalculationId))
                .Returns((Calculation)null);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.GetCalculationBySpecificationIdAndCalculationId(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"A calculation was not found for specification id {SpecificationId} and calculation id {CalculationId}"));
        }

        [TestMethod]
        public async Task GetCalculationBySpecificationIdAndCalculationId_GivenCalculationDoesExist_ReturnsOK()
        {
            //Arrange
            Calculation calculation = new Calculation();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
                { "calculationId", new StringValues(CalculationId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetCalculationBySpecificationIdAndCalculationId(Arg.Is(SpecificationId), Arg.Is(CalculationId))
                .Returns(calculation);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.GetCalculationBySpecificationIdAndCalculationId(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"A calculation was found for specification id {SpecificationId} and calculation id {CalculationId}"));
        }

        [TestMethod]
        public void AssignDataDefinitionRelationship_GivenMessageWithNullRealtionshipObject_ThrowsArgumentNullException()
        {
            //Arrange
            Message message = new Message(new byte[0]);

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            Func<Task> test = async () => await service.AssignDataDefinitionRelationship(message);

            //Assert
            test
                .ShouldThrowExactly<ArgumentNullException>();

            logger
                .Received()
                .Error("A null relationship message was provided to AssignDataDefinitionRelationship");
        }

        [TestMethod]
        public void AssignDataDefinitionRelationship_GivenMessageWithObjectButDoesntValidate_ThrowsInvalidModelException()
        {
            //Arrange
            dynamic anyObject = new { something = 1 };

            string json = JsonConvert.SerializeObject(anyObject);


            Message message = new Message(Encoding.UTF8.GetBytes(json));


            ValidationResult validationResult = new ValidationResult(new[]{
                    new ValidationFailure("prop1", "any error")
                });

            IValidator<AssignDefinitionRelationshipMessage> validator = CreateAssignDefinitionRelationshipMessageValidator(validationResult);

            SpecificationsService service = CreateService(assignDefinitionRelationshipMessageValidator: validator);

            //Act
            Func<Task> test = async () => await service.AssignDataDefinitionRelationship(message);

            //Assert
            test
                .ShouldThrowExactly<InvalidModelException>();
        }

        [TestMethod]
        public void AssignDataDefinitionRelationship_GivenValidMessageButUnableToFindSpecification_ThrowsInvalidModelException()
        {
            //Arrange

            dynamic anyObject = new { specificationId = SpecificationId, relationshipId = RelationshipId };

            string json = JsonConvert.SerializeObject(anyObject);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns((Specification)null);

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository);

            //Act
            Func<Task> test = async () => await service.AssignDataDefinitionRelationship(message);

            //Assert
            test
                .ShouldThrowExactly<InvalidModelException>();
        }

        [TestMethod]
        public void AssignDataDefinitionRelationship_GivenFailedToUpdateSpecification_ThrowsException()
        {
            //Arrange
            dynamic anyObject = new { specificationId = SpecificationId, relationshipId = RelationshipId };

            string json = JsonConvert.SerializeObject(anyObject);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            Specification specification = new Specification()
            {
                Current = new SpecificationVersion(),
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            specificationsRepository
                .UpdateSpecification(Arg.Is(specification))
                .Returns(HttpStatusCode.InternalServerError);

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository, logs: logger);

            //Act
            Func<Task> test = async () => await service.AssignDataDefinitionRelationship(message);

            //Assert
            test
                .ShouldThrowExactly<Exception>();

            logger
                .Received()
                .Error($"Failed to update specification for id: {SpecificationId} with dataset definition relationship id {RelationshipId}");
        }

        [TestMethod]
        public void AssignDataDefinitionRelationship_GivenFailedToUpdateSearch_ThrowsFailedToIndexSearchException()
        {
            //Arrange
            dynamic anyObject = new { specificationId = SpecificationId, relationshipId = RelationshipId };

            string json = JsonConvert.SerializeObject(anyObject);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            Specification specification = new Specification
            {
                Id = SpecificationId,
                Name = SpecificationName,
                Current = new SpecificationVersion()
                {
                    FundingStreams = new List<Reference>() { new Reference("fs-id", "fs-name") },
                    FundingPeriod = new Reference("18/19", "2018/19"),
                },
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            specificationsRepository
                .UpdateSpecification(Arg.Is(specification))
                .Returns(HttpStatusCode.OK);

            IList<IndexError> errors = new List<IndexError> { new IndexError() };

            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Index(Arg.Any<List<SpecificationIndex>>())
                .Returns(errors);

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository, searchRepository: searchRepository);

            //Act
            Func<Task> test = async () => await service.AssignDataDefinitionRelationship(message);

            //Assert
            test
                .ShouldThrowExactly<FailedToIndexSearchException>();
        }

        [TestMethod]
        public async Task AssignDataDefinitionRelationship_GivenUpdatedCosmosAndSearch_LogsSuccess()
        {
            //Arrange
            dynamic anyObject = new { specificationId = SpecificationId, relationshipId = RelationshipId };

            string json = JsonConvert.SerializeObject(anyObject);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            Specification specification = new Specification
            {
                Id = SpecificationId,
                Name = SpecificationName,
                Current = new SpecificationVersion()
                {
                    FundingStreams = new List<Reference>() { new Reference("fs-id", "fs-name") },
                    FundingPeriod = new Reference("18/19", "2018/19"),
                },
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            specificationsRepository
                .UpdateSpecification(Arg.Is(specification))
                .Returns(HttpStatusCode.OK);

            IList<IndexError> errors = new List<IndexError>();

            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Index(Arg.Any<List<SpecificationIndex>>())
                .Returns(errors);

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository,
                searchRepository: searchRepository, logs: logger);

            //Act
            await service.AssignDataDefinitionRelationship(message);

            //Assert
            logger
                .Received(1)
                .Information($"Succeffuly assigned relationship id: {RelationshipId} to specification with id: {SpecificationId}");

            await
                searchRepository
                    .Received(1)
                    .Index(Arg.Is<IList<SpecificationIndex>>(
                        m => m.First().Id == SpecificationId &&
                        m.First().Name == SpecificationName &&
                        m.First().FundingStreamIds.First() == "fs-id" &&
                        m.First().FundingStreamNames.First() == "fs-name" &&
                        m.First().FundingPeriodId == "18/19" &&
                        m.First().FundingPeriodName == "2018/19" &&
                        m.First().LastUpdatedDate.Value.Date == DateTimeOffset.Now.Date));
        }

        [TestMethod]
        public async Task ReIndex_GivenDeleteIndexThrowsException_RetunsInternalServerError()
        {
            //Arrange
            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .When(x => x.DeleteIndex())
                .Do(x => { throw new Exception(); });

            ILogger logger = CreateLogger();

            ISpecificationsService service = CreateService(searchRepository: searchRepository, logs: logger);

            //Act
            IActionResult result = await service.ReIndex();

            //Assert
            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is("Failed re-indexing specifications"));

            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(500);
        }

        [TestMethod]
        public async Task ReIndex_GivenGetAllSpecificationDocumentsThrowsException_RetunsInternalServerError()
        {
            //Arrange
            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .When(x => x.GetSpecificationsByRawQuery<SpecificationSearchModel>(Arg.Any<string>()))
                .Do(x => { throw new Exception(); });

            ILogger logger = CreateLogger();

            ISpecificationsService service = CreateService(searchRepository: searchRepository, logs: logger,
                specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.ReIndex();

            //Assert
            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is("Failed re-indexing specifications"));

            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(500);

            await
                searchRepository
                    .DidNotReceive()
                    .Index(Arg.Any<List<SpecificationIndex>>());
        }

        [TestMethod]
        public async Task ReIndex_GivenIndexingThrowsException_RetunsInternalServerError()
        {
            //Arrange
            IEnumerable<SpecificationSearchModel> specifications = new[]
            {
                new SpecificationSearchModel
                {
                    Id = SpecificationId,
                    Name = SpecificationName,
                    FundingStreams = new List<Reference>() { new Reference("fs-id", "fs-name") },
                    FundingPeriod = new Reference("18/19", "2018/19"),
                    UpdatedAt = DateTime.Now
                }
            };

            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .When(x => x.Index(Arg.Any<List<SpecificationIndex>>()))
                .Do(x => { throw new Exception(); });

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationsByRawQuery<SpecificationSearchModel>(Arg.Any<string>())
                .Returns(specifications);

            ILogger logger = CreateLogger();

            ISpecificationsService service = CreateService(searchRepository: searchRepository, logs: logger,
                specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.ReIndex();

            //Assert
            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is("Failed re-indexing specifications"));

            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(500);
        }

        [TestMethod]
        public async Task ReIndex_GivenNoDocumentsReturnedFromCosmos_RetunsNoContent()
        {
            //Arrange
            IEnumerable<SpecificationSearchModel> specifications = new SpecificationSearchModel[0];

            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationsByRawQuery<SpecificationSearchModel>(Arg.Any<string>())
                .Returns(specifications);

            ILogger logger = CreateLogger();

            ISpecificationsService service = CreateService(searchRepository: searchRepository, logs: logger,
                specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.ReIndex();

            //Assert
            logger
                .Received(1)
                .Warning(Arg.Is("No specification documents were returned from cosmos db"));

            result
                .Should()
                .BeOfType<NoContentResult>();
        }

        [TestMethod]
        public async Task ReIndex_GivenDocumentsReturnedFromCosmos_RetunsNoContent()
        {
            //Arrange
            IEnumerable<SpecificationSearchModel> specifications = new[]
            {
                new SpecificationSearchModel
                {
                    Id = SpecificationId,
                    Name = SpecificationName,
                    FundingStreams = new List<Reference>() { new Reference("fs-id", "fs-name") },
                    FundingPeriod = new Reference("18/19", "2018/19"),
                    UpdatedAt = DateTime.Now
                }
            };

            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationsByRawQuery<SpecificationSearchModel>(Arg.Any<string>())
                .Returns(specifications);

            ILogger logger = CreateLogger();

            ISpecificationsService service = CreateService(searchRepository: searchRepository, logs: logger,
                specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.ReIndex();

            //Assert
            logger
                .Received(1)
                .Information(Arg.Is($"Succesfully re-indexed 1 documents"));

            result
                .Should()
                .BeOfType<NoContentResult>();
        }

        [TestMethod]
        async public Task SaveFundingStream_GivenNoYamlWasProvidedWithNoFileName_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.SaveFundingStream(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is($"Null or empty yaml provided for file: File name not provided"));
        }

        [TestMethod]
        async public Task SaveFundingStream_GivenNoYamlWasProvidedButFileNameWas_ReturnsBadRequest()
        {
            //Arrange
            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary
                .Add("yaml-file", new StringValues(yamlFile));

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Headers
                .Returns(headerDictionary);

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.SaveFundingStream(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is($"Null or empty yaml provided for file: {yamlFile}"));
        }

        [TestMethod]
        async public Task SaveFundingStream_GivenNoYamlWasProvidedButIsInvalid_ReturnsBadRequest()
        {
            //Arrange
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

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.SaveFundingStream(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is($"Invalid yaml was provided for file: {yamlFile}"));
        }

        [TestMethod]
        async public Task SaveFundingStream_GivenValidYamlButFailedToSaveToDatabase_ReturnsStatusCode()
        {
            //Arrange
            string yaml = CreateRawFundingStream();
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

            HttpStatusCode failedCode = HttpStatusCode.BadGateway;

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .SaveFundingStream(Arg.Any<FundingStream>())
                .Returns(failedCode);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.SaveFundingStream(request);

            //Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = (StatusCodeResult)result;
            statusCodeResult
                .StatusCode
                .Should()
                .Be(502);

            logger
                .Received(1)
                .Error(Arg.Is($"Failed to save yaml file: {yamlFile} to cosmos db with status 502"));
        }

        [TestMethod]
        async public Task SaveFundingStream_GivenValidYamlButSavingToDatabaseThrowsException_ReturnsInternalServerError()
        {
            //Arrange
            string yaml = CreateRawFundingStream();
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
                .When(x => x.SaveFundingStream(Arg.Any<FundingStream>()))
                .Do(x => { throw new Exception(); });

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.SaveFundingStream(request);

            //Assert
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
        async public Task SaveFundingStream_GivenValidYamlAndSaveWasSuccesful_ReturnsOK()
        {
            //Arrange
            string yaml = CreateRawFundingStream();
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

            HttpStatusCode statusCode = HttpStatusCode.Created;

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .SaveFundingStream(Arg.Any<FundingStream>())
                .Returns(statusCode);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.SaveFundingStream(request);

            //Assert
            result
                .Should()
                .BeOfType<OkResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"Successfully saved file: {yamlFile} to cosmos db"));
        }

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
        public async Task GetFundingStreams_GivenNullOrEmptyFundingStreamsReturned_LogsAndReturnsOKWithEmptyList()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            IEnumerable<FundingStream> fundingStreams = null;

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetFundingStreams()
                .Returns(fundingStreams);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.GetFundingStreams(request);

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
            HttpRequest request = Substitute.For<HttpRequest>();

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
            IActionResult result = await service.GetFundingStreams(request);

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

        [TestMethod]
        async public Task SaveFundingPeriods_GivenNoYamlWasProvidedWithNoFileName_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.SaveFundingPeriods(request);

            //Assert
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
            //Arrange
            IHeaderDictionary headerDictionary = new HeaderDictionary();
            headerDictionary
                .Add("yaml-file", new StringValues(yamlFile));

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Headers
                .Returns(headerDictionary);

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.SaveFundingPeriods(request);

            //Assert
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
            //Arrange
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

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.SaveFundingPeriods(request);

            //Assert
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
            //Arrange
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
                .When(x => x.SaveFundingPeriods(Arg.Any<FundingPeriod[]>()))
                .Do(x => { throw new Exception(); });

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.SaveFundingPeriods(request);

            //Assert
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
            //Arrange
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

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await service.SaveFundingPeriods(request);

            //Assert
            result
                .Should()
                .BeOfType<OkResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"Successfully saved file: {yamlFile} to cosmos db"));

            await
                cacheProvider
                .Received(1)
                .SetAsync<FundingPeriod[]>(Arg.Is(CacheKeys.FundingPeriods), Arg.Any<FundingPeriod[]>(), Arg.Any<TimeSpan>(), Arg.Is(true));
        }

        [TestMethod]
        public async Task GetFundingPeriods_GivenFundingPeriodsInCache_ReturnsFromCache()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            FundingPeriod[] fundingPeriods = {
                new FundingPeriod(),
                new FundingPeriod()
            };

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<FundingPeriod[]>(Arg.Is(CacheKeys.FundingPeriods))
                .Returns(fundingPeriods);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();

            SpecificationsService specificationsService = CreateService(specificationsRepository: specificationsRepository, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await specificationsService.GetFundingPeriods(request);

            result
                .Should()
                .BeOfType<OkObjectResult>();

            await
                specificationsRepository
                .DidNotReceive()
                .GetFundingPeriods();
        }

        [TestMethod]
        public async Task GetFundingPeriods_GivenFundingPeriodsNotInCacheAndFailedToGetFromCosomos_ReturnsInternalServerError()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<FundingPeriod[]>(Arg.Is(CacheKeys.FundingPeriods))
                .Returns((FundingPeriod[])null);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();

            SpecificationsService specificationsService = CreateService(specificationsRepository: specificationsRepository, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await specificationsService.GetFundingPeriods(request);

            result
                .Should()
                .BeOfType<InternalServerErrorResult>();

            await
                cacheProvider
                    .DidNotReceive()
                    .SetAsync<FundingPeriod[]>(Arg.Any<string>(), Arg.Any<FundingPeriod[]>(), Arg.Any<TimeSpan>(), Arg.Any<bool>());
        }

        [TestMethod]
        public async Task GetFundingPeriods_GivenFundingPeriodsNotInCacheButGetsFromCosmos_ReturnsFundingPeriods()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            FundingPeriod[] fundingPeriods = {
                new FundingPeriod(),
                new FundingPeriod()
            };

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<FundingPeriod[]>(Arg.Is(CacheKeys.FundingPeriods))
                .Returns((FundingPeriod[])null);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetFundingPeriods()
                .Returns(fundingPeriods);

            SpecificationsService specificationsService = CreateService(specificationsRepository: specificationsRepository, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await specificationsService.GetFundingPeriods(request);

            result
                .Should()
                .BeOfType<OkObjectResult>();

            await
                cacheProvider
                    .Received(1)
                    .SetAsync<FundingPeriod[]>(Arg.Any<string>(), Arg.Any<FundingPeriod[]>(), Arg.Any<TimeSpan>(), Arg.Any<bool>());
        }

        [TestMethod]
        public async Task SpecificationsService_CreateSpecification_WhenValidInputProvided_ThenSpecificationIsCreated()
        {
            // Arrange
            const string fundingStreamId = "fs1";
            const string fundingPeriodId = "fp1";


            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();

            IMapper mapper = CreateImplementedMapper();

            SpecificationsService specificationsService = CreateService(
                specificationsRepository: specificationsRepository,
                searchRepository: searchRepository,
                mapper: mapper);

            SpecificationCreateModel specificationCreateModel = new SpecificationCreateModel()
            {
                Name = "Specification Name",
                Description = "Specification Description",
                FundingPeriodId = "fp1",
                FundingStreamIds = new List<string>() { fundingStreamId },
            };

            string json = JsonConvert.SerializeObject(specificationCreateModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            ClaimsPrincipal principle = new ClaimsPrincipal(new[]
            {
                new ClaimsIdentity(new []{ new Claim(ClaimTypes.Sid, UserId), new Claim(ClaimTypes.Name, Username) })
            });

            HttpContext context = Substitute.For<HttpContext>();
            context
                .User
                .Returns(principle);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            request
                .HttpContext
                .Returns(context);

            specificationsRepository
                .GetSpecificationByQuery(Arg.Any<Expression<Func<Specification, bool>>>())
                .Returns((Specification)null);

            FundingPeriod fundingPeriod = new FundingPeriod()
            {
                Id = fundingPeriodId,
                Name = "Funding Period 1",
                Type = "Test",
            };

            specificationsRepository
                .GetFundingPeriodById(Arg.Is(fundingPeriodId))
                .Returns(fundingPeriod);

            FundingStream fundingStream = new FundingStream()
            {
                Id = fundingStreamId,
                Name = "Funding Stream 1",
                AllocationLines = new List<AllocationLine>(),
            };

            specificationsRepository
                .GetFundingStreamById(Arg.Is(fundingStreamId))
                .Returns(fundingStream);

            specificationsRepository
                .CreateSpecification(Arg.Is<Specification>(
                    s => s.Name == specificationCreateModel.Name &&
                    s.Current.Description == specificationCreateModel.Description &&
                    s.Current.FundingPeriod.Id == fundingPeriodId))
                .Returns(HttpStatusCode.Created);

            // Act
            IActionResult result = await specificationsService.CreateSpecification(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<Specification>()
                .And
                .NotBeNull();

            await specificationsRepository
                .Received(1)
                .CreateSpecification(Arg.Is<Specification>(
                   s => s.Name == specificationCreateModel.Name &&
                   s.Current.Description == specificationCreateModel.Description &&
                   s.Current.FundingPeriod.Id == fundingPeriodId));

            await searchRepository
                .Received(1)
                .Index(Arg.Is<List<SpecificationIndex>>(c =>
                c.Count() == 1 &&
                !string.IsNullOrWhiteSpace(c.First().Id) &&
                c.First().Name == specificationCreateModel.Name
                ));
        }

        [TestMethod]
        public async Task SpecificationsService_CreateSpecification_WhenFundingStreamIDIsProvidedButDoesNotExist_ThenPreConditionFailedReturned()
        {
            // Arrange
            const string fundingStreamId = "fs1";
            const string fundingStreamNotFoundId = "notfound";

            const string fundingPeriodId = "fp1";


            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();

            IMapper mapper = CreateImplementedMapper();

            SpecificationsService specificationsService = CreateService(
                specificationsRepository: specificationsRepository,
                searchRepository: searchRepository,
                mapper: mapper);

            SpecificationCreateModel specificationCreateModel = new SpecificationCreateModel()
            {
                Name = "Specification Name",
                Description = "Specification Description",
                FundingPeriodId = "fp1",
                FundingStreamIds = new List<string>() { fundingStreamId, fundingStreamNotFoundId, },
            };

            string json = JsonConvert.SerializeObject(specificationCreateModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            ClaimsPrincipal principle = new ClaimsPrincipal(new[]
            {
                new ClaimsIdentity(new []{ new Claim(ClaimTypes.Sid, UserId), new Claim(ClaimTypes.Name, Username) })
            });

            HttpContext context = Substitute.For<HttpContext>();
            context
                .User
                .Returns(principle);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            request
                .HttpContext
                .Returns(context);

            specificationsRepository
                .GetSpecificationByQuery(Arg.Any<Expression<Func<Specification, bool>>>())
                .Returns((Specification)null);

            FundingPeriod fundingPeriod = new FundingPeriod()
            {
                Id = fundingPeriodId,
                Name = "Funding Period 1",
                Type = "Test",
            };

            specificationsRepository
                .GetFundingPeriodById(Arg.Is(fundingPeriodId))
                .Returns(fundingPeriod);

            FundingStream fundingStream = new FundingStream()
            {
                Id = fundingStreamId,
                Name = "Funding Stream 1",
                AllocationLines = new List<AllocationLine>(),
            };

            specificationsRepository
                .GetFundingStreamById(Arg.Is(fundingStreamId))
                .Returns(fundingStream);

            specificationsRepository
                .GetFundingStreamById(Arg.Is(fundingStreamNotFoundId))
                .Returns((FundingStream)null);

            // Act
            IActionResult result = await specificationsService.CreateSpecification(request);

            // Assert
            result
                .Should()
                .BeOfType<PreconditionFailedResult>()
                .Which
                .Value
                .Should()
                .Be("Unable to find funding stream with ID 'notfound'.");

            await specificationsRepository
                .Received(1)
                .GetFundingStreamById(Arg.Is(fundingStreamNotFoundId));

            await specificationsRepository
                .Received(1)
                .GetFundingStreamById(Arg.Is(fundingStreamId));
        }

        [TestMethod]
        public async Task SpecificationsService_CreateSpecification_WhenInvalidInputProvided_ThenValidationErrorReturned()
        {
            // Arrange
            ValidationResult validationResult = new ValidationResult();
            validationResult.Errors.Add(new ValidationFailure("fundingStreamId", "Test"));

            IValidator<SpecificationCreateModel> validator = CreateSpecificationValidator(validationResult);

            SpecificationsService specificationsService = CreateService(specificationCreateModelvalidator: validator);

            SpecificationCreateModel specificationCreateModel = new SpecificationCreateModel()
            {
                Name = "Specification Name",
                Description = "Specification Description",
                FundingPeriodId = null,
                FundingStreamIds = new List<string>() { },
            };

            string json = JsonConvert.SerializeObject(specificationCreateModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            request
                .HttpContext
                .Returns(context);

            // Act
            IActionResult result = await specificationsService.CreateSpecification(request);

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

            await validator
                .Received(1)
                .ValidateAsync(Arg.Any<SpecificationCreateModel>());
        }

        [TestMethod]
        public async Task SpecificationsService_GetFundingStreamsForSpecificationById_WhenRequestingASpecificationWhenExists_ThenFundingStreamsReturned()
        {
            // Arrange
            const string specificationId = "spec1";

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            SpecificationsService specificationsService = CreateService(specificationsRepository: specificationsRepository);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            Specification specification = new Specification()
            {
                Id = specificationId,
                Name = "Test Specification",
                Current = new SpecificationVersion()
                {
                    FundingStreams = new List<Reference>()
                    {
                       new Reference("fs1", "Funding Stream 1"),
                       new Reference("fs2", "Funding Stream Two"),
                    },
                },
            };

            specificationsRepository
                .GetSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            List<FundingStream> fundingStreams = new List<FundingStream>()
            {
                new FundingStream()
                {
                    Id = "fs1",
                    Name = "Funding Stream 1",
                    AllocationLines = new List<AllocationLine>(),
                },
                new FundingStream()
                {
                    Id = "fs2",
                    Name = "Funding Stream Two",
                    AllocationLines = new List<AllocationLine>(),
                }
            };

            specificationsRepository
                .GetFundingStreams(Arg.Any<Expression<Func<FundingStream, bool>>>())
                .Returns(fundingStreams);

            // Act
            IActionResult actionResult = await specificationsService.GetFundingStreamsForSpecificationById(request);

            // Assert

            List<FundingStream> expectedFundingStreams = new List<FundingStream>()
            {
                new FundingStream()
                {
                    Id = "fs1",
                    Name = "Funding Stream 1",
                    AllocationLines = new List<AllocationLine>(),
                },
                new FundingStream()
                {
                    Id = "fs2",
                    Name = "Funding Stream Two",
                    AllocationLines = new List<AllocationLine>(),
                }
            };

            actionResult
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .ShouldBeEquivalentTo(expectedFundingStreams.AsEnumerable());

            await specificationsRepository
                .Received(1)
                .GetSpecificationById(Arg.Is(specificationId));

            await specificationsRepository
                .Received(1)
                .GetFundingStreams(Arg.Any<Expression<Func<FundingStream, bool>>>());
        }

        [TestMethod]
        public async Task SpecificationsService_GetFundingStreamsForSpecificationById_WhenRequestingASpecificationWhenDoesNotExist_ThenErrorReturned()
        {
            // Arrange
            const string specificationId = "spec1";

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            SpecificationsService specificationsService = CreateService(specificationsRepository: specificationsRepository);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            // Act
            IActionResult actionResult = await specificationsService.GetFundingStreamsForSpecificationById(request);

            // Assert

            List<FundingStream> expectedFundingStreams = new List<FundingStream>();

            actionResult
                .Should()
                .BeOfType<PreconditionFailedResult>()
                .Which
                .Value
                .Should()
                .Be("Specification not found");
        }

        [TestMethod]
        public async Task SpecificationsService_GetFundingStreamsForSpecificationById_WhenHasMissingSpecificationId_ThenErrorReturned()
        {
            // Arrange
            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            SpecificationsService specificationsService = CreateService(specificationsRepository: specificationsRepository);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(string.Empty) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            // Act
            IActionResult actionResult = await specificationsService.GetFundingStreamsForSpecificationById(request);

            // Assert

            List<FundingStream> expectedFundingStreams = new List<FundingStream>();

            actionResult
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty specificationId provided");
        }

        [TestMethod]
        public async Task SpecificationsService_GetFundingStreamsForSpecificationById_WhenFundingStreamNotFoundInReferencedSpecification_ThenErrorReturned()
        {
            // Arrange
            const string specificationId = "spec1";

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            SpecificationsService specificationsService = CreateService(specificationsRepository: specificationsRepository);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            Specification specification = new Specification()
            {
                Id = specificationId,
                Name = "Test Specification",
                Current = new SpecificationVersion()
                {
                    FundingStreams = new List<Reference>()
                    {
                       new Reference("fs1", "Funding Stream 1"),
                       new Reference("fs2", "Funding Stream Two"),
                    },
                },
            };

            specificationsRepository
                .GetSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            List<FundingStream> fundingStreams = new List<FundingStream>();

            specificationsRepository
                .GetFundingStreams(Arg.Any<Expression<Func<FundingStream, bool>>>())
                .Returns(fundingStreams);

            // Act
            IActionResult actionResult = await specificationsService.GetFundingStreamsForSpecificationById(request);

            // Assert
            actionResult
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be("No funding stream were returned");

            await specificationsRepository
                .Received(1)
                .GetSpecificationById(Arg.Is(specificationId));

            await specificationsRepository
                .Received(1)
                .GetFundingStreams(Arg.Any<Expression<Func<FundingStream, bool>>>());
        }

        [TestMethod]
        public async Task SpecificationsService_GetFundingStreamsForSpecificationById_WhenSpecificationContainsNoFundingStreams_ThenErrorReturned()
        {
            // Arrange
            const string specificationId = "spec1";

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            SpecificationsService specificationsService = CreateService(specificationsRepository: specificationsRepository);

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            Specification specification = new Specification()
            {
                Id = specificationId,
                Name = "Test Specification",
                Current = new SpecificationVersion()
                {
                    FundingStreams = new List<Reference>() { },
                },
            };

            specificationsRepository
                .GetSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            List<FundingStream> fundingStreams = new List<FundingStream>();

            specificationsRepository
                .GetFundingStreams(Arg.Any<Expression<Func<FundingStream, bool>>>())
                .Returns(fundingStreams);

            // Act
            IActionResult actionResult = await specificationsService.GetFundingStreamsForSpecificationById(request);

            // Assert
            actionResult
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be("Specification contains no funding streams");

            await specificationsRepository
                .Received(1)
                .GetSpecificationById(Arg.Is(specificationId));

            await specificationsRepository
                .Received(0)
                .GetFundingStreams(Arg.Any<Expression<Func<FundingStream, bool>>>());
        }

        [TestMethod]
        public async Task EditSpecification_GivenNoSpecificationIdWasProvided_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.EditSpecification(request);

            //Arrange
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No specification Id was provided to EditSpecification"));
        }

        [TestMethod]
        public async Task EditSpecification_GivenNullEditModeldWasProvided_ReturnsBadRequest()
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
            IActionResult result = await service.EditSpecification(request);

            //Arrange
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No edit modeld was provided to EditSpecification"));
        }

        [TestMethod]
        public async Task EditSpecification_WhenInvalidModelProvided_ThenValidationErrorReturned()
        {
            // Arrange
            ValidationResult validationResult = new ValidationResult();
            validationResult.Errors.Add(new ValidationFailure("error", "error"));

            IValidator<SpecificationEditModel> validator = CreateEditSpecificationValidator(validationResult);

            SpecificationsService specificationsService = CreateService(specificationEditModelValidator: validator);

            SpecificationEditModel specificationEditModel = new SpecificationEditModel();
           
            string json = JsonConvert.SerializeObject(specificationEditModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
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

            // Act
            IActionResult result = await specificationsService.EditSpecification(request);

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

            await validator
                .Received(1)
                .ValidateAsync(Arg.Any<SpecificationEditModel>());
        }

        [TestMethod]
        public async Task EditSpecification_GivenSpecificationWasNotFound_ReturnsNotFoundResult()
        {
            //Arrange
            SpecificationEditModel specificationEditModel = new SpecificationEditModel();

            string json = JsonConvert.SerializeObject(specificationEditModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) },
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

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns((Specification)null);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.EditSpecification(request);

            //Arrange
            result
                .Should()
                .BeOfType<NotFoundObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Specification not found");

            logger
                .Received(1)
                .Warning(Arg.Is($"Failed to find specification for id: {SpecificationId}"));
        }




        static SpecificationsService CreateService(
            IMapper mapper = null,
            ISpecificationsRepository specificationsRepository = null,
            ILogger logs = null,
            IValidator<PolicyCreateModel> policyCreateModelValidator = null,
            IValidator<SpecificationCreateModel> specificationCreateModelvalidator = null,
            IValidator<CalculationCreateModel> calculationCreateModelValidator = null,
            IMessengerService messengerService = null, ServiceBusSettings EventHubSettings = null,
            ISearchRepository<SpecificationIndex> searchRepository = null,
            IValidator<AssignDefinitionRelationshipMessage> assignDefinitionRelationshipMessageValidator = null,
            ICacheProvider cacheProvider = null,
            IValidator<SpecificationEditModel> specificationEditModelValidator = null)
        {
            return new SpecificationsService(mapper ?? CreateMapper(),
                specificationsRepository ?? CreateSpecificationsRepository(),
                logs ?? CreateLogger(),
                policyCreateModelValidator ?? CreatePolicyValidator(),
                specificationCreateModelvalidator ?? CreateSpecificationValidator(),
                calculationCreateModelValidator ?? CreateCalculationValidator(),
                messengerService ?? CreateMessengerService(),
                EventHubSettings ?? CreateEventHubSettings(),
                searchRepository ?? CreateSearchRepository(),
                assignDefinitionRelationshipMessageValidator ?? CreateAssignDefinitionRelationshipMessageValidator(),
                cacheProvider ?? CreateCacheProvider(),
                specificationEditModelValidator ?? CreateEditSpecificationValidator());
        }

        static IMapper CreateMapper()
        {
            return Substitute.For<IMapper>();
        }

        private static IMapper CreateImplementedMapper()
        {
            MapperConfiguration mappingConfiguration = new MapperConfiguration(c => c.AddProfile<SpecificationsMappingProfile>());
            IMapper mapper = mappingConfiguration.CreateMapper();
            return mapper;
        }

        static IMessengerService CreateMessengerService()
        {
            return Substitute.For<IMessengerService>();
        }

        static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        static ISpecificationsRepository CreateSpecificationsRepository()
        {
            return Substitute.For<ISpecificationsRepository>();
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static ServiceBusSettings CreateEventHubSettings()
        {
            return new ServiceBusSettings
            {
            };
        }

        static ISearchRepository<SpecificationIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<SpecificationIndex>>();
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

        static IValidator<SpecificationEditModel> CreateEditSpecificationValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
                validationResult = new ValidationResult();

            IValidator<SpecificationEditModel> validator = Substitute.For<IValidator<SpecificationEditModel>>();

            validator
               .ValidateAsync(Arg.Any<SpecificationEditModel>())
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

        static IValidator<AssignDefinitionRelationshipMessage> CreateAssignDefinitionRelationshipMessageValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
                validationResult = new ValidationResult();

            IValidator<AssignDefinitionRelationshipMessage> validator = Substitute.For<IValidator<AssignDefinitionRelationshipMessage>>();

            validator
               .ValidateAsync(Arg.Any<AssignDefinitionRelationshipMessage>())
               .Returns(validationResult);

            return validator;
        }

        static string CreateRawFundingStream()
        {
            var yaml = new StringBuilder();

            yaml.AppendLine(@"id: YPLRE");
            yaml.AppendLine(@"name: School Budget Share");
            yaml.AppendLine(@"allocationLines:");
            yaml.AppendLine(@"- id: YPE01");
            yaml.AppendLine(@"  name: School Budget Share");
            yaml.AppendLine(@"- id: YPE02");
            yaml.AppendLine(@"  name: Education Services Grant");
            yaml.AppendLine(@"- id: YPE03");
            yaml.AppendLine(@"  name: Insurance");
            yaml.AppendLine(@"- id: YPE04");
            yaml.AppendLine(@"  name: Teacher Threshold");
            yaml.AppendLine(@"- id: YPE05");
            yaml.AppendLine(@"  name: Mainstreamed Grants");
            yaml.AppendLine(@"- id: YPE06");
            yaml.AppendLine(@"  name: Start Up Grant Part a");
            yaml.AppendLine(@"- id: YPE07");
            yaml.AppendLine(@"  name: Start Up Grant Part b Formulaic");


            return yaml.ToString();
        }

        static string CreateRawFundingPeriods()
        {
            var yaml = new StringBuilder();

            yaml.AppendLine(@"fundingPeriods:");
            yaml.AppendLine(@"- type: AY");
            yaml.AppendLine(@"  id: AY2017181");
            yaml.AppendLine(@"  name: Academic 2017/18");
            yaml.AppendLine(@"  startDate: 09/01/2017 00:00:00");
            yaml.AppendLine(@"  endDate: 08/31/2018 00:00:00");
            yaml.AppendLine(@"- type: AY");
            yaml.AppendLine(@"  id: AY2018191");
            yaml.AppendLine(@"  name: Academic 2018/19");
            yaml.AppendLine(@"  startDate: 09/01/2018 00:00:00");
            yaml.AppendLine(@"  endDate: 08/31/2019 00:00:00");
            yaml.AppendLine(@"- type: FY");
            yaml.AppendLine(@"  id: FY2017181");
            yaml.AppendLine(@"  name: Financial 2017/18");
            yaml.AppendLine(@"  startDate: 04/01/2017 00:00:00");
            yaml.AppendLine(@"  endDate: 03/31/2018 00:00:00");
            yaml.AppendLine(@"- type: AY");
            yaml.AppendLine(@"  id: AY2018191");
            yaml.AppendLine(@"  name: Financial 2018/19");
            yaml.AppendLine(@"  startDate: 04/01/2018 00:00:00");
            yaml.AppendLine(@"  endDate: 03/31/2019 00:00:00");

            return yaml.ToString();
        }
    }
}
