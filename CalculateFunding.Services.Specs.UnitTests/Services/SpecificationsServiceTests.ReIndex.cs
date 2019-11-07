using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Specs.Interfaces;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    public partial class SpecificationsServiceTests
    {
        [TestMethod]
        public async Task ReIndex_GivenDeleteIndexThrowsException_ReturnsInternalServerError()
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
        public async Task ReIndex_GivenGetAllSpecificationDocumentsThrowsException_ReturnsInternalServerError()
        {
            //Arrange
            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .When(x => x.GetSpecificationsByRawQuery<SpecificationSearchModel>(Arg.Any<CosmosDbQuery>()))
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
        public async Task ReIndex_GivenIndexingThrowsException_ReturnsInternalServerError()
        {
            //Arrange
            IEnumerable<SpecificationSearchModel> specifications = new[]
            {
                new SpecificationSearchModel
                {
                    Id = SpecificationId,
                    Name = SpecificationName,
                    FundingStreams = new List<Reference> { new Reference("fs-id", "fs-name") },
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
                .GetSpecificationsByRawQuery<SpecificationSearchModel>(Arg.Any<CosmosDbQuery>())
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
        public async Task ReIndex_GivenNoDocumentsReturnedFromCosmos_ReturnsNoContent()
        {
            //Arrange
            IEnumerable<SpecificationSearchModel> specifications = new SpecificationSearchModel[0];

            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationsByRawQuery<SpecificationSearchModel>(Arg.Any<CosmosDbQuery>())
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
        public async Task ReIndex_GivenDocumentsReturnedFromCosmos_ReturnsNoContent()
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
                .GetSpecificationsByRawQuery<SpecificationSearchModel>(Arg.Any<CosmosDbQuery>())
                .Returns(specifications);

            ILogger logger = CreateLogger();

            ISpecificationsService service = CreateService(searchRepository: searchRepository, logs: logger,
                specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.ReIndex();

            //Assert
            logger
                .Received(1)
                .Information(Arg.Is($"Successfully re-indexed 1 documents"));

            result
                .Should()
                .BeOfType<NoContentResult>();
        }
    }
}
