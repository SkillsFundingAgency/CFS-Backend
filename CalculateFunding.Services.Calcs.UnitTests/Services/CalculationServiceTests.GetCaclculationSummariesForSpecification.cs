using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Exceptions;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs.Interfaces;
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.Compiler.Interfaces;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.CodeMetadataGenerator.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.CodeGeneration;
using Microsoft.Azure.ServiceBus;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [TestMethod]
        public async Task GetCalculationSummariesForSpecification_GivenNoCalculationIdWasProvided_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            IActionResult result = await service.GetCalculationSummariesForSpecification(request);

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
                .Warning(Arg.Is("No specificationId was provided to GetCalculationSummariesForSpecification"));
        }

        [TestMethod]
        public async Task GetCalculationSummariesForSpecification_WhenCalculationsFoundInCache_ThenCalculationsReturnedFromCache()
        {
            //Arrange
            const string specificationId = "specid";

            List<CalculationSummaryModel> calculations = new List<CalculationSummaryModel>()
            {
                new CalculationSummaryModel()
                {
                    Id ="one",
                    Name ="Calculation Name",
                    CalculationType = CalculationType.Funding
                },
                new CalculationSummaryModel()
                {
                    Id ="two",
                    Name ="Calculation Name Two",
                    CalculationType =  CalculationType.Number,
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
                .GetAsync<List<CalculationSummaryModel>>($"{CacheKeys.CalculationsSummariesForSpecification}{specificationId}")
                .Returns(calculations);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository, cacheProvider: cacheProvider);

            // Act
            IActionResult result = await service.GetCalculationSummariesForSpecification(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeAssignableTo<IEnumerable<CalculationSummaryModel>>()
                .Which
                .ShouldAllBeEquivalentTo(new List<CalculationSummaryModel>()
                {
                    new CalculationSummaryModel()
                    {
                        Id ="one",
                        Name ="Calculation Name",
                        CalculationType = CalculationType.Funding
                    },
                    new CalculationSummaryModel()
                    {
                        Id ="two",
                        Name ="Calculation Name Two",
                        CalculationType =  CalculationType.Number,
                    }
                });

            await calculationsRepository
                .Received(0)
                .GetCalculationsBySpecificationId(Arg.Is(specificationId));

            await cacheProvider
                .Received(1)
                .GetAsync<List<CalculationSummaryModel>>($"{CacheKeys.CalculationsSummariesForSpecification}{specificationId}");
        }

        [TestMethod]
        async public Task GetCalculationSummariesForSpecification_WhenCalculationsNotFoundInCache_ThenCalculationsReturnedFromRepository()
        {
            // Arrange
            const string specificationId = "specid";

            DateTime calc1DateTime = DateTime.UtcNow;
            DateTime calc2DateTime = DateTime.UtcNow;

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
                        PublishStatus = PublishStatus.Published,
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
                .GetAsync<List<CalculationSummaryModel>>($"{CacheKeys.CalculationsSummariesForSpecification}{specificationId}")
                .Returns((List<CalculationSummaryModel>)null);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository, cacheProvider: cacheProvider);

            // Act
            IActionResult result = await service.GetCalculationSummariesForSpecification(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeAssignableTo<IEnumerable<CalculationSummaryModel>>()
                .Which
                .ShouldAllBeEquivalentTo(new List<CalculationSummaryModel>()
                {
                    new CalculationSummaryModel()
                    {
                        Id ="one",
                        Name ="Calculation Name",
                        CalculationType = CalculationType.Funding
                    },
                    new CalculationSummaryModel()
                    {
                        Id ="two",
                        Name ="Calculation Name Two",
                        CalculationType =  CalculationType.Number,
                    }
                });

            await calculationsRepository
                .Received(1)
                .GetCalculationsBySpecificationId(Arg.Is(specificationId));

            await cacheProvider
                .Received(1)
                .GetAsync<List<CalculationSummaryModel>>($"{CacheKeys.CalculationsSummariesForSpecification}{specificationId}");
        }

        [TestMethod]
        async public Task GetCalculationSummariesForSpecification_WhenEmptyListOfCalculationsFoundInCache_ThenCalculationsReturned()
        {
            // Arrange
            const string specificationId = "specid";

            List<CalculationSummaryModel> calculations = new List<CalculationSummaryModel>();

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
                .GetAsync<List<CalculationSummaryModel>>($"{CacheKeys.CalculationsSummariesForSpecification}{specificationId}")
                .Returns(calculations);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await service.GetCalculationSummariesForSpecification(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeAssignableTo<IEnumerable<CalculationSummaryModel>>()
                .Which
                .Should()
                .HaveCount(calculations.Count);

            await calculationsRepository
                .Received(0)
                .GetCalculationsBySpecificationId(Arg.Is(specificationId));
        }

        [TestMethod]
        async public Task GetCalculationSummariesForSpecification_WhenCalculationsNotFoundInCacheAndResponseFromRepositoryIsNull_ThenErrorReturned()
        {
            // Arrange
            const string specificationId = "specid";

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
                .Returns((IEnumerable<Calculation>)null);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CalculationSummaryModel>>($"{CacheKeys.CalculationsSummariesForSpecification}{specificationId}")
                .Returns((List<CalculationSummaryModel>)null);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository, cacheProvider: cacheProvider);

            // Act
            IActionResult result = await service.GetCalculationSummariesForSpecification(request);

            // Assert
            result
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be("Calculations from repository returned null");

            await calculationsRepository
                .Received(1)
                .GetCalculationsBySpecificationId(Arg.Is(specificationId));

            await cacheProvider
                .Received(1)
                .GetAsync<List<CalculationSummaryModel>>($"{CacheKeys.CalculationsSummariesForSpecification}{specificationId}");

            logger
                .Received(1)
                .Warning($"Calculations from repository returned null for specification ID of '{specificationId}'");
        }
    }
}
