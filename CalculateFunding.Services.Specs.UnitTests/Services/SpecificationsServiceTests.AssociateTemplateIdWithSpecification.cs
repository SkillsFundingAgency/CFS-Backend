using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Specs.Interfaces;
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
using AutoMapper;

namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    public partial class SpecificationsServiceTests
    {
        [TestMethod]
        public void AssociateTemplateIdWithSpecification_SpecificationIsNull_ReturnsBadRequestObjectResult()
        {
            //Arrange
            ILogger logger = CreateLogger();
            string specificationId = null;

            SpecificationsService service = CreateService(logs: logger);

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

            SpecificationsService service = CreateService(logs: logger);

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

            SpecificationsService service = CreateService(logs: logger);

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

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(specificationId)
                .Returns<Specification>(x => null);

            SpecificationsService service = CreateService(logs: logger, specificationsRepository: specificationsRepository);

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
                    }
                }
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();

            specificationsRepository
                .GetSpecificationById(specificationId)
                .Returns(specification);

            var fundingTemplateApiResponse = new ApiResponse<PolicyModels.FundingTemplateContents>(HttpStatusCode.NotFound, null);

            policiesApiClient
                .GetFundingTemplate(Arg.Is(fundingStreamId), Arg.Is(templateId))
                .Returns(fundingTemplateApiResponse);

            SpecificationsService service = CreateService(
                logs: logger, 
                specificationsRepository: specificationsRepository,
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
            string schemaVersion = "1.0";
            string removedCalculationName = "calculation2Name";
            int newSpecificationVersion = 2;

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

            string expectedErrorMessage = $"Template metadata generator with given schema {schemaVersion} could not be retrieved.";

            SpecificationVersion existingSpecificationVersion = new SpecificationVersion
            {
                Version = 1,
                FundingStreams = new List<Reference>
                    {
                        new Reference
                        {
                            Id = fundingStreamId
                        }
                    },
                Calculations = new List<Calculation>
                    {
                        new Calculation{
                            Name = removedCalculationName
                        }
                    }
            };

            SpecificationVersion updatedSpecificationVersion = new SpecificationVersion
            {
                Version = newSpecificationVersion,
                FundingStreams = new List<Reference>
                    {
                        new Reference
                        {
                            Id = fundingStreamId
                        }
                    },
                Calculations = new List<Calculation>()
            };

            Specification existingSpecification = new Specification
            {
                Current = existingSpecificationVersion
            };

            Specification updatedSpecification = new Specification
            {
                Current = updatedSpecificationVersion
            };

            TemplateMetadataModels.TemplateMetadataContents fundingTemplateMetadataContents = CreateTemplateMetadataContents(calculations.Take(1));
            PolicyModels.FundingTemplateContents fundingTemplateContents = new PolicyModels.FundingTemplateContents
            {
                Metadata = fundingTemplateMetadataContents
            };

            TemplateMapping specificationTemplateMetadataContents = GetTemplateMapping(calculations);

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();
            IVersionRepository<SpecificationVersion> specificationVersionRepository = CreateSpecificationVersionRepository();

            specificationsRepository
                .GetSpecificationById(specificationId)
                .Returns(existingSpecification);

            specificationsRepository
                .GetTemplateMappingForSpecificationId(specificationId)
                .Returns(specificationTemplateMetadataContents);

            specificationVersionRepository
                .CreateVersion(Arg.Is<SpecificationVersion>(
                    x => x.TemplateId == templateId && x.Calculations.Count() == 1 && x.Version == newSpecificationVersion), 
                existingSpecification.Current)
                .Returns(updatedSpecificationVersion);

            specificationsRepository
                .UpdateSpecification(Arg.Any<Specification>())
                .Returns(HttpStatusCode.OK);

            ApiResponse<PolicyModels.FundingTemplateContents> fundingTemplateResponse 
                = new ApiResponse<PolicyModels.FundingTemplateContents>(HttpStatusCode.OK, fundingTemplateContents);

            policiesApiClient
                .GetFundingTemplate(Arg.Is(fundingStreamId), Arg.Is(templateId))
                .Returns(fundingTemplateResponse);

            SpecificationsService service = CreateService(
                logs: logger,
                specificationsRepository: specificationsRepository,
                policiesApiClient: policiesApiClient,
                specificationVersionRepository: specificationVersionRepository);

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
