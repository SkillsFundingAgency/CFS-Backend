using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Policy;
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
using PolicyModels = CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    public partial class SpecificationsServiceTests
    {

        [TestMethod]
        public async Task SpecificationsService_GetFundingStreamsForSpecificationById_WhenRequestingASpecificationWhenExists_ThenFundingStreamsReturned()
        {
            // Arrange
            const string specificationId = "spec1";

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();

            IMapper mapper = CreateImplementedMapper();
            SpecificationsService specificationsService = CreateService(mapper: mapper, specificationsRepository: specificationsRepository, policiesApiClient: policiesApiClient);

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

            PolicyModels.FundingStream fundingStream = new PolicyModels.FundingStream()
            {
                Id = "fs1",
                Name = "Funding Stream 1",
                AllocationLines = new List<PolicyModels.AllocationLine>(),
            };

            ApiResponse<PolicyModels.FundingStream> fundingStreamResponse = new ApiResponse<PolicyModels.FundingStream>(HttpStatusCode.OK, fundingStream);

            policiesApiClient
                .GetFundingStreamById(Arg.Is<string>(fundingStream.Id))
                .Returns(fundingStreamResponse);

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

            await policiesApiClient
                .Received(1)
                .GetFundingStreamById(fundingStream.Id);
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
            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();

            SpecificationsService specificationsService = CreateService(specificationsRepository: specificationsRepository, policiesApiClient: policiesApiClient);

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

            ApiResponse<PolicyModels.FundingStream> fundingStreamResponse = new ApiResponse<PolicyModels.FundingStream>(HttpStatusCode.OK, null);

            policiesApiClient
                .GetFundingStreamById(Arg.Any<string>())
                .Returns(fundingStreamResponse);

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

            await policiesApiClient
                .Received(1)
                .GetFundingStreamById(Arg.Any<string>());
        }

        [TestMethod]
        public async Task SpecificationsService_GetFundingStreamsForSpecificationById_WhenSpecificationContainsNoFundingStreams_ThenErrorReturned()
        {
            // Arrange
            const string specificationId = "spec1";

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();

            SpecificationsService specificationsService = CreateService(specificationsRepository: specificationsRepository, policiesApiClient: policiesApiClient);

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

            List<PolicyModels.FundingStream> fundingStreams = new List<PolicyModels.FundingStream>();

            ApiResponse<IEnumerable<PolicyModels.FundingStream>> fundingStreamsResponse = new ApiResponse<IEnumerable<PolicyModels.FundingStream>>(HttpStatusCode.OK, fundingStreams);

            policiesApiClient
                .GetFundingStreams()
                .Returns(fundingStreamsResponse);

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

            await policiesApiClient
                .Received(0)
                .GetFundingStreams();
        }

    }
}
