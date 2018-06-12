using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Services.Calcs.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Caching;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [TestMethod]
        public async Task GetCurrentCalculationsForSpecification_GivenNoCalculationIdWasProvided_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            IActionResult result = await service.GetCurrentCalculationsForSpecification(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty specificationId provided");

            logger
                .Received(1)
                .Warning(Arg.Is("No specificationId was provided to GetCalculationsForSpecification"));
        }

        [TestMethod]
        public async Task GetCurrentCalculationsForSpecification_WhenCalculationsFoundInCache_ThenCalculationsReturnedFromCache()
        {
            //Arrange
            const string specificationId = "specid";

            List<CalculationCurrentVersion> calculations = new List<CalculationCurrentVersion>()
            {
                new CalculationCurrentVersion()
                {
                    Id ="one",
                    Name ="Calculation Name",
                    SourceCode = "Return 10",
                    PublishStatus = PublishStatus.Draft,
                    Author = new Reference("userId", "User Name"),
                    CalculationSpecification = new Reference("specId", "Calculation Specification ID"),
                    CalculationType = "Funding",
                    Version = 1,
                },
                new CalculationCurrentVersion()
                {
                    Id ="two",
                    Name ="Calculation Name Two",
                    SourceCode = "Return 50",
                    PublishStatus = PublishStatus.Approved,
                    Author = new Reference("userId", "User Name"),
                    CalculationSpecification = new Reference("specId", "Calculation Specification ID"),
                    CalculationType = "Number",
                    Version = 5,
                }
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CalculationCurrentVersion>>($"{CacheKeys.CurrentCalculationsForSpecification}{specificationId}")
                .Returns(calculations);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await service.GetCurrentCalculationsForSpecification(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeAssignableTo<IEnumerable<CalculationCurrentVersion>>()
                .Which
                .ShouldAllBeEquivalentTo(new List<CalculationCurrentVersion>()
                {
                    new CalculationCurrentVersion()
                    {
                        Id ="one",
                        Name ="Calculation Name",
                        SourceCode = "Return 10",
                        Author = new Reference("userId", "User Name"),
                        CalculationSpecification = new Reference("specId", "Calculation Specification ID"),
                        CalculationType = "Funding",
                        Version = 1,
                        PublishStatus = PublishStatus.Draft,
                    },
                    new CalculationCurrentVersion()
                    {
                        Id ="two",
                        Name ="Calculation Name Two",
                        SourceCode = "Return 50",
                        PublishStatus = PublishStatus.Approved,
                        Author = new Reference("userId", "User Name"),
                        CalculationSpecification = new Reference("specId", "Calculation Specification ID"),
                        CalculationType = "Number",
                        Version = 5,
                    }
                });

            await calculationsRepository
                .Received(0)
                .GetCalculationsBySpecificationId(Arg.Is(specificationId));

            await cacheProvider
                .Received(1)
                .GetAsync<List<CalculationCurrentVersion>>($"{CacheKeys.CurrentCalculationsForSpecification}{specificationId}");
        }

        [TestMethod]
        async public Task GetCurrentCalculationsForSpecification_WhenCalculationsNotFoundInCache_ThenCalculationsReturnedFromRepository()
        {
            //Arrange
            const string specificationId = "specid";

            DateTimeOffset calc1DateTime = DateTimeOffset.Now;
            DateTimeOffset calc2DateTime = DateTimeOffset.Now;

            List<Calculation> calculations = new List<Calculation>()
            {
                new Calculation()
                {
                    Id ="one",
                    Name ="Calculation Name",
                    Current = new CalculationVersion()
                    {
                        SourceCode = "Return 10",
                        PublishStatus = PublishStatus.Draft,
                        Author = new Reference("userId", "User Name"),
                        Version = 1,
                        Date = calc1DateTime,
                    },
                    CalculationSpecification = new Reference("specId", "Calculation Specification ID"),
                    CalculationType = CalculationType.Funding,
                    FundingPeriod = new Reference("fp1", "Funding Period 1"),
                    SpecificationId = specificationId,
                },
                new Calculation()
                {
                    Id ="two",
                    Name ="Calculation Name Two",
                    Current = new CalculationVersion()
                    {
                        SourceCode = "Return 50",
                        PublishStatus = PublishStatus.Approved,
                        Author = new Reference("userId", "User Name"),
                        Version = 5,
                        Date= calc2DateTime,
                    },
                    CalculationSpecification = new Reference("specId", "Calculation Specification ID"),
                    CalculationType = CalculationType.Number,
                    FundingPeriod = new Reference("fp2", "Funding Period Two"),
                    SpecificationId = specificationId,
                }
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calculations);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CalculationCurrentVersion>>($"{CacheKeys.CurrentCalculationsForSpecification}{specificationId}")
                .Returns((List<CalculationCurrentVersion>)null);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await service.GetCurrentCalculationsForSpecification(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeAssignableTo<IEnumerable<CalculationCurrentVersion>>()
                .Which
                .ShouldAllBeEquivalentTo(new List<CalculationCurrentVersion>()
                {
                    new CalculationCurrentVersion()
                    {
                        Id ="one",
                        Name ="Calculation Name",
                        SourceCode = "Return 10",
                        Author = new Reference("userId", "User Name"),
                        CalculationSpecification = new Reference("specId", "Calculation Specification ID"),
                        CalculationType = "Funding",
                        Version = 1,
                        FundingPeriodId = "fp1",
                        FundingPeriodName = "Funding Period 1",
                        Date = calc1DateTime,
                        SpecificationId = specificationId,
                        PublishStatus = PublishStatus.Draft,
                    },
                    new CalculationCurrentVersion()
                    {
                        Id ="two",
                        Name ="Calculation Name Two",
                        SourceCode = "Return 50",
                        Author = new Reference("userId", "User Name"),
                        CalculationSpecification = new Reference("specId", "Calculation Specification ID"),
                        CalculationType = "Number",
                        Version = 5,
                        FundingPeriodId = "fp2",
                        FundingPeriodName = "Funding Period Two",
                        Date = calc2DateTime,
                        SpecificationId = specificationId,
                        PublishStatus = PublishStatus.Approved,
                    }
                });

            await calculationsRepository
                .Received(1)
                .GetCalculationsBySpecificationId(Arg.Is(specificationId));

            await cacheProvider
                .Received(1)
                .GetAsync<List<CalculationCurrentVersion>>($"{CacheKeys.CurrentCalculationsForSpecification}{specificationId}");
        }

        [TestMethod]
        async public Task GetCurrentCalculationsForSpecification_WhenEmptyListOfCalculationsFoundInCache_ThenCalculationsReturned()
        {
            //Arrange
            const string specificationId = "specid";

            List<CalculationCurrentVersion> calculations = new List<CalculationCurrentVersion>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(specificationId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CalculationCurrentVersion>>($"{CacheKeys.CurrentCalculationsForSpecification}{specificationId}")
                .Returns(calculations);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await service.GetCurrentCalculationsForSpecification(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeAssignableTo<IEnumerable<CalculationCurrentVersion>>()
                .Which
                .Should()
                .HaveCount(calculations.Count);

            await calculationsRepository
                .Received(0)
                .GetCalculationsBySpecificationId(Arg.Is(specificationId));
        }
    }
}
