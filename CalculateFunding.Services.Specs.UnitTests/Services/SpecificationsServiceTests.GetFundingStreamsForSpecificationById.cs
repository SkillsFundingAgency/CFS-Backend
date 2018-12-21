using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Specs.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Specs.Services
{
    public partial class SpecificationsServiceTests
    {

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
                .Should().BeEquivalentTo(expectedFundingStreams.AsEnumerable());

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

    }
}
