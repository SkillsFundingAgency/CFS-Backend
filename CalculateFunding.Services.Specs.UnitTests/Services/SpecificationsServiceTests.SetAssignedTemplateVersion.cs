using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Specs.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Interfaces;
using PolicyModels = CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    public partial class SpecificationsServiceTests
    {
        [TestMethod]
        public void SetAssignedTemplateVersion_SpecificationIsNull_ReturnsBadRequestObjectResult()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = null;

            SpecificationsService service = CreateService(logs: logger);

            // Act
            Func<Task<IActionResult>> func = () => service.SetAssignedTemplateVersion(specificationId, null, null);

            //Assert
            func
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void SetAssignedTemplateVersion_templateVersionIsNull_ReturnsBadRequestObjectResult()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = "test";
            string templateVersion = null;

            SpecificationsService service = CreateService(logs: logger);

            // Act
            Func<Task<IActionResult>> func = () => service.SetAssignedTemplateVersion(specificationId, null, templateVersion);

            //Assert
            func
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void SetAssignedTemplateVersion_FundingStreamIdIsNull_ReturnsBadRequestObjectResult()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = "test";
            string templateVersion = "test";
            string fundingStreamId = null;

            SpecificationsService service = CreateService(logs: logger);

            // Act
            Func<Task<IActionResult>> func = () => service.SetAssignedTemplateVersion(specificationId, fundingStreamId, templateVersion);

            //Assert
            func
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public async Task SetAssignedTemplateVersion_SpecDoesNotExistsInSystem_Returns412WithErrorMessage()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = "test";
            string templateVersion = "test";
            string fundingStreamId = "test";

            string expectedErrorMessage = $"No specification ID {specificationId} were returned from the repository, result came back null";

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(specificationId)
                .Returns<Specification>(x => null);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            // Act
            IActionResult result = await service.SetAssignedTemplateVersion(specificationId, fundingStreamId, templateVersion);

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
        public async Task SetAssignedTemplateVersion_FundingStreamDoesNotExistsOnSpecification_Returns412WithErrorMessage()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = "testSpecification";
            string templateVersion = "testTemplate";
            string fundingStreamId = "testFundingStream";

            string expectedErrorMessage = $"Specification ID {specificationId} does not contains given funding stream with ID {fundingStreamId}";

            Specification specification = new Specification
            {
                Current = new SpecificationVersion
                {
                    FundingStreams = new List<Reference>()
                }
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(specificationId)
                .Returns(specification);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

            // Act
            IActionResult result = await service.SetAssignedTemplateVersion(specificationId, fundingStreamId, templateVersion);

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
        public async Task SetAssignedTemplateVersion_FundingTemplateDoesNotExistsOnSystem_Returns412WithErrorMessage()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = "testSpecification";
            string templateVersion = "testTemplate";
            string fundingStreamId = "testFundingStream";
            string fundingPeriodId = "testFundingPeriod";

            string expectedErrorMessage = $"Retrieve funding template with fundingStreamId: {fundingStreamId}, fundingPeriodId: {fundingPeriodId} and templateId: {templateVersion} did not return OK.";

            Specification specification = new Specification
            {
                Current = new SpecificationVersion
                {
                    FundingStreams = new List<Reference>
                    {
                        new Reference
                        {
                            Id = fundingStreamId
                        }
                    },
                    FundingPeriod = new Reference { Id = fundingPeriodId }
                }
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();

            specificationsRepository
                .GetSpecificationById(specificationId)
                .Returns(specification);

            var fundingTemplateApiResponse = new ApiResponse<PolicyModels.FundingTemplateContents>(HttpStatusCode.NotFound, null);

            policiesApiClient
                .GetFundingTemplate(Arg.Is(fundingStreamId), Arg.Is(fundingPeriodId),Arg.Is(templateVersion))
                .Returns(fundingTemplateApiResponse);

            SpecificationsService service = CreateService(
                logs: logger,
                specificationsRepository: specificationsRepository,
                policiesApiClient: policiesApiClient);

            // Act
            IActionResult result = await service.SetAssignedTemplateVersion(specificationId, fundingStreamId, templateVersion);

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
        public async Task SetAssignedTemplateVersion_ValidParametersPassed_ReturnsOKAndSetsTemplateVersionForFundingStreamOnSpec()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string templateVersion = "testTemplate";
            string specificationId = "testSpecification";
            string fundingStreamId = "testFundingStream";
            string fundingPeriodId = "testFundingPeriod";

            Specification specification = new Specification
            {
                Current = new SpecificationVersion
                {
                    FundingStreams = new List<Reference>
                    {
                        new Reference
                        {
                            Id = fundingStreamId
                        }
                    },
                    FundingPeriod = new Reference { Id = fundingPeriodId }
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
                .Returns(_ => (SpecificationVersion) _[0])
                .AndDoes(_ => clonedSpecificationVersion = _.ArgAt<SpecificationVersion>(0));

            specificationsRepository
                .UpdateSpecification(Arg.Any<Specification>())
                .Returns(HttpStatusCode.OK);

            var fundingTemplateApiResponse = new ApiResponse<PolicyModels.FundingTemplateContents>(HttpStatusCode.OK, null);

            //TODO; track down where we can grab the instance thats being saved to check we set the correct stuff on it
            
            policiesApiClient
                .GetFundingTemplate(Arg.Is(fundingStreamId), Arg.Is(fundingPeriodId), Arg.Is(templateVersion))
                .Returns(fundingTemplateApiResponse);

            SpecificationsService service = CreateService(
                logs: logger,
                specificationsRepository: specificationsRepository,
                specificationVersionRepository: specificationVersionRepository,
                policiesApiClient: policiesApiClient);

            // Act
            IActionResult result = await service.SetAssignedTemplateVersion(specificationId, fundingStreamId, templateVersion);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(HttpStatusCode.OK);

            Dictionary<string,string> templateIds = clonedSpecificationVersion?
                .TemplateIds;
            
            templateIds?.ContainsKey(fundingStreamId)
                .Should()
                .BeTrue();

            templateIds?[fundingStreamId]
                .Should()
                .Be(templateVersion);
        }
    }
}
