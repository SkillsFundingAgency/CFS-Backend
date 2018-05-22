using AutoMapper;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using System.Linq.Expressions;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Security.Claims;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;

namespace CalculateFunding.Services.Specs.Services
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

            IMapper mapper = CreateImplementedMapper();

            SpecificationsService specificationsService = CreateService(
                specificationsRepository: specificationsRepository,
                searchRepository: searchRepository,
                mapper: mapper);

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

            FundingPeriod fundingPeriod = new FundingPeriod()
            {
                Id = fundingPeriodId,
                Name = "Funding Period 1",
                Type = "Test",
            };

            specificationsRepository
                .GetFundingPeriodById(Arg.Is(fundingPeriodId))
                .Returns(fundingPeriod);

            FundingStream fundingStream = new FundingStream()
            {
                Id = fundingStreamId,
                Name = "Funding Stream 1",
                AllocationLines = new List<AllocationLine>(),
            };

            specificationsRepository
                .GetFundingStreamById(Arg.Is(fundingStreamId))
                .Returns(fundingStream);

            specificationsRepository
                .CreateSpecification(Arg.Is<Specification>(
                    s => s.Name == specificationCreateModel.Name &&
                    s.Current.Description == specificationCreateModel.Description &&
                    s.Current.FundingPeriod.Id == fundingPeriodId))
                .Returns(HttpStatusCode.Created);

            // Act
            IActionResult result = await specificationsService.CreateSpecification(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<Specification>()
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

            IMapper mapper = CreateImplementedMapper();

            SpecificationsService specificationsService = CreateService(
                specificationsRepository: specificationsRepository,
                searchRepository: searchRepository,
                mapper: mapper);

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

            FundingPeriod fundingPeriod = new FundingPeriod()
            {
                Id = fundingPeriodId,
                Name = "Funding Period 1",
                Type = "Test",
            };

            specificationsRepository
                .GetFundingPeriodById(Arg.Is(fundingPeriodId))
                .Returns(fundingPeriod);

            FundingStream fundingStream = new FundingStream()
            {
                Id = fundingStreamId,
                Name = "Funding Stream 1",
                AllocationLines = new List<AllocationLine>(),
            };

            specificationsRepository
                .GetFundingStreamById(Arg.Is(fundingStreamId))
                .Returns(fundingStream);

            specificationsRepository
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

            await specificationsRepository
                .Received(1)
                .GetFundingStreamById(Arg.Is(fundingStreamNotFoundId));

            await specificationsRepository
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
