using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Specs.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;


namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    public partial class SpecificationsServiceTests
    {
        [TestMethod]
        public void GetPublishDates_SpecificationIsNull_ReturnsBadRequestObjectResult()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = null;

            SpecificationsService service = CreateService(logs: logger);

            // Act
            Func<Task<IActionResult>> func = () => service.GetPublishDates(specificationId);

            //Assert
            func
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public async Task GetPublishDates_GivenNoSpecificationId_ReturnsBadRequestObject()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            SpecificationsService service = CreateService(logs: logger);

            //Act
            IActionResult result = await service.GetPublishDates(string.Empty);

            //Assert
            result
                .Should()
                .BeOfType<PreconditionFailedResult>()
                .Which
                .Value
                .Should()
                .Be("No specification ID  were returned from the repository, result came back null");

            logger
                .Received(1)
                .Error(Arg.Is("No specification ID  were returned from the repository, result came back null"));
        }

        [TestMethod]
        public async Task GetPublishDates_SpecDoesNotExistsInSystem_Returns412WithErrorMessage()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = "test";
           

            string expectedErrorMessage = $"No specification ID {specificationId} were returned from the repository, result came back null";

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(specificationId)
                .Returns<Specification>(x => null);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            // Act
            IActionResult result = await service.GetPublishDates(specificationId);

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
        public async Task GetPublishDates_FundingStreamDoesNotExistsOnSpecification_Returns412WithErrorMessage()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = "testSpecification";
           

            string expectedErrorMessage = $"Specification ID {specificationId} does not contains current for given specification";

            Specification specification = new Specification
            {
                Id = "test"
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(specificationId)
                .Returns(specification);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            // Act
            IActionResult result = await service.GetPublishDates(specificationId);

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
        public async Task GetPublishDates_ValidParametersPassed_ReturnsOK()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = "testSpecification";


            Specification specification = new Specification
            {
                Current = new SpecificationVersion
                {
                    ExternalPublicationDate = DateTimeOffset.Now.Date,
                    EarliestPaymentAvailableDate = DateTimeOffset.Now.Date
                }
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


            SpecificationsService service = CreateService(
                logs: logger,
                specificationsRepository: specificationsRepository,
                specificationVersionRepository: specificationVersionRepository,
                policiesApiClient: policiesApiClient);

            // Act
            IActionResult result = await service.GetPublishDates(specificationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            var objContent = ((OkObjectResult)result);

            objContent
                .Should()
                .NotBeNull();


            SpecificationPublishDateModel publishDates = objContent.Value as SpecificationPublishDateModel;

            publishDates.EarliestPaymentAvailableDate
                .Should()
                .Equals(specification.Current.EarliestPaymentAvailableDate);

            publishDates.ExternalPublicationDate
               .Should()
               .Equals(specification.Current.ExternalPublicationDate);

        }
    }
}
