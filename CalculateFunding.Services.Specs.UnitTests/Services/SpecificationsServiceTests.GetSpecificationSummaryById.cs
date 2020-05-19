using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Specs.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    public partial class SpecificationsServiceTests
    {

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
            IActionResult result = await service.GetSpecificationSummaryById(SpecificationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should().BeEquivalentTo(new SpecificationSummary()
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
            IActionResult result = await service.GetSpecificationSummaryById(SpecificationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should().BeEquivalentTo(new SpecificationSummary()
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
                    DataDefinitionRelationshipIds = new []
                    {
                        "dr1", 
                        "dr2"
                    }
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
            IActionResult result = await service.GetSpecificationSummaryById(SpecificationId);

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
    }
}
