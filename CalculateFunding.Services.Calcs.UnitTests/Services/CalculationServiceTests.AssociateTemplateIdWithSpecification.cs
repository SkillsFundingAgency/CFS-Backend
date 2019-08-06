using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using TemplateMetadataModels = CalculateFunding.Common.TemplateMetadata.Models;
using PolicyModels = CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Common.Caching;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Common.ApiClient.Specifications;
using System.Collections.Generic;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [TestMethod]
        public void AssociateTemplateIdWithSpecification_SpecificationIsNull_ReturnsBadRequestObjectResult()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = null;

            CalculationService service = CreateCalculationService(logger: logger);

            // Act
            Func<Task<IActionResult>> func = () => service.AssociateTemplateIdWithSpecification(specificationId, null, null);

            //Assert
            func
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void AssociateTemplateIdWithSpecification_TemplateIdIsNull_ReturnsBadRequestObjectResult()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = "test";
            string templateId = null;

            CalculationService service = CreateCalculationService(logger: logger);

            // Act
            Func<Task<IActionResult>> func = () => service.AssociateTemplateIdWithSpecification(specificationId, templateId, null);

            //Assert
            func
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void AssociateTemplateIdWithSpecification_FundingStreamIdIsNull_ReturnsBadRequestObjectResult()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = "test";
            string templateId = "test";
            string fundingStreamId = null;

            CalculationService service = CreateCalculationService(logger: logger);

            // Act
            Func<Task<IActionResult>> func = () => service.AssociateTemplateIdWithSpecification(specificationId, templateId, fundingStreamId);

            //Assert
            func
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public async Task AssociateTemplateIdWithSpecification_SpecDoesNotExistsInSystem_Returns412WithErrorMessage()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = "test";
            string templateId = "test";
            string fundingStreamId = "test";

            string expectedErrorMessage = $"No specification ID {specificationId} were returned from the repository, result came back null";

            ISpecificationRepository specificationsRepository = CreateSpecificationRepository();
            specificationsRepository
                .GetSpecificationSummaryById(specificationId)
                .Returns<SpecificationSummary>(x => null);

            CalculationService service = CreateCalculationService(logger: logger, specificationRepository: specificationsRepository);

            // Act
            IActionResult result = await service.AssociateTemplateIdWithSpecification(specificationId, templateId, fundingStreamId);

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
        public async Task AssociateTemplateIdWithSpecification_FundingStreamDoesNotExistsOnSpecification_Returns412WithErrorMessage()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = "testSpecification";
            string templateId = "testTemplate";
            string fundingStreamId = "testFundingStream";

            string expectedErrorMessage = $"Specification ID {specificationId} does not have contain given funding stream with ID {fundingStreamId}";

            SpecificationSummary specificationSummary = new SpecificationSummary();

            ISpecificationRepository specificationsRepository = CreateSpecificationRepository();
            specificationsRepository
                .GetSpecificationSummaryById(specificationId)
                .Returns(specificationSummary);

            CalculationService service = CreateCalculationService(logger: logger, specificationRepository: specificationsRepository);

            // Act
            IActionResult result = await service.AssociateTemplateIdWithSpecification(specificationId, templateId, fundingStreamId);

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
        public async Task AssociateTemplateIdWithSpecification_FundingTemplateDoesNotExistsOnSystem_Returns412WithErrorMessage()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = "testSpecification";
            string templateId = "testTemplate";
            string fundingStreamId = "testFundingStream";

            string expectedErrorMessage = $"Retrieve funding template with fundingStreamId: {fundingStreamId} and templateId: {templateId} did not return OK.";

            SpecificationSummary specificationSummary = new SpecificationSummary
            {
                FundingStreams = new List<Reference>
                    {
                        new Reference
                        {
                            Id = fundingStreamId
                        }
                    }
            };

            ISpecificationRepository specificationsRepository = CreateSpecificationRepository();
            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();

            specificationsRepository
                .GetSpecificationSummaryById(specificationId)
                .Returns(specificationSummary);

            var fundingTemplateApiResponse = new ApiResponse<PolicyModels.FundingTemplateContents>(HttpStatusCode.NotFound, null);

            policiesApiClient
                .GetFundingTemplate(Arg.Is(fundingStreamId), Arg.Is(templateId))
                .Returns(fundingTemplateApiResponse);

            CalculationService service = CreateCalculationService(
                logger: logger, 
                specificationRepository: specificationsRepository,
                policiesApiClient: policiesApiClient);

            // Act
            IActionResult result = await service.AssociateTemplateIdWithSpecification(specificationId, templateId, fundingStreamId);

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
        public async Task AssociateTemplateIdWithSpecification_GivenWithValidParameters_AdditionalTemplateMappingItemsRemoved()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = "testSpecification";
            string templateId = "testTemplate";
            string fundingStreamId = "testFundingStream";
            string removedCalculationName = "calculation2Name";

            IEnumerable<TemplateMetadataModels.Calculation> calculations = new List<TemplateMetadataModels.Calculation>
            {
                new TemplateMetadataModels.Calculation
                {
                    Name = "calculation1Name",
                    TemplateCalculationId = 1,
                    ReferenceData = new List<TemplateMetadataModels.ReferenceData>()
                },
                new TemplateMetadataModels.Calculation
                {
                    Name = removedCalculationName,
                    TemplateCalculationId = 2,
                    ReferenceData = new List<TemplateMetadataModels.ReferenceData>()
                },
            };

            SpecificationSummary existingSpecificationSummary = new SpecificationSummary
            {
                FundingStreams = new List<Reference>
                    {
                        new Reference
                        {
                            Id = fundingStreamId
                        }
                    }
            };

            TemplateMetadataModels.TemplateMetadataContents fundingTemplateMetadataContents = CreateTemplateMetadataContents(calculations.Take(1));
            PolicyModels.FundingTemplateContents fundingTemplateContents = new PolicyModels.FundingTemplateContents
            {
                Metadata = fundingTemplateMetadataContents
            };

            ISpecificationRepository specificationsRepository = CreateSpecificationRepository();
            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();
            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();

            specificationsRepository
                .GetSpecificationSummaryById(specificationId)
                .Returns(existingSpecificationSummary);

            ApiResponse<PolicyModels.FundingTemplateContents> fundingTemplateResponse
                = new ApiResponse<PolicyModels.FundingTemplateContents>(HttpStatusCode.OK, fundingTemplateContents);

            policiesApiClient
                .GetFundingTemplate(Arg.Is(fundingStreamId), Arg.Is(templateId))
                .Returns(fundingTemplateResponse);

            specificationsApiClient
                .SetAssignedTemplateVersion(specificationId, templateId, fundingStreamId)
                .Returns(HttpStatusCode.OK);

            List<Calculation> calculationsBySpecId = new List<Calculation>
            {
            };

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<Calculation>>($"{CacheKeys.CalculationsForSpecification}{specificationId}")
                .Returns(calculationsBySpecId);

            CalculationService service = CreateCalculationService(
                logger: logger,
                specificationRepository: specificationsRepository,
                policiesApiClient: policiesApiClient,
                cacheProvider: cacheProvider,
                specificationsApiClient: specificationsApiClient);

            //Act
            IActionResult result = await service.AssociateTemplateIdWithSpecification(specificationId, templateId, fundingStreamId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .Be(HttpStatusCode.OK);
        }

        private TemplateMetadataModels.TemplateMetadataContents CreateTemplateMetadataContents(IEnumerable<TemplateMetadataModels.Calculation> calculations)
        {
            return new TemplateMetadataModels.TemplateMetadataContents
            {
                RootFundingLines = new List<TemplateMetadataModels.FundingLine>
                {
                    new TemplateMetadataModels.FundingLine
                    {
                        Calculations = calculations,
                        FundingLines = new List<TemplateMetadataModels.FundingLine>(),
                    }
                },
            };
        }
        private TemplateMapping GetTemplateMapping(IEnumerable<TemplateMetadataModels.Calculation> calculations)
        {
            List<TemplateMappingItem> calculationTemplateMappingItems = calculations.Select(x => new TemplateMappingItem
            {
                EntityType = TemplateMappingEntityType.Calculation,
                Name = x.Name
            }).ToList();

            return new TemplateMapping
            {
                TemplateMappingItems = calculationTemplateMappingItems
            };
        }

    }
}
