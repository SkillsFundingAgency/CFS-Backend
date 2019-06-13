using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Specs.Interfaces;
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

namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    public partial class SpecificationsServiceTests
    {
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
        public async Task CreateCalculation_GivenValidModelAndPolicyFoundButAddingCalcCausesBadRequest_ReturnsBadRequest()
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

            Specification specification = CreateSpecification();
            specification.Current.Policies = new[] { policy };
            specification.Current.FundingStreams = new List<Reference>()
            {
                new Reference {Id = FundingStreamId}
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

            ISearchRepository<SpecificationIndex> mockSearchRepository = CreateSearchRepository();
            mockSearchRepository
                .Index(Arg.Any<IEnumerable<SpecificationIndex>>())
                .Returns(new List<IndexError>());

            SpecificationVersion newSpecVersion = specification.Current.Clone() as SpecificationVersion;
            newSpecVersion.PublishStatus = PublishStatus.Updated;
            newSpecVersion.Version = 2;
            IVersionRepository<SpecificationVersion> mockVersionRepository = CreateVersionRepository();
            mockVersionRepository
                .CreateVersion(Arg.Any<SpecificationVersion>(), Arg.Any<SpecificationVersion>())
                .Returns(newSpecVersion);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository,
                mapper: mapper, messengerService: messengerService, specificationVersionRepository: mockVersionRepository, searchRepository: mockSearchRepository);


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

            await
                mockSearchRepository
                    .Received(1)
                    .Index(Arg.Is<IEnumerable<SpecificationIndex>>(
                        m => m.First().Id == SpecificationId &&
                             m.First().Status == newSpecVersion.PublishStatus.ToString()));

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

            Specification specification = CreateSpecification();
            specification.Current.Policies = new[] { policy };
            specification.Current.FundingStreams = new List<Reference>()
            {
                new Reference {Id = FundingStreamId}
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

            SpecificationVersion newSpecVersion = specification.Current.Clone() as SpecificationVersion;
            newSpecVersion.PublishStatus = PublishStatus.Updated;
            newSpecVersion.Version = 2;

            IVersionRepository<SpecificationVersion> mockVersionRepository = CreateVersionRepository();
            mockVersionRepository
                .CreateVersion(Arg.Any<SpecificationVersion>(), Arg.Any<SpecificationVersion>())
                .Returns(newSpecVersion);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository,
                mapper: mapper, messengerService: messengerService, specificationVersionRepository: mockVersionRepository);

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
        public void CreateCalculation_WhenSomethingGoesWrongDuringIndexing_ShouldThrowException()
        {
            //Arrange
            const string errorMessage = "Encountered 802 error code";

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

            Specification specification = CreateSpecification();
            specification.Current.Policies = new[] { policy };
            specification.Current.FundingStreams = new List<Reference>()
            {
                new Reference {Id = FundingStreamId}
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

            ISearchRepository<SpecificationIndex> mockSearchRepository = CreateSearchRepository();
            mockSearchRepository
                .Index(Arg.Any<IEnumerable<SpecificationIndex>>())
                .Returns(new List<IndexError>() { new IndexError() { ErrorMessage = errorMessage } });

            SpecificationVersion newSpecVersion = specification.Current.Clone() as SpecificationVersion;
            newSpecVersion.PublishStatus = PublishStatus.Updated;
            newSpecVersion.Version = 2;
            IVersionRepository<SpecificationVersion> mockVersionRepository = CreateVersionRepository();
            mockVersionRepository
                .CreateVersion(Arg.Any<SpecificationVersion>(), Arg.Any<SpecificationVersion>())
                .Returns(newSpecVersion);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository,
                mapper: mapper, messengerService: messengerService, specificationVersionRepository: mockVersionRepository, searchRepository: mockSearchRepository);


            //Act
            Func<Task<IActionResult>> createCalculation = async () => await service.CreateCalculation(request);

            //Assert
            createCalculation
                .Should()
                .Throw<ApplicationException>()
                .Which
                .Message
                .Should()
                .Be($"Could not index specification {specification.Current.Id} because: {errorMessage}");
        }

        [TestMethod]
        public async Task CreateCalculation_GivenDuplicateCalculationName_ReturnsBadRequest()
        {
            // Arrange
            Specification specification = CreateSpecification();

            CalculationCreateModel model = new CalculationCreateModel
            {
                SpecificationId = SpecificationId,
                PolicyId = PolicyId,
                Name = "calc1",
                CalculationType = CalculationType.Number
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            ClaimsPrincipal principal = new ClaimsPrincipal(new[]
            {
                new ClaimsIdentity(new []{ new Claim(ClaimTypes.Sid, UserId), new Claim(ClaimTypes.Name, Username) })
            });

            HttpContext context = Substitute.For<HttpContext>();
            context
                .User
                .Returns(principal);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            request
                .HttpContext
                .Returns(context);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            IValidator<CalculationCreateModel> validator = CreateCalculationValidator(new ValidationResult(new[] { new ValidationFailure("prop1", "any error") }));

            SpecificationsService service = CreateService(specificationsRepository: specificationsRepository, 
                calculationCreateModelValidator: validator);

            // Act
            IActionResult result = await service.CreateCalculation(request);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }
    }
}