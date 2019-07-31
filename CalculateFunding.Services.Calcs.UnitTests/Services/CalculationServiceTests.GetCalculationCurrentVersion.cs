﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Common.Caching;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [TestMethod]
        public async Task GetCalculationCurrentVersion_GivenNoCalculationIdprovided_returnsBadrequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            CalculationService service = CreateCalculationService(logger: logger);

            //Act
            IActionResult result = await service.GetCalculationCurrentVersion(request);

            //Assert
            logger
                .Received(1)
                .Error("No calculation Id was provided to GetCalculationCurrentVersion");

            result
                 .Should()
                 .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        async public Task GetCalculationCurrentVersion_GivencalculationWasNotFound_ReturnsNotFound()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns((Calculation)null);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository);

            //Act
            IActionResult result = await service.GetCalculationCurrentVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"A calculation was not found for calculation id {CalculationId}"));
        }

        [TestMethod]
        async public Task GetCalculationCurrentVersion_GivencalculationWasFoundWithNoCurrent_ReturnsNotFound()
        {
            //Arrange
            Calculation calculation = new Calculation();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository);

            //Act
            IActionResult result = await service.GetCalculationCurrentVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"A current calculation was not found for calculation id {CalculationId}"));
        }

        [TestMethod]
        async public Task GetCalculationCurrentVersion_GivenCalculationExistsAndWasWasFoundInCache_ReturnsOK()
        {
            //Arrange
            const string specificationId = "specId";
            DateTimeOffset lastModifiedDate = DateTimeOffset.Now;

            CalculationCurrentVersion calculation = new CalculationCurrentVersion
            {
                Author = new Reference(UserId, Username),
                Date = lastModifiedDate,
                SourceCode = "source code",
                Version = 1,
                Name = "any name",
                Id = CalculationId,
                FundingPeriodName = "2018/2019",
                FundingStreamId = "18/19",
                CalculationType = "Number",
                SpecificationId = specificationId,
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();

            ICacheProvider cacheProvider = CreateCacheProvider();

            cacheProvider
                .GetAsync<CalculationCurrentVersion>(Arg.Is($"{CacheKeys.CurrentCalculation}{CalculationId}"))
                .Returns(calculation);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await service.GetCalculationCurrentVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should().BeEquivalentTo(new CalculationCurrentVersion
                {
                    Author = new Reference(UserId, Username),
                    Date = lastModifiedDate,
                    SourceCode = "source code",
                    Version = 1,
                    Name = "any name",
                    Id = CalculationId,
                    FundingPeriodName = "2018/2019",
                    FundingStreamId = "18/19",
                    CalculationType = "Number",
                    SpecificationId = specificationId,
                });

            await calculationsRepository
                .Received(0)
                .GetCalculationById(Arg.Any<string>());

            await cacheProvider
                .Received(1)
                .GetAsync<CalculationCurrentVersion>(Arg.Is($"{CacheKeys.CurrentCalculation}{CalculationId}"));
        }

        [TestMethod]
        async public Task GetCalculationCurrentVersion_GivenCalculationExistsAndWasWasNotFoundInCache_ReturnsOK()
        {
            //Arrange
            const string specificationId = "specId";
            DateTimeOffset lastModifiedDate = DateTimeOffset.Now;

            Calculation calculation = new Calculation
            {
                SpecificationId = specificationId,
                Current = new CalculationVersion
                {
                    Author = new Reference(UserId, Username),
                    Date = lastModifiedDate,
                    PublishStatus = PublishStatus.Draft,
                    SourceCode = "source code",
                    Version = 1,
                    Name = "any name",
                    CalculationType = CalculationType.Template
                },
                
                Id = CalculationId
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "calculationId", new StringValues(CalculationId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationById(Arg.Is(CalculationId))
                .Returns(calculation);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
               .GetAsync<CalculationCurrentVersion>(Arg.Is($"{CacheKeys.CurrentCalculation}{CalculationId}"))
               .Returns((CalculationCurrentVersion)null);

            CalculationService service = CreateCalculationService(logger: logger, calculationsRepository: calculationsRepository, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await service.GetCalculationCurrentVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should().BeEquivalentTo(new CalculationCurrentVersion
                {
                    Author = new Reference(UserId, Username),
                    Date = lastModifiedDate,
                    SourceCode = "source code",
                    Version = 1,
                    Name = "any name",
                    Id = CalculationId,
                    CalculationType = "Template",
                    SpecificationId = specificationId,
                    PublishStatus = PublishStatus.Draft,
                });

            await calculationsRepository
                .Received(1)
                .GetCalculationById(Arg.Is<string>(CalculationId));

            await cacheProvider
                .Received(1)
                .GetAsync<CalculationCurrentVersion>(Arg.Is($"{CacheKeys.CurrentCalculation}{CalculationId}"));
        }
    }
}
