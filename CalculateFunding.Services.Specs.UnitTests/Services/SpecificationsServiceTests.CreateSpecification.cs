using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Specs.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;

namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    public partial class SpecificationsServiceTests
    {
        [TestMethod]
        public async Task SpecificationsService_CreateSpecification_WhenValidInputProvided_ThenSpecificationIsCreated()
        {
            // Arrange
            const string fundingStreamId = "fs1";
            const string fundingPeriodId = "fp1";


            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            IPoliciesRepository policiesRepository = CreatePoliciesRepository();

            IMapper mapper = CreateImplementedMapper();
            IVersionRepository<SpecificationVersion> versionRepository = CreateVersionRepository();

            SpecificationsService specificationsService = CreateService(
                specificationsRepository: specificationsRepository,
                searchRepository: searchRepository,
                mapper: mapper,
                specificationVersionRepository: versionRepository,
                policiesRepository: policiesRepository);

            SpecificationCreateModel specificationCreateModel = new SpecificationCreateModel()
            {
                Name = "Specification Name",
                Description = "Specification Description",
                FundingPeriodId = "fp1",
                FundingStreamIds = new List<string>() { fundingStreamId },
            };

            string json = JsonConvert.SerializeObject(specificationCreateModel);
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

            specificationsRepository
                .GetSpecificationByQuery(Arg.Any<Expression<Func<Specification, bool>>>())
                .Returns((Specification)null);

            Period fundingPeriod = new Period()
            {
                Id = fundingPeriodId,
                Name = "Funding Period 1"
            };

            policiesRepository
                .GetPeriodById(Arg.Is(fundingPeriodId))
                .Returns(fundingPeriod);

            FundingStream fundingStream = new FundingStream()
            {
                Id = fundingStreamId,
                Name = "Funding Stream 1",
                AllocationLines = new List<AllocationLine>(),
            };

            policiesRepository
                .GetFundingStreamById(Arg.Is(fundingStreamId))
                .Returns(fundingStream);

            DateTime createdDate = new DateTime(2018, 1, 2, 5, 6, 2);

            SpecificationVersion specificationVersion = new SpecificationVersion()
            {
                Description = "Specification Description",
                FundingPeriod = new Reference("fp1", "Funding Period 1"),
                Date = createdDate,
                PublishStatus = Models.Versioning.PublishStatus.Draft,
                FundingStreams = new List<Reference>() { new Reference(FundingStreamId, "Funding Stream 1") },
                Name = "Specification Name",
                Version = 1,
                SpecificationId = SpecificationId
            };

            versionRepository
                .CreateVersion(Arg.Any<SpecificationVersion>())
                .Returns(specificationVersion);

            DocumentEntity<Specification> createdSpecification = new DocumentEntity<Specification>()
            {
                Content = new Specification()
                {
                    Name = "Specification Name",
                    Id = "createdSpec",
                    Current = specificationVersion
                },
            };

            specificationsRepository
                .CreateSpecification(Arg.Is<Specification>(
                    s => s.Name == specificationCreateModel.Name &&
                    s.Current.Description == specificationCreateModel.Description &&
                    s.Current.FundingPeriod.Id == fundingPeriodId))
                .Returns(createdSpecification);

            // Act
            IActionResult result = await specificationsService.CreateSpecification(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<SpecificationCurrentVersion>()
                .And
                .NotBeNull();

            await specificationsRepository
                .Received(1)
                .CreateSpecification(Arg.Is<Specification>(
                   s => s.Name == specificationCreateModel.Name &&
                   s.Current.Description == specificationCreateModel.Description &&
                   s.Current.FundingPeriod.Id == fundingPeriodId));

            await searchRepository
                .Received(1)
                .Index(Arg.Is<List<SpecificationIndex>>(c =>
                c.Count() == 1 &&
                !string.IsNullOrWhiteSpace(c.First().Id) &&
                c.First().Name == specificationCreateModel.Name
                ));

            await versionRepository
               .Received(1)
               .SaveVersion(Arg.Is<SpecificationVersion>(
                       m => !string.IsNullOrWhiteSpace(m.EntityId) &&
                       m.PublishStatus == Models.Versioning.PublishStatus.Draft &&
                       m.Description == "Specification Description" &&
                       m.FundingPeriod.Id == "fp1" &&
                       m.FundingPeriod.Name == "Funding Period 1" &&
                       m.FundingStreams.Any() &&
                       m.Name == "Specification Name" &&
                       m.Version == 1
                   ));
        }

        [TestMethod]
        public async Task SpecificationsService_CreateSpecification_WhenFundingStreamIDIsProvidedButDoesNotExist_ThenPreConditionFailedReturned()
        {
            // Arrange
            const string fundingStreamId = "fs1";
            const string fundingStreamNotFoundId = "notfound";

            const string fundingPeriodId = "fp1";


            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            ISearchRepository<SpecificationIndex> searchRepository = CreateSearchRepository();
            IPoliciesRepository policiesRepository = CreatePoliciesRepository();

            IMapper mapper = CreateImplementedMapper();

            SpecificationsService specificationsService = CreateService(
                specificationsRepository: specificationsRepository,
                searchRepository: searchRepository,
                mapper: mapper,
                policiesRepository: policiesRepository);

            SpecificationCreateModel specificationCreateModel = new SpecificationCreateModel()
            {
                Name = "Specification Name",
                Description = "Specification Description",
                FundingPeriodId = "fp1",
                FundingStreamIds = new List<string>() { fundingStreamId, fundingStreamNotFoundId, },
            };

            string json = JsonConvert.SerializeObject(specificationCreateModel);
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

            specificationsRepository
                .GetSpecificationByQuery(Arg.Any<Expression<Func<Specification, bool>>>())
                .Returns((Specification)null);

            Period fundingPeriod = new Period()
            {
                Id = fundingPeriodId,
                Name = "Funding Period 1"
            };

            policiesRepository
                .GetPeriodById(Arg.Is(fundingPeriodId))
                .Returns(fundingPeriod);

            FundingStream fundingStream = new FundingStream()
            {
                Id = fundingStreamId,
                Name = "Funding Stream 1",
                AllocationLines = new List<AllocationLine>(),
            };

            policiesRepository
                .GetFundingStreamById(Arg.Is(fundingStreamId))
                .Returns(fundingStream);

            policiesRepository
                .GetFundingStreamById(Arg.Is(fundingStreamNotFoundId))
                .Returns((FundingStream)null);

            // Act
            IActionResult result = await specificationsService.CreateSpecification(request);

            // Assert
            result
                .Should()
                .BeOfType<PreconditionFailedResult>()
                .Which
                .Value
                .Should()
                .Be("Unable to find funding stream with ID 'notfound'.");

            await policiesRepository
                .Received(1)
                .GetFundingStreamById(Arg.Is(fundingStreamNotFoundId));

            await policiesRepository
                .Received(1)
                .GetFundingStreamById(Arg.Is(fundingStreamId));
        }

        [TestMethod]
        public async Task SpecificationsService_CreateSpecification_WhenInvalidInputProvided_ThenValidationErrorReturned()
        {
            // Arrange
            ValidationResult validationResult = new ValidationResult();
            validationResult.Errors.Add(new ValidationFailure("fundingStreamId", "Test"));

            IValidator<SpecificationCreateModel> validator = CreateSpecificationValidator(validationResult);

            SpecificationsService specificationsService = CreateService(specificationCreateModelvalidator: validator);

            SpecificationCreateModel specificationCreateModel = new SpecificationCreateModel()
            {
                Name = "Specification Name",
                Description = "Specification Description",
                FundingPeriodId = null,
                FundingStreamIds = new List<string>() { },
            };

            string json = JsonConvert.SerializeObject(specificationCreateModel);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpContext context = Substitute.For<HttpContext>();

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            request
                .HttpContext
                .Returns(context);

            // Act
            IActionResult result = await specificationsService.CreateSpecification(request);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<SerializableError>()
                .Which
                .Should()
                .HaveCount(1);

            await validator
                .Received(1)
                .ValidateAsync(Arg.Any<SpecificationCreateModel>());
        }

    }
}
