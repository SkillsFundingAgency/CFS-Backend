using System;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
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
        public void SetPublishDates_SpecificationIsNull_ReturnsBadRequestObjectResult()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = null;

            SpecificationsService service = CreateService(logs: logger);

            // Act
            Func<Task<IActionResult>> func = () => service.SetPublishDates(specificationId, null);

            //Assert
            func
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void SetPublishDates_ExternalPublishDateIsNull_ReturnsBadRequestObjectResult()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = "test";
            DateTimeOffset externalPublishDate = DateTimeOffset.Now.Date;
            DateTimeOffset earliestPaymentAvailableDate = DateTimeOffset.Now.Date;

            SpecificationsService service = CreateService(logs: logger);

            // Act
            Func<Task<IActionResult>> func = () => service.SetPublishDates(specificationId, null);

            //Assert
            func
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }
       
        [TestMethod]
        public async Task SetPublishDates_SpecDoesNotExistsInSystem_Returns412WithErrorMessage()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = "test";
            DateTimeOffset externalPublishDate = DateTimeOffset.Now.Date;
            DateTimeOffset earliestPaymentAvailableDate = DateTimeOffset.Now.Date;

            SpecificationPublishDateModel specificationPublishDateModel = new SpecificationPublishDateModel()
            {
                EarliestPaymentAvailableDate = DateTimeOffset.Now.Date,
                ExternalPublicationDate = DateTimeOffset.Now.Date
            };
                
            string expectedErrorMessage = $"No specification ID {specificationId} were returned from the repository, result came back null";

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(specificationId)
                .Returns<Specification>(x => null);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            // Act
            IActionResult result = await service.SetPublishDates(specificationId, specificationPublishDateModel);

            //Assert
            result
                .Should()
                .BeOfType<PreconditionFailedResult>()
                .Which
                .Value
                .Should()
                .Be(expectedErrorMessage);

            logger
                .Received(1)
                .Error(Arg.Is(expectedErrorMessage));
        }

        [TestMethod]
        public async Task SetPublishDates_UpdateSpecFails_Returns412WithErrorMessage()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = "test";
            DateTimeOffset externalPublishDate = DateTimeOffset.Now.Date;
            DateTimeOffset earliestPaymentAvailableDate = DateTimeOffset.Now.Date;

            Specification specification = new Specification
            {
                Current = new SpecificationVersion
                {
                    ExternalPublicationDate = DateTimeOffset.Now.Date,
                    EarliestPaymentAvailableDate = DateTimeOffset.Now.Date
                }
            };

            SpecificationPublishDateModel specificationPublishDateModel = new SpecificationPublishDateModel()
            {
                EarliestPaymentAvailableDate = DateTimeOffset.Now.Date,
                ExternalPublicationDate = DateTimeOffset.Now.Date
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();
            IVersionRepository<SpecificationVersion> specificationVersionRepository = CreateSpecificationVersionRepository();

            specificationsRepository
                .GetSpecificationById(specificationId)
                .Returns(specification);

            SpecificationVersion clonedSpecificationVersion = null;

            specificationVersionRepository.CreateVersion(Arg.Any<SpecificationVersion>(), Arg.Any<SpecificationVersion>())
                .Returns(_ => (SpecificationVersion)_[0])
                .AndDoes(_ => clonedSpecificationVersion = _.ArgAt<SpecificationVersion>(0));

            specificationsRepository
                .UpdateSpecification(Arg.Any<Specification>())
                .Returns(HttpStatusCode.BadRequest);

            SpecificationsService service = CreateService(
                logs: logger,
                specificationsRepository: specificationsRepository,
                specificationVersionRepository: specificationVersionRepository,
                policiesApiClient: policiesApiClient);

            // Act
            IActionResult result = await service.SetPublishDates(specificationId, specificationPublishDateModel);

            //Assert
            result
               .Should()
               .BeOfType<InternalServerErrorResult>()
               .Which
               .Value
               .Should()               
               .Be($"Failed to update specification for id: test with ExternalPublishDate {specificationPublishDateModel.ExternalPublicationDate} and EarliestPaymentAvailableDate {specificationPublishDateModel.EarliestPaymentAvailableDate}");
        }

        [TestMethod]
        public async Task SetPublishDates_ValidParametersPassed_ReturnsOKAndSetsPublishedDatesOnSpec()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = "test";
            DateTimeOffset externalPublishDate = DateTimeOffset.Now.Date;
            DateTimeOffset earliestPaymentAvailableDate = DateTimeOffset.Now.Date;

            Specification specification = new Specification
            {                
                Current = new SpecificationVersion
                {
                    ExternalPublicationDate = DateTimeOffset.Now.Date,
                    EarliestPaymentAvailableDate = DateTimeOffset.Now.Date
                }                
            };

            SpecificationPublishDateModel specificationPublishDateModel = new SpecificationPublishDateModel()
            {
                EarliestPaymentAvailableDate = DateTimeOffset.Now.Date,
                ExternalPublicationDate = DateTimeOffset.Now.Date
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();
            IVersionRepository<SpecificationVersion> specificationVersionRepository = CreateSpecificationVersionRepository();

            specificationsRepository
                .GetSpecificationById(specificationId)
                .Returns(specification);

            SpecificationVersion clonedSpecificationVersion = null;

            specificationVersionRepository.CreateVersion(Arg.Any<SpecificationVersion>(), Arg.Any<SpecificationVersion>())
                .Returns(_ => (SpecificationVersion)_[0])
                .AndDoes(_ => clonedSpecificationVersion = _.ArgAt<SpecificationVersion>(0));

            specificationsRepository
                .UpdateSpecification(Arg.Any<Specification>())
                .Returns(HttpStatusCode.OK);

            SpecificationsService service = CreateService(
                logs: logger,
                specificationsRepository: specificationsRepository,
                specificationVersionRepository: specificationVersionRepository,
                policiesApiClient: policiesApiClient);

            // Act
            IActionResult result = await service.SetPublishDates(specificationId, specificationPublishDateModel);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(HttpStatusCode.OK);

            specificationsRepository
                .Received(1);

            var externalPublicationDateResult = clonedSpecificationVersion?
                .ExternalPublicationDate;

            var earliestPaymentAvailableDateResult = clonedSpecificationVersion?
               .EarliestPaymentAvailableDate;

            externalPublicationDateResult
                .Should()
                .NotBeNull()
                .Equals(DateTimeOffset.Now.Date);

            earliestPaymentAvailableDateResult
                .Should()
                .NotBeNull()
                .Equals(DateTimeOffset.Now.Date);
        }
    }
}
