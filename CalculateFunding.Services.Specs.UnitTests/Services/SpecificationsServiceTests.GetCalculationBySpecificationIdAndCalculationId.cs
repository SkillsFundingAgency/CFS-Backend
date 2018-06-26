using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using Serilog;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;
using AutoMapper;
using CalculateFunding.Services.Core.Extensions;

namespace CalculateFunding.Services.Specs.Services
{
    public partial class SpecificationsServiceTests
    {

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

            Specification specification = new Specification()
            {
                Current = new SpecificationVersion()
                {
                    Policies = new List<Policy>()
                    {
                        new Policy()
                        {
                            Id = "pol1",
                            Name = "Policy 1",
                        },
                        new Policy()
                        {
                            Id="pol2",
                            Name = "Pol2",
                            Calculations = new List<Calculation>(),
                        }
                    }
                }
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.GetCalculationBySpecificationIdAndCalculationId(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Calculation not found");

            logger
                .Received(1)
                .Information(Arg.Is($"A calculation was not found for specification id {SpecificationId} and calculation id {CalculationId}"));

            await specificationsRepository
                 .Received(1)
                 .GetSpecificationById(Arg.Is(SpecificationId));
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

            Specification specification = new Specification()
            {
                Current = new SpecificationVersion()
                {
                    Policies = new List<Policy>()
                    {
                        new Policy()
                        {
                            Id = "emptyPolicy",
                                Name = "Empty Calculations Policy",
                        },
                        new Policy()
                        {
                            Id = "pol1",
                            Name = "Policy 1",
                            Calculations = new List<Calculation>()
                            {
                                new Calculation()
                                {
                                    Id = "calc2",
                                    Name = "Calc On Policy 1",
                                    CalculationType = CalculationType.Number,
                                    Description = "Testing",
                                    AllocationLine = new Models.Reference("al2", "Allocation Line 2"),
                                }
                            },
                        },
                        new Policy()
                        {
                            Id="pol2",
                            Name = "Pol2",
                            Calculations = new List<Calculation>()
                            {
                                new Calculation()
                                {
                                    Id = CalculationId,
                                    Name = "Calc Name",
                                    CalculationType =CalculationType.Funding,
                                    Description = "Test",
                                    AllocationLine = new Models.Reference("al1", "Allocation Line 1"),
                                }
                            }
                        }
                    }
                }
            };

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            IMapper mapper = CreateImplementedMapper();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository, mapper: mapper);

            // Act
            IActionResult result = await service.GetCalculationBySpecificationIdAndCalculationId(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<CalculationCurrentVersion>()
                .Which
                .ShouldBeEquivalentTo<CalculationCurrentVersion>(new CalculationCurrentVersion()
                {
                    PolicyName = "Pol2",
                    PolicyId = "pol2",
                    Id = CalculationId,
                    AllocationLine = new Models.Reference("al1", "Allocation Line 1"),
                    Description = "Test",
                    Name = "Calc Name",
                    CalculationType = CalculationType.Funding,
                });

            logger
                .Received(1)
                .Information(Arg.Is($"A calculation was found for specification id {SpecificationId} and calculation id {CalculationId}"));

            await specificationsRepository
                .Received(1)
                .GetSpecificationById(Arg.Is(SpecificationId));
        }

        [TestMethod]
        public async Task GetCalculationBySpecificationIdAndCalculationId_GivenSpecificationDoesNotExist_ReturnsNotFound()
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
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns((Specification)null);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.GetCalculationBySpecificationIdAndCalculationId(request);

            //Assert
            result
                .Should()
                .BeOfType<PreconditionFailedResult>()
                .Which
                .Value
                .Should()
                .Be("Specification not found");
        }
    }
}
