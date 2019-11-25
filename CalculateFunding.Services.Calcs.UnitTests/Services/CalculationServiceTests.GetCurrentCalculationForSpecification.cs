using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Caching;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [TestMethod]
        public async Task GetCurrentCalculationsForSpecification_GivenNoCalculationIdWasProvided_ReturnsBadRequest()
        {
            //Arrange
            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            IActionResult result = await service.GetCurrentCalculationsForSpecification(null);

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

            List<CalculationResponseModel> calculations = new List<CalculationResponseModel>()
            {
                new CalculationResponseModel()
                {
                    Id ="one",
                    Name ="Calculation Name",
                    SourceCode = "Return 10",
                    PublishStatus = PublishStatus.Draft,
                    Author = new Reference("userId", "User Name"),
                    CalculationType = CalculationType.Template,
                    Version = 1,
                },
                new CalculationResponseModel()
                {
                    Id ="two",
                    Name ="Calculation Name Two",
                    SourceCode = "Return 50",
                    PublishStatus = PublishStatus.Approved,
                    Author = new Reference("userId", "User Name"),
                    CalculationType =   CalculationType.Template,
                    Version = 5,
                }
            };

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CalculationResponseModel>>($"{CacheKeys.CurrentCalculationsForSpecification}{specificationId}")
                .Returns(calculations);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await service.GetCurrentCalculationsForSpecification(specificationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeAssignableTo<IEnumerable<CalculationResponseModel>>()
                .Which
                .Should()
                    .Contain(m => m.Id == "one" && m.Name == "Calculation Name" && m.SourceCode == "Return 10" && m.CalculationType == CalculationType.Template && m.Version == 1 && m.PublishStatus == PublishStatus.Draft)
                    .And.Contain(m => m.Id == "two" && m.Name == "Calculation Name Two" && m.SourceCode == "Return 50" && m.PublishStatus == PublishStatus.Approved && m.CalculationType == CalculationType.Template && m.Version == 5);

            await calculationsRepository
                .Received(0)
                .GetCalculationsBySpecificationId(Arg.Is(specificationId));

            await cacheProvider
                .Received(1)
                .GetAsync<List<CalculationResponseModel>>($"{CacheKeys.CurrentCalculationsForSpecification}{specificationId}");
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
                    Current = new CalculationVersion()
                    {
                        SourceCode = "Return 10",
                        PublishStatus = PublishStatus.Draft,
                        Author = new Reference("userId", "User Name"),
                        Version = 1,
                        Date = calc1DateTime,
                        Name ="Calculation Name",
                        CalculationType = CalculationType.Template,
                    },
                    SpecificationId = specificationId,
                },
                new Calculation()
                {
                    Id ="two",
                    Current = new CalculationVersion()
                    {
                        SourceCode = "Return 50",
                        PublishStatus = PublishStatus.Approved,
                        Author = new Reference("userId", "User Name"),
                        Version = 5,
                        Date= calc2DateTime,
                        Name ="Calculation Name Two",
                        CalculationType = CalculationType.Template,
                    },
                    SpecificationId = specificationId,
                }
            };

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();

            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calculations);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CalculationResponseModel>>($"{CacheKeys.CurrentCalculationsForSpecification}{specificationId}")
                .Returns((List<CalculationResponseModel>)null);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await service.GetCurrentCalculationsForSpecification(specificationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeAssignableTo<IEnumerable<CalculationResponseModel>>()
                .Which
                .Should()
                    .Contain(m => m.Id == "one" && m.Name == "Calculation Name" && m.SourceCode == "Return 10" && m.CalculationType == CalculationType.Template && m.Version == 1 && m.LastUpdated == calc1DateTime && m.SpecificationId == specificationId && m.PublishStatus == PublishStatus.Draft)
                    .And.Contain(m => m.Id == "two" && m.Name == "Calculation Name Two" && m.SourceCode == "Return 50" && m.CalculationType == CalculationType.Template && m.Version == 5 && m.LastUpdated == calc2DateTime && m.SpecificationId == specificationId && m.PublishStatus == PublishStatus.Approved);

            await calculationsRepository
                .Received(1)
                .GetCalculationsBySpecificationId(Arg.Is(specificationId));

            await cacheProvider
                .Received(1)
                .GetAsync<List<CalculationResponseModel>>($"{CacheKeys.CurrentCalculationsForSpecification}{specificationId}");
        }

        [TestMethod]
        async public Task GetCurrentCalculationsForSpecification_WhenEmptyListOfCalculationsFoundInCache_ThenCalculationsReturned()
        {
            //Arrange
            const string specificationId = "specid";

            List<CalculationResponseModel> calculations = new List<CalculationResponseModel>();

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CalculationResponseModel>>($"{CacheKeys.CurrentCalculationsForSpecification}{specificationId}")
                .Returns(calculations);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await service.GetCurrentCalculationsForSpecification(CalculationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeAssignableTo<IEnumerable<CalculationResponseModel>>()
                .Which
                .Should()
                .HaveCount(calculations.Count);

            await calculationsRepository
                .Received(0)
                .GetCalculationsBySpecificationId(Arg.Is(specificationId));
        }
    }
}
