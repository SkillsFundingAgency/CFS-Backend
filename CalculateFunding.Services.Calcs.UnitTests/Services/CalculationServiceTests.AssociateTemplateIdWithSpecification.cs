﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using TemplateMetadataModels = CalculateFunding.Common.TemplateMetadata.Models;

namespace CalculateFunding.Services.Calcs.Services
{
    public partial class CalculationServiceTests
    {
        [TestMethod]
        public async Task AssociateTemplateIdWithSpecification_WhenValidTemplateProvidedAndNoExistingTemplateMappingExists_ThenTemplateMappingIsCreated()
        {
            // Arrange
            string specificationId = "spec1";
            string templateVersion = "1.0";
            string fundingStreamId = "PSG";
            string templateId = "2.2";

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();

            ISpecificationRepository specificationsRepository = CreateSpecificationRepository();

            SpecificationSummary specificationSummary = new SpecificationSummary()
            {
                TemplateIds = new Dictionary<string, string> { { fundingStreamId, templateId } },
                FundingStreams = new List<Reference>()
                {
                    new Reference(fundingStreamId, "PE and Sports"),
                },
            };

            specificationsRepository
               .GetSpecificationSummaryById(specificationId)
               .Returns(specificationSummary);

            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();

            TemplateMetadataContents fundingMetadataContents = new TemplateMetadataContents()
            {
                RootFundingLines = new List<FundingLine>()
                {
                    new FundingLine()
                    {
                        Name = "TotalFundingLine",
                        Calculations = new List<TemplateMetadataModels.Calculation>()
                        {
                            new TemplateMetadataModels.Calculation()
                            {
                                Name = "Calculation 1",
                                TemplateCalculationId = 1,
                                Type = Common.TemplateMetadata.Enums.CalculationType.Cash,
                                ValueFormat = Common.TemplateMetadata.Enums.CalculationValueFormat.Currency,
                                AggregationType = Common.TemplateMetadata.Enums.AggregationType.Sum,
                                ReferenceData = new List<ReferenceData>
                                {
                                    new ReferenceData
                                    {
                                        AggregationType = Common.TemplateMetadata.Enums.AggregationType.None,
                                        Format = Common.TemplateMetadata.Enums.ReferenceDataValueFormat.Number,
                                        Name = "Reference Data 1",
                                        TemplateReferenceId = 3
                                    }
                                }
                            },
                        },
                        FundingLines = new List<FundingLine>()
                        {
                            new FundingLine()
                            {
                                Name = "Third Funding Line",
                                Calculations = new List<TemplateMetadataModels.Calculation>()
                                {
                                    new TemplateMetadataModels.Calculation()
                                    {
                                        Name = "Calculation 3",
                                        TemplateCalculationId = 5,
                                        Type = Common.TemplateMetadata.Enums.CalculationType.Drilldown,
                                        ValueFormat = Common.TemplateMetadata.Enums.CalculationValueFormat.Percentage,
                                        AggregationType = Common.TemplateMetadata.Enums.AggregationType.None,
                                        ReferenceData = new List<ReferenceData>
                                        {
                                            new ReferenceData
                                            {
                                                AggregationType = Common.TemplateMetadata.Enums.AggregationType.Average,
                                                Format = Common.TemplateMetadata.Enums.ReferenceDataValueFormat.Currency,
                                                Name = "Reference Data 3",
                                                TemplateReferenceId = 6
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                    new FundingLine()
                    {
                        Name = "Second Funding Line",
                        Calculations = new List<TemplateMetadataModels.Calculation>()
                        {
                            new TemplateMetadataModels.Calculation()
                            {
                                Name = "Calculation 2",
                                TemplateCalculationId = 2,
                                Type = Common.TemplateMetadata.Enums.CalculationType.PerPupilFunding,
                                ValueFormat = Common.TemplateMetadata.Enums.CalculationValueFormat.Number,
                                AggregationType = Common.TemplateMetadata.Enums.AggregationType.None,
                                ReferenceData = new List<ReferenceData>
                                {
                                    new ReferenceData
                                    {
                                        Name = "Reference Data 2",
                                        AggregationType = Common.TemplateMetadata.Enums.AggregationType.Average,
                                        Format = Common.TemplateMetadata.Enums.ReferenceDataValueFormat.Percentage,
                                        TemplateReferenceId = 4
                                    }
                                }
                            },
                        }
                    },
                }
            };

            List<CalculationMetadata> specificationCalculations = new List<CalculationMetadata>();
            calculationsRepository
                .GetCalculationsMetatadataBySpecificationId(Arg.Is(specificationId))
                .Returns(specificationCalculations);

            policiesApiClient
                .GetFundingTemplateContents(fundingStreamId, templateVersion)
                .Returns(new ApiResponse<TemplateMetadataContents>(HttpStatusCode.OK, fundingMetadataContents));

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .SetAssignedTemplateVersion(Arg.Is(specificationId), Arg.Is(templateVersion), Arg.Is(fundingStreamId))
                .Returns(HttpStatusCode.OK);

            TemplateMapping savedTemplateMapping = null;

            await calculationsRepository
               .UpdateTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId), Arg.Do<TemplateMapping>(r => savedTemplateMapping = r));

            CalculationService service = CreateCalculationService(
                calculationsRepository: calculationsRepository,
                specificationRepository: specificationsRepository,
                policiesApiClient: policiesApiClient,
                specificationsApiClient: specificationsApiClient);

            // Act
            var result = await service.AssociateTemplateIdWithSpecification(specificationId, templateVersion, fundingStreamId);

            // Assert
            await policiesApiClient
                .Received(1)
                .GetFundingTemplateContents(Arg.Is(fundingStreamId), Arg.Is(templateVersion));

            await calculationsRepository
                .Received(1)
                .UpdateTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId), Arg.Is(savedTemplateMapping));

            savedTemplateMapping
                .Should()
                .NotBeNull();

            TemplateMapping expectedTemplateMapping = new TemplateMapping()
            {
                FundingStreamId = fundingStreamId,
                SpecificationId = specificationId,
                TemplateMappingItems = new List<TemplateMappingItem>()
                {
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 1",
                        TemplateId = 1,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 2",
                        TemplateId = 2,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.ReferenceData,
                        Name = "Reference Data 1",
                        TemplateId = 3,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.ReferenceData,
                        Name = "Reference Data 2",
                        TemplateId = 4,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 3",
                        TemplateId = 5,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.ReferenceData,
                        Name = "Reference Data 3",
                        TemplateId = 6,
                    },
                }
            };

            savedTemplateMapping
                .Should()
                .BeEquivalentTo(expectedTemplateMapping);
        }

        [TestMethod]
        public async Task AssociateTemplateIdWithSpecification_WhenValidTemplateProvidedAndNoExistingTemplateMappingExistsAndNoCalculationsOrReferenceDataExist_ThenTemplateMappingIsCreated()
        {
            // Arrange
            string specificationId = "spec1";
            string templateVersion = "1.0";
            string fundingStreamId = "PSG";
            string templateId = "2.2";

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();

            ISpecificationRepository specificationsRepository = CreateSpecificationRepository();

            SpecificationSummary specificationSummary = new SpecificationSummary()
            {
                TemplateIds = new Dictionary<string, string> { { fundingStreamId, templateId } },
                FundingStreams = new List<Reference>()
                {
                    new Reference(fundingStreamId, "PE and Sports"),
                },
            };

            specificationsRepository
               .GetSpecificationSummaryById(specificationId)
               .Returns(specificationSummary);

            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();

            TemplateMetadataContents fundingMetadataContents = new TemplateMetadataContents()
            {
                RootFundingLines = new List<FundingLine>()
                {
                    new FundingLine()
                    {
                        Name = "TotalFundingLine",
                    },
                    new FundingLine()
                    {
                        Name = "Second Funding Line",
                    },
                }
            };

            List<CalculationMetadata> specificationCalculations = new List<CalculationMetadata>();
            calculationsRepository
                .GetCalculationsMetatadataBySpecificationId(Arg.Is(specificationId))
                .Returns(specificationCalculations);

            policiesApiClient
                .GetFundingTemplateContents(fundingStreamId, templateVersion)
                .Returns(new ApiResponse<TemplateMetadataContents>(HttpStatusCode.OK, fundingMetadataContents));

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .SetAssignedTemplateVersion(Arg.Is(specificationId), Arg.Is(templateVersion), Arg.Is(fundingStreamId))
                .Returns(HttpStatusCode.OK);

            TemplateMapping savedTemplateMapping = null;

            await calculationsRepository
               .UpdateTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId), Arg.Do<TemplateMapping>(r => savedTemplateMapping = r));

            CalculationService service = CreateCalculationService(
                calculationsRepository: calculationsRepository,
                specificationRepository: specificationsRepository,
                policiesApiClient: policiesApiClient,
                specificationsApiClient: specificationsApiClient);

            // Act
            var result = await service.AssociateTemplateIdWithSpecification(specificationId, templateVersion, fundingStreamId);

            // Assert
            await policiesApiClient
                .Received(1)
                .GetFundingTemplateContents(Arg.Is(fundingStreamId), Arg.Is(templateVersion));

            await calculationsRepository
                .Received(1)
                .UpdateTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId), Arg.Is(savedTemplateMapping));

            savedTemplateMapping
                .Should()
                .NotBeNull();

            TemplateMapping expectedTemplateMapping = new TemplateMapping()
            {
                FundingStreamId = fundingStreamId,
                SpecificationId = specificationId,
                TemplateMappingItems = new List<TemplateMappingItem>(),
            };

            savedTemplateMapping
                .Should()
                .BeEquivalentTo(expectedTemplateMapping);
        }

        [TestMethod]
        public async Task AssociateTemplateIdWithSpecification_WhenConfigurationIsTheSame_ThenTemplateMappingIsNotUpdated()
        {
            // Arrange
            string specificationId = "spec1";
            string templateVersion = "1.0";
            string fundingStreamId = "PSG";
            string templateId = "2.2";

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();

            ISpecificationRepository specificationsRepository = CreateSpecificationRepository();

            SpecificationSummary specificationSummary = new SpecificationSummary()
            {
                TemplateIds = new Dictionary<string, string> { { fundingStreamId, templateId } },
                FundingStreams = new List<Reference>()
                {
                    new Reference(fundingStreamId, "PE and Sports"),
                },
            };

            specificationsRepository
               .GetSpecificationSummaryById(specificationId)
               .Returns(specificationSummary);

            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();

            TemplateMetadataContents fundingMetadataContents = new TemplateMetadataContents()
            {
                RootFundingLines = new List<FundingLine>()
                {
                    new FundingLine()
                    {
                        Name = "TotalFundingLine",
                        Calculations = new List<TemplateMetadataModels.Calculation>()
                        {
                            new TemplateMetadataModels.Calculation()
                            {
                                Name = "Calculation 1",
                                TemplateCalculationId = 1,
                                Type = Common.TemplateMetadata.Enums.CalculationType.Cash,
                                ValueFormat = Common.TemplateMetadata.Enums.CalculationValueFormat.Currency,
                                AggregationType = Common.TemplateMetadata.Enums.AggregationType.Sum,
                                ReferenceData = new List<ReferenceData>
                                {
                                    new ReferenceData
                                    {
                                        AggregationType = Common.TemplateMetadata.Enums.AggregationType.None,
                                        Format = Common.TemplateMetadata.Enums.ReferenceDataValueFormat.Number,
                                        Name = "Reference Data 1",
                                        TemplateReferenceId = 3
                                    }
                                }
                            },
                        }
                    },
                    new FundingLine()
                    {
                        Name = "Second Funding Line",
                        Calculations = new List<TemplateMetadataModels.Calculation>()
                        {
                            new TemplateMetadataModels.Calculation()
                            {
                                Name = "Calculation 2",
                                TemplateCalculationId = 2,
                                Type = Common.TemplateMetadata.Enums.CalculationType.PerPupilFunding,
                                ValueFormat = Common.TemplateMetadata.Enums.CalculationValueFormat.Number,
                                AggregationType = Common.TemplateMetadata.Enums.AggregationType.None,
                                ReferenceData = new List<ReferenceData>
                                {
                                    new ReferenceData
                                    {
                                        Name = "Reference Data 2",
                                        AggregationType = Common.TemplateMetadata.Enums.AggregationType.Average,
                                        Format = Common.TemplateMetadata.Enums.ReferenceDataValueFormat.Percentage,
                                        TemplateReferenceId = 4
                                    }
                                }
                            },
                        }
                    },
                }
            };

            List<CalculationMetadata> specificationCalculations = new List<CalculationMetadata>();
            calculationsRepository
                .GetCalculationsMetatadataBySpecificationId(Arg.Is(specificationId))
                .Returns(specificationCalculations);

            TemplateMapping existingTemplateMapping = new TemplateMapping()
            {
                FundingStreamId = fundingStreamId,
                SpecificationId = specificationId,
                TemplateMappingItems = new List<TemplateMappingItem>()
                {
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 1",
                        TemplateId = 1,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 2",
                        TemplateId = 2,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.ReferenceData,
                        Name = "Reference Data 1",
                        TemplateId = 3,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.ReferenceData,
                        Name = "Reference Data 2",
                        TemplateId = 4,
                    },
                }
            };

            calculationsRepository
                .GetTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId))
                .Returns(existingTemplateMapping);

            policiesApiClient
                .GetFundingTemplateContents(fundingStreamId, templateVersion)
                .Returns(new ApiResponse<TemplateMetadataContents>(HttpStatusCode.OK, fundingMetadataContents));

            CalculationService service = CreateCalculationService(
                calculationsRepository: calculationsRepository,
                specificationRepository: specificationsRepository,
                policiesApiClient: policiesApiClient);

            // Act
            var result = await service.AssociateTemplateIdWithSpecification(specificationId, templateVersion, fundingStreamId);

            // Assert
            await calculationsRepository
                .Received(1)
                .GetTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId));

            await policiesApiClient
                .Received(1)
                .GetFundingTemplateContents(Arg.Is(fundingStreamId), Arg.Is(templateVersion));

            await calculationsRepository
                .Received(0)
                .UpdateTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId), Arg.Any<TemplateMapping>());
        }

        [TestMethod]
        public async Task AssociateTemplateIdWithSpecification_WhenValidTemplateProvidedAndExistingTemplateMappingExistsWithCalculationsWithNewCalculationsToAdd_ThenTemplateMappingIsCreated()
        {
            // Arrange
            string specificationId = "spec1";
            string templateVersion = "1.0";
            string fundingStreamId = "PSG";
            string templateId = "2.2";

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();

            ISpecificationRepository specificationsRepository = CreateSpecificationRepository();

            SpecificationSummary specificationSummary = new SpecificationSummary()
            {
                TemplateIds = new Dictionary<string, string> { { fundingStreamId, templateId } },
                FundingStreams = new List<Reference>()
                {
                    new Reference(fundingStreamId, "PE and Sports"),
                },
            };

            specificationsRepository
               .GetSpecificationSummaryById(specificationId)
               .Returns(specificationSummary);

            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();

            TemplateMetadataContents fundingMetadataContents = new TemplateMetadataContents()
            {
                RootFundingLines = new List<FundingLine>()
                {
                    new FundingLine()
                    {
                        Name = "TotalFundingLine",
                        Calculations = new List<TemplateMetadataModels.Calculation>()
                        {
                            new TemplateMetadataModels.Calculation()
                            {
                                Name = "Calculation 1",
                                TemplateCalculationId = 1,
                                Type = Common.TemplateMetadata.Enums.CalculationType.Cash,
                                ValueFormat = Common.TemplateMetadata.Enums.CalculationValueFormat.Currency,
                                AggregationType = Common.TemplateMetadata.Enums.AggregationType.Sum,
                            },
                        }
                    },
                    new FundingLine()
                    {
                        Name = "Second Funding Line",
                        Calculations = new List<TemplateMetadataModels.Calculation>()
                        {
                            new TemplateMetadataModels.Calculation()
                            {
                                Name = "Calculation 2",
                                TemplateCalculationId = 2,
                                Type = Common.TemplateMetadata.Enums.CalculationType.PerPupilFunding,
                                ValueFormat = Common.TemplateMetadata.Enums.CalculationValueFormat.Number,
                                AggregationType = Common.TemplateMetadata.Enums.AggregationType.None,
                            },
                            new TemplateMetadataModels.Calculation()
                            {
                                Name = "Calculation 5",
                                TemplateCalculationId = 5,
                                Type = Common.TemplateMetadata.Enums.CalculationType.LumpSum,
                                ValueFormat = Common.TemplateMetadata.Enums.CalculationValueFormat.Number,
                                AggregationType = Common.TemplateMetadata.Enums.AggregationType.None,
                            },
                        }
                    },
                }
            };

            List<CalculationMetadata> specificationCalculations = new List<CalculationMetadata>();
            calculationsRepository
                .GetCalculationsMetatadataBySpecificationId(Arg.Is(specificationId))
                .Returns(specificationCalculations);

            TemplateMapping existingTemplateMapping = new TemplateMapping()
            {
                FundingStreamId = fundingStreamId,
                SpecificationId = specificationId,
                TemplateMappingItems = new List<TemplateMappingItem>()
                {
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 1",
                        TemplateId = 1,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 2",
                        TemplateId = 2,
                    },
                }
            };

            calculationsRepository
                .GetTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId))
                .Returns(existingTemplateMapping);

            policiesApiClient
                .GetFundingTemplateContents(fundingStreamId, templateVersion)
                .Returns(new ApiResponse<TemplateMetadataContents>(HttpStatusCode.OK, fundingMetadataContents));

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .SetAssignedTemplateVersion(Arg.Is(specificationId), Arg.Is(templateVersion), Arg.Is(fundingStreamId))
                .Returns(HttpStatusCode.OK);

            CalculationService service = CreateCalculationService(
                calculationsRepository: calculationsRepository,
                specificationRepository: specificationsRepository,
                policiesApiClient: policiesApiClient,
                specificationsApiClient: specificationsApiClient);

            TemplateMapping savedTemplateMapping = null;

            await calculationsRepository
               .UpdateTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId), Arg.Do<TemplateMapping>(r => savedTemplateMapping = r));

            // Act
            var result = await service.AssociateTemplateIdWithSpecification(specificationId, templateVersion, fundingStreamId);

            // Assert
            await calculationsRepository
                .Received(1)
                .GetTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId));

            await policiesApiClient
                .Received(1)
                .GetFundingTemplateContents(Arg.Is(fundingStreamId), Arg.Is(templateVersion));

            await calculationsRepository
                .Received(1)
                .UpdateTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId), Arg.Is(savedTemplateMapping));

            savedTemplateMapping
                .Should()
                .NotBeNull();

            TemplateMapping expectedTemplateMapping = new TemplateMapping()
            {
                FundingStreamId = fundingStreamId,
                SpecificationId = specificationId,
                TemplateMappingItems = new List<TemplateMappingItem>()
                {
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 1",
                        TemplateId = 1,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 2",
                        TemplateId = 2,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 5",
                        TemplateId = 5,
                    },
                }
            };

            savedTemplateMapping
                .Should()
                .BeEquivalentTo(expectedTemplateMapping);
        }

        [TestMethod]
        public async Task AssociateTemplateIdWithSpecification_WhenValidTemplateProvidedAndExistingTemplateMappingExistsWithCalculationsWithNewCalculationsToAddInNesting_ThenTemplateMappingIsCreated()
        {
            // Arrange
            string specificationId = "spec1";
            string templateVersion = "1.0";
            string fundingStreamId = "PSG";
            string templateId = "2.2";

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();

            ISpecificationRepository specificationsRepository = CreateSpecificationRepository();

            SpecificationSummary specificationSummary = new SpecificationSummary()
            {
                TemplateIds = new Dictionary<string, string> { { fundingStreamId, templateId } },
                FundingStreams = new List<Reference>()
                {
                    new Reference(fundingStreamId, "PE and Sports"),
                },
            };

            specificationsRepository
               .GetSpecificationSummaryById(specificationId)
               .Returns(specificationSummary);

            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();

            TemplateMetadataContents fundingMetadataContents = new TemplateMetadataContents()
            {
                RootFundingLines = new List<FundingLine>()
                {
                    new FundingLine()
                    {
                        Name = "TotalFundingLine",
                        Calculations = new List<TemplateMetadataModels.Calculation>()
                        {
                            new TemplateMetadataModels.Calculation()
                            {
                                Name = "Calculation 1",
                                TemplateCalculationId = 1,
                                Type = Common.TemplateMetadata.Enums.CalculationType.Cash,
                                ValueFormat = Common.TemplateMetadata.Enums.CalculationValueFormat.Currency,
                                AggregationType = Common.TemplateMetadata.Enums.AggregationType.Sum,
                                Calculations = new List<TemplateMetadataModels.Calculation>()
                                {
                                    new TemplateMetadataModels.Calculation()
                                    {
                                        Name = "Calculation Nested 1",
                                        TemplateCalculationId = 100,
                                        Type = Common.TemplateMetadata.Enums.CalculationType.Cash,
                                        ValueFormat = Common.TemplateMetadata.Enums.CalculationValueFormat.Currency,
                                        AggregationType = Common.TemplateMetadata.Enums.AggregationType.Sum,
                                        ReferenceData = new List<ReferenceData>
                                        {
                                            new ReferenceData
                                            {
                                                AggregationType = Common.TemplateMetadata.Enums.AggregationType.None,
                                                Format = Common.TemplateMetadata.Enums.ReferenceDataValueFormat.Number,
                                                Name = "Reference Data Nested 1",
                                                TemplateReferenceId = 200
                                            }
                                        }
                                    },
                                }
                            },
                        }
                    },
                    new FundingLine()
                    {
                        Name = "Second Funding Line",
                        Calculations = new List<TemplateMetadataModels.Calculation>()
                        {
                            new TemplateMetadataModels.Calculation()
                            {
                                Name = "Calculation 2",
                                TemplateCalculationId = 2,
                                Type = Common.TemplateMetadata.Enums.CalculationType.PerPupilFunding,
                                ValueFormat = Common.TemplateMetadata.Enums.CalculationValueFormat.Number,
                                AggregationType = Common.TemplateMetadata.Enums.AggregationType.None,
                            },
                            new TemplateMetadataModels.Calculation()
                            {
                                Name = "Calculation 5",
                                TemplateCalculationId = 5,
                                Type = Common.TemplateMetadata.Enums.CalculationType.LumpSum,
                                ValueFormat = Common.TemplateMetadata.Enums.CalculationValueFormat.Number,
                                AggregationType = Common.TemplateMetadata.Enums.AggregationType.None,
                            },
                        }
                    },
                }
            };

            List<CalculationMetadata> specificationCalculations = new List<CalculationMetadata>();
            calculationsRepository
                .GetCalculationsMetatadataBySpecificationId(Arg.Is(specificationId))
                .Returns(specificationCalculations);

            TemplateMapping existingTemplateMapping = new TemplateMapping()
            {
                FundingStreamId = fundingStreamId,
                SpecificationId = specificationId,
                TemplateMappingItems = new List<TemplateMappingItem>()
                {
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 1",
                        TemplateId = 1,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 2",
                        TemplateId = 2,
                    },
                }
            };

            calculationsRepository
                .GetTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId))
                .Returns(existingTemplateMapping);

            policiesApiClient
                .GetFundingTemplateContents(fundingStreamId, templateVersion)
                .Returns(new ApiResponse<TemplateMetadataContents>(HttpStatusCode.OK, fundingMetadataContents));

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .SetAssignedTemplateVersion(Arg.Is(specificationId), Arg.Is(templateVersion), Arg.Is(fundingStreamId))
                .Returns(HttpStatusCode.OK);

            CalculationService service = CreateCalculationService(
                calculationsRepository: calculationsRepository,
                specificationRepository: specificationsRepository,
                policiesApiClient: policiesApiClient,
                specificationsApiClient: specificationsApiClient);

            TemplateMapping savedTemplateMapping = null;

            await calculationsRepository
               .UpdateTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId), Arg.Do<TemplateMapping>(r => savedTemplateMapping = r));

            // Act
            var result = await service.AssociateTemplateIdWithSpecification(specificationId, templateVersion, fundingStreamId);

            // Assert
            await calculationsRepository
                .Received(1)
                .GetTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId));

            await policiesApiClient
                .Received(1)
                .GetFundingTemplateContents(Arg.Is(fundingStreamId), Arg.Is(templateVersion));

            await calculationsRepository
                .Received(1)
                .UpdateTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId), Arg.Is(savedTemplateMapping));

            savedTemplateMapping
                .Should()
                .NotBeNull();

            TemplateMapping expectedTemplateMapping = new TemplateMapping()
            {
                FundingStreamId = fundingStreamId,
                SpecificationId = specificationId,
                TemplateMappingItems = new List<TemplateMappingItem>()
                {
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 1",
                        TemplateId = 1,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation Nested 1",
                        TemplateId = 100,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 2",
                        TemplateId = 2,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 5",
                        TemplateId = 5,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.ReferenceData,
                        Name = "Reference Data Nested 1",
                        TemplateId = 200,
                    },
                }
            };

            savedTemplateMapping
                .Should()
                .BeEquivalentTo(expectedTemplateMapping);
        }


        [TestMethod]
        public async Task AssociateTemplateIdWithSpecification_WhenValidTemplateProvidedAndExistingTemplateMappingExistsWithReferenceDataWithNewReferenceDataToAdd_ThenTemplateMappingIsCreated()
        {
            // Arrange
            string specificationId = "spec1";
            string templateVersion = "1.0";
            string fundingStreamId = "PSG";
            string templateId = "2.2";

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();

            ISpecificationRepository specificationsRepository = CreateSpecificationRepository();

            SpecificationSummary specificationSummary = new SpecificationSummary()
            {
                TemplateIds = new Dictionary<string, string> { { fundingStreamId, templateId } },
                FundingStreams = new List<Reference>()
                {
                    new Reference(fundingStreamId, "PE and Sports"),
                },
            };

            specificationsRepository
               .GetSpecificationSummaryById(specificationId)
               .Returns(specificationSummary);

            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();

            TemplateMetadataContents fundingMetadataContents = new TemplateMetadataContents()
            {
                RootFundingLines = new List<FundingLine>()
                {
                    new FundingLine()
                    {
                        Name = "TotalFundingLine",
                        Calculations = new List<TemplateMetadataModels.Calculation>()
                        {
                            new TemplateMetadataModels.Calculation()
                            {
                                Name = "Calculation 1",
                                TemplateCalculationId = 1,
                                Type = Common.TemplateMetadata.Enums.CalculationType.Cash,
                                ValueFormat = Common.TemplateMetadata.Enums.CalculationValueFormat.Currency,
                                AggregationType = Common.TemplateMetadata.Enums.AggregationType.Sum,
                                ReferenceData = new List<ReferenceData>
                                {
                                    new ReferenceData
                                    {
                                        AggregationType = Common.TemplateMetadata.Enums.AggregationType.None,
                                        Format = Common.TemplateMetadata.Enums.ReferenceDataValueFormat.Number,
                                        Name = "Reference Data 1",
                                        TemplateReferenceId = 3
                                    }
                                }
                            },
                        }
                    },
                    new FundingLine()
                    {
                        Name = "Second Funding Line",
                        Calculations = new List<TemplateMetadataModels.Calculation>()
                        {
                            new TemplateMetadataModels.Calculation()
                            {
                                Name = "Calculation 2",
                                TemplateCalculationId = 2,
                                Type = Common.TemplateMetadata.Enums.CalculationType.PerPupilFunding,
                                ValueFormat = Common.TemplateMetadata.Enums.CalculationValueFormat.Number,
                                AggregationType = Common.TemplateMetadata.Enums.AggregationType.None,
                                ReferenceData = new List<ReferenceData>
                                {
                                    new ReferenceData
                                    {
                                        Name = "Reference Data 2",
                                        AggregationType = Common.TemplateMetadata.Enums.AggregationType.Average,
                                        Format = Common.TemplateMetadata.Enums.ReferenceDataValueFormat.Percentage,
                                        TemplateReferenceId = 4
                                    }
                                }
                            },
                        }
                    },
                }
            };

            List<CalculationMetadata> specificationCalculations = new List<CalculationMetadata>();
            calculationsRepository
                .GetCalculationsMetatadataBySpecificationId(Arg.Is(specificationId))
                .Returns(specificationCalculations);

            TemplateMapping existingTemplateMapping = new TemplateMapping()
            {
                FundingStreamId = fundingStreamId,
                SpecificationId = specificationId,
                TemplateMappingItems = new List<TemplateMappingItem>()
                {
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 1",
                        TemplateId = 1,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 2",
                        TemplateId = 2,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.ReferenceData,
                        Name = "Reference Data 1",
                        TemplateId = 3,
                    }
                }
            };

            calculationsRepository
                .GetTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId))
                .Returns(existingTemplateMapping);

            policiesApiClient
                .GetFundingTemplateContents(fundingStreamId, templateVersion)
                .Returns(new ApiResponse<TemplateMetadataContents>(HttpStatusCode.OK, fundingMetadataContents));

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .SetAssignedTemplateVersion(Arg.Is(specificationId), Arg.Is(templateVersion), Arg.Is(fundingStreamId))
                .Returns(HttpStatusCode.OK);

            CalculationService service = CreateCalculationService(
                calculationsRepository: calculationsRepository,
                specificationRepository: specificationsRepository,
                policiesApiClient: policiesApiClient,
                specificationsApiClient: specificationsApiClient);

            TemplateMapping savedTemplateMapping = null;

            await calculationsRepository
               .UpdateTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId), Arg.Do<TemplateMapping>(r => savedTemplateMapping = r));

            // Act
            var result = await service.AssociateTemplateIdWithSpecification(specificationId, templateVersion, fundingStreamId);

            // Assert
            await calculationsRepository
                .Received(1)
                .GetTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId));

            await policiesApiClient
                .Received(1)
                .GetFundingTemplateContents(Arg.Is(fundingStreamId), Arg.Is(templateVersion));

            await calculationsRepository
                .Received(1)
                .UpdateTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId), Arg.Is(savedTemplateMapping));

            savedTemplateMapping
                .Should()
                .NotBeNull();

            TemplateMapping expectedTemplateMapping = new TemplateMapping()
            {
                FundingStreamId = fundingStreamId,
                SpecificationId = specificationId,
                TemplateMappingItems = new List<TemplateMappingItem>()
                {
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 1",
                        TemplateId = 1,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 2",
                        TemplateId = 2,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.ReferenceData,
                        Name = "Reference Data 1",
                        TemplateId = 3,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.ReferenceData,
                        Name = "Reference Data 2",
                        TemplateId = 4,
                    },
                }
            };

            savedTemplateMapping
                .Should()
                .BeEquivalentTo(expectedTemplateMapping);
        }

        [TestMethod]
        public async Task AssociateTemplateIdWithSpecification_WhenExistingMappedCalculationIsRemovedFromTemplate_ThenTemplateMappingIsUnmapped()
        {
            // Arrange
            string specificationId = "spec1";
            string templateVersion = "1.0";
            string fundingStreamId = "PSG";
            string templateId = "2.2";

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();

            ISpecificationRepository specificationsRepository = CreateSpecificationRepository();

            SpecificationSummary specificationSummary = new SpecificationSummary()
            {
                TemplateIds = new Dictionary<string, string> { { fundingStreamId, templateId } },
                FundingStreams = new List<Reference>()
                {
                    new Reference(fundingStreamId, "PE and Sports"),
                },
            };

            specificationsRepository
               .GetSpecificationSummaryById(specificationId)
               .Returns(specificationSummary);

            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();

            TemplateMetadataContents fundingMetadataContents = new TemplateMetadataContents()
            {
                RootFundingLines = new List<FundingLine>()
                {
                    new FundingLine()
                    {
                        Name = "TotalFundingLine",
                        Calculations = new List<TemplateMetadataModels.Calculation>()
                        {
                            new TemplateMetadataModels.Calculation()
                            {
                                Name = "Calculation 1",
                                TemplateCalculationId = 1,
                                Type = Common.TemplateMetadata.Enums.CalculationType.Cash,
                                ValueFormat = Common.TemplateMetadata.Enums.CalculationValueFormat.Currency,
                                AggregationType = Common.TemplateMetadata.Enums.AggregationType.Sum,
                            },
                        }
                    },
                    new FundingLine()
                    {
                        Name = "Second Funding Line",
                        Calculations = new List<TemplateMetadataModels.Calculation>()
                        {
                            new TemplateMetadataModels.Calculation()
                            {
                                Name = "Calculation 5",
                                TemplateCalculationId = 5,
                                Type = Common.TemplateMetadata.Enums.CalculationType.LumpSum,
                                ValueFormat = Common.TemplateMetadata.Enums.CalculationValueFormat.Number,
                                AggregationType = Common.TemplateMetadata.Enums.AggregationType.None,
                            },
                        }
                    },
                }
            };

            List<CalculationMetadata> specificationCalculations = new List<CalculationMetadata>();
            calculationsRepository
                .GetCalculationsMetatadataBySpecificationId(Arg.Is(specificationId))
                .Returns(specificationCalculations);

            TemplateMapping existingTemplateMapping = new TemplateMapping()
            {
                FundingStreamId = fundingStreamId,
                SpecificationId = specificationId,
                TemplateMappingItems = new List<TemplateMappingItem>()
                {
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 1",
                        TemplateId = 1,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 2",
                        TemplateId = 2,
                    },
                }
            };

            calculationsRepository
                .GetTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId))
                .Returns(existingTemplateMapping);

            policiesApiClient
                .GetFundingTemplateContents(fundingStreamId, templateVersion)
                .Returns(new ApiResponse<TemplateMetadataContents>(HttpStatusCode.OK, fundingMetadataContents));

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .SetAssignedTemplateVersion(Arg.Is(specificationId), Arg.Is(templateVersion), Arg.Is(fundingStreamId))
                .Returns(HttpStatusCode.OK);

            TemplateMapping savedTemplateMapping = null;

            await calculationsRepository
               .UpdateTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId), Arg.Do<TemplateMapping>(r => savedTemplateMapping = r));

            CalculationService service = CreateCalculationService(
                calculationsRepository: calculationsRepository,
                specificationRepository: specificationsRepository,
                policiesApiClient: policiesApiClient,
                specificationsApiClient: specificationsApiClient);

            // Act
            var result = await service.AssociateTemplateIdWithSpecification(specificationId, templateVersion, fundingStreamId);

            // Assert
            await calculationsRepository
                .Received(1)
                .GetTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId));

            await policiesApiClient
                .Received(1)
                .GetFundingTemplateContents(Arg.Is(fundingStreamId), Arg.Is(templateVersion));

            await calculationsRepository
                .Received(1)
                .UpdateTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId), Arg.Is(savedTemplateMapping));

            savedTemplateMapping
                .Should()
                .NotBeNull();

            TemplateMapping expectedTemplateMapping = new TemplateMapping()
            {
                FundingStreamId = fundingStreamId,
                SpecificationId = specificationId,
                TemplateMappingItems = new List<TemplateMappingItem>()
                {
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 1",
                        TemplateId = 1,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 5",
                        TemplateId = 5,
                    },
                }
            };

            savedTemplateMapping
                .Should()
                .BeEquivalentTo(expectedTemplateMapping);
        }

        [TestMethod]
        public async Task AssociateTemplateIdWithSpecification_WhenExistingMappedReferenceDataIsRemovedFromTemplate_ThenTemplateMappingIsUnmapped()
        {
            // Arrange
            string specificationId = "spec1";
            string templateVersion = "1.0";
            string fundingStreamId = "PSG";
            string templateId = "2.2";

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();

            ISpecificationRepository specificationsRepository = CreateSpecificationRepository();

            SpecificationSummary specificationSummary = new SpecificationSummary()
            {
                TemplateIds = new Dictionary<string, string> { { fundingStreamId, templateId } },
                FundingStreams = new List<Reference>()
                {
                    new Reference(fundingStreamId, "PE and Sports"),
                },
            };

            specificationsRepository
               .GetSpecificationSummaryById(specificationId)
               .Returns(specificationSummary);

            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();

            TemplateMetadataContents fundingMetadataContents = new TemplateMetadataContents()
            {
                RootFundingLines = new List<FundingLine>()
                {
                    new FundingLine()
                    {
                        Name = "TotalFundingLine",
                        Calculations = new List<TemplateMetadataModels.Calculation>()
                        {
                            new TemplateMetadataModels.Calculation()
                            {
                                Name = "Calculation 1",
                                TemplateCalculationId = 1,
                                Type = Common.TemplateMetadata.Enums.CalculationType.Cash,
                                ValueFormat = Common.TemplateMetadata.Enums.CalculationValueFormat.Currency,
                                AggregationType = Common.TemplateMetadata.Enums.AggregationType.Sum,
                                ReferenceData = new List<ReferenceData>
                                {
                                    new ReferenceData
                                    {
                                        AggregationType = Common.TemplateMetadata.Enums.AggregationType.None,
                                        Format = Common.TemplateMetadata.Enums.ReferenceDataValueFormat.Number,
                                        Name = "Reference Data 6",
                                        TemplateReferenceId = 6
                                    },
                                    new ReferenceData
                                    {
                                        AggregationType = Common.TemplateMetadata.Enums.AggregationType.None,
                                        Format = Common.TemplateMetadata.Enums.ReferenceDataValueFormat.Number,
                                        Name = "Reference Data 7",
                                        TemplateReferenceId = 7
                                    }
                                }
                            },
                        }
                    }
                }
            };

            List<CalculationMetadata> specificationCalculations = new List<CalculationMetadata>();
            calculationsRepository
                .GetCalculationsMetatadataBySpecificationId(Arg.Is(specificationId))
                .Returns(specificationCalculations);

            TemplateMapping existingTemplateMapping = new TemplateMapping()
            {
                FundingStreamId = fundingStreamId,
                SpecificationId = specificationId,
                TemplateMappingItems = new List<TemplateMappingItem>()
                {
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.ReferenceData,
                        Name = "Reference Data 6",
                        TemplateId = 6,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.ReferenceData,
                        Name = "Reference Data 2",
                        TemplateId = 2,
                    },
                }
            };

            calculationsRepository
                .GetTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId))
                .Returns(existingTemplateMapping);

            policiesApiClient
                .GetFundingTemplateContents(fundingStreamId, templateVersion)
                .Returns(new ApiResponse<TemplateMetadataContents>(HttpStatusCode.OK, fundingMetadataContents));

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .SetAssignedTemplateVersion(Arg.Is(specificationId), Arg.Is(templateVersion), Arg.Is(fundingStreamId))
                .Returns(HttpStatusCode.OK);

            TemplateMapping savedTemplateMapping = null;

            await calculationsRepository
               .UpdateTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId), Arg.Do<TemplateMapping>(r => savedTemplateMapping = r));

            CalculationService service = CreateCalculationService(
                calculationsRepository: calculationsRepository,
                specificationRepository: specificationsRepository,
                policiesApiClient: policiesApiClient,
                specificationsApiClient: specificationsApiClient);

            // Act
            var result = await service.AssociateTemplateIdWithSpecification(specificationId, templateVersion, fundingStreamId);

            // Assert
            await calculationsRepository
                .Received(1)
                .GetTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId));

            await policiesApiClient
                .Received(1)
                .GetFundingTemplateContents(Arg.Is(fundingStreamId), Arg.Is(templateVersion));

            await calculationsRepository
                .Received(1)
                .UpdateTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId), Arg.Is(savedTemplateMapping));

            savedTemplateMapping
                .Should()
                .NotBeNull();

            TemplateMapping expectedTemplateMapping = new TemplateMapping()
            {
                FundingStreamId = fundingStreamId,
                SpecificationId = specificationId,
                TemplateMappingItems = new List<TemplateMappingItem>()
                {
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 1",
                        TemplateId = 1,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.ReferenceData,
                        Name = "Reference Data 6",
                        TemplateId = 6,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.ReferenceData,
                        Name = "Reference Data 7",
                        TemplateId = 7,
                    },
                }
            };

            savedTemplateMapping
                .Should()
                .BeEquivalentTo(expectedTemplateMapping);
        }

        [TestMethod]
        public async Task AssociateTemplateIdWithSpecification_WhenExistingMappedCalculationCalculationsAreMapppedToCalculationIdsAndNewCalculationIsAdded_ThenExistingMappingIsKept()
        {
            // Arrange
            string specificationId = "spec1";
            string templateVersion = "1.0";
            string fundingStreamId = "PSG";
            string templateId = "2.2";

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();

            ISpecificationRepository specificationsRepository = CreateSpecificationRepository();

            SpecificationSummary specificationSummary = new SpecificationSummary()
            {
                TemplateIds = new Dictionary<string, string> { { fundingStreamId, templateId } },
                FundingStreams = new List<Reference>()
                {
                    new Reference(fundingStreamId, "PE and Sports"),
                },
            };

            specificationsRepository
               .GetSpecificationSummaryById(specificationId)
               .Returns(specificationSummary);

            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();

            TemplateMetadataContents fundingMetadataContents = new TemplateMetadataContents()
            {
                RootFundingLines = new List<FundingLine>()
                {
                    new FundingLine()
                    {
                        Name = "TotalFundingLine",
                        Calculations = new List<TemplateMetadataModels.Calculation>()
                        {
                            new TemplateMetadataModels.Calculation()
                            {
                                Name = "Calculation 1",
                                TemplateCalculationId = 1,
                                Type = Common.TemplateMetadata.Enums.CalculationType.Cash,
                                ValueFormat = Common.TemplateMetadata.Enums.CalculationValueFormat.Currency,
                                AggregationType = Common.TemplateMetadata.Enums.AggregationType.Sum,
                            },
                        }
                    },
                    new FundingLine()
                    {
                        Name = "Second Funding Line",
                        Calculations = new List<TemplateMetadataModels.Calculation>()
                        {
                            new TemplateMetadataModels.Calculation()
                            {
                                Name = "Calculation 2",
                                TemplateCalculationId = 2,
                                Type = Common.TemplateMetadata.Enums.CalculationType.LumpSum,
                                ValueFormat = Common.TemplateMetadata.Enums.CalculationValueFormat.Number,
                                AggregationType = Common.TemplateMetadata.Enums.AggregationType.None,
                            },
                        }
                    },
                     new FundingLine()
                    {
                        Name = "Third Funding Line",
                        Calculations = new List<TemplateMetadataModels.Calculation>()
                        {
                            new TemplateMetadataModels.Calculation()
                            {
                                Name = "Calculation 3",
                                TemplateCalculationId = 30,
                                Type = Common.TemplateMetadata.Enums.CalculationType.Weighting,
                                ValueFormat = Common.TemplateMetadata.Enums.CalculationValueFormat.Number,
                                AggregationType = Common.TemplateMetadata.Enums.AggregationType.None,
                            },
                        }
                    },
                }
            };

            List<CalculationMetadata> specificationCalculations = new List<CalculationMetadata>()
            {
                new CalculationMetadata()
                {
                    CalculationId = "calc1",
                    CalculationType = CalculationType.Template,
                    Name = "Calculation 1",
                    WasTemplateCalculation = false,
                },
                new CalculationMetadata()
                {
                    CalculationId = "calc2",
                    CalculationType = CalculationType.Template,
                    Name = "Calculation 2",
                    WasTemplateCalculation = false,
                },
                new CalculationMetadata()
                {
                    CalculationId = "calc3",
                    CalculationType = CalculationType.Template,
                    Name = "Calculation 3",
                    WasTemplateCalculation = false,
                }
            };

            calculationsRepository
                .GetCalculationsMetatadataBySpecificationId(Arg.Is(specificationId))
                .Returns(specificationCalculations);

            TemplateMapping existingTemplateMapping = new TemplateMapping()
            {
                FundingStreamId = fundingStreamId,
                SpecificationId = specificationId,
                TemplateMappingItems = new List<TemplateMappingItem>()
                {
                    new TemplateMappingItem()
                    {
                        CalculationId = "calc1",
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 1",
                        TemplateId = 1,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = "calc2",
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 2",
                        TemplateId = 2,
                    },
                }
            };

            calculationsRepository
                .GetTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId))
                .Returns(existingTemplateMapping);

            policiesApiClient
                .GetFundingTemplateContents(fundingStreamId, templateVersion)
                .Returns(new ApiResponse<TemplateMetadataContents>(HttpStatusCode.OK, fundingMetadataContents));

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .SetAssignedTemplateVersion(Arg.Is(specificationId), Arg.Is(templateVersion), Arg.Is(fundingStreamId))
                .Returns(HttpStatusCode.OK);

            TemplateMapping savedTemplateMapping = null;

            await calculationsRepository
               .UpdateTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId), Arg.Do<TemplateMapping>(r => savedTemplateMapping = r));

            CalculationService service = CreateCalculationService(
                calculationsRepository: calculationsRepository,
                specificationRepository: specificationsRepository,
                policiesApiClient: policiesApiClient,
                specificationsApiClient: specificationsApiClient);

            // Act
            var result = await service.AssociateTemplateIdWithSpecification(specificationId, templateVersion, fundingStreamId);

            // Assert
            await calculationsRepository
                .Received(1)
                .GetTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId));

            await policiesApiClient
                .Received(1)
                .GetFundingTemplateContents(Arg.Is(fundingStreamId), Arg.Is(templateVersion));

            await calculationsRepository
                .Received(1)
                .UpdateTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId), Arg.Is(savedTemplateMapping));

            savedTemplateMapping
                .Should()
                .NotBeNull();

            TemplateMapping expectedTemplateMapping = new TemplateMapping()
            {
                FundingStreamId = fundingStreamId,
                SpecificationId = specificationId,
                TemplateMappingItems = new List<TemplateMappingItem>()
                {
                    new TemplateMappingItem()
                    {
                        CalculationId = "calc1",
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 1",
                        TemplateId = 1,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = "calc2",
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 2",
                        TemplateId = 2,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 3",
                        TemplateId = 30,
                    },
                }
            };

            savedTemplateMapping
                .Should()
                .BeEquivalentTo(expectedTemplateMapping);
        }

        [TestMethod]
        public async Task AssociateTemplateIdWithSpecification_WhenExistingMappedCalculationCalculationsAreMapppedToCalculationIdsAndCalculationIsRenamed_ThenExistingMappingIsKeptAndNameIsUpdated()
        {
            // Arrange
            string specificationId = "spec1";
            string templateVersion = "1.0";
            string fundingStreamId = "PSG";
            string templateId = "2.2";

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();

            ISpecificationRepository specificationsRepository = CreateSpecificationRepository();

            SpecificationSummary specificationSummary = new SpecificationSummary()
            {
                TemplateIds = new Dictionary<string, string> { { fundingStreamId, templateId } },
                FundingStreams = new List<Reference>()
                {
                    new Reference(fundingStreamId, "PE and Sports"),
                },
            };

            specificationsRepository
               .GetSpecificationSummaryById(specificationId)
               .Returns(specificationSummary);

            IPoliciesApiClient policiesApiClient = CreatePoliciesApiClient();

            TemplateMetadataContents fundingMetadataContents = new TemplateMetadataContents()
            {
                RootFundingLines = new List<FundingLine>()
                {
                    new FundingLine()
                    {
                        Name = "TotalFundingLine",
                        Calculations = new List<TemplateMetadataModels.Calculation>()
                        {
                            new TemplateMetadataModels.Calculation()
                            {
                                Name = "Calculation 1",
                                TemplateCalculationId = 1,
                                Type = Common.TemplateMetadata.Enums.CalculationType.Cash,
                                ValueFormat = Common.TemplateMetadata.Enums.CalculationValueFormat.Currency,
                                AggregationType = Common.TemplateMetadata.Enums.AggregationType.Sum,
                            },
                        }
                    },
                    new FundingLine()
                    {
                        Name = "Second Funding Line",
                        Calculations = new List<TemplateMetadataModels.Calculation>()
                        {
                            new TemplateMetadataModels.Calculation()
                            {
                                Name = "Calculation Renamed",
                                TemplateCalculationId = 2,
                                Type = Common.TemplateMetadata.Enums.CalculationType.LumpSum,
                                ValueFormat = Common.TemplateMetadata.Enums.CalculationValueFormat.Number,
                                AggregationType = Common.TemplateMetadata.Enums.AggregationType.None,
                            },
                        }
                    },
                     new FundingLine()
                    {
                        Name = "Third Funding Line",
                        Calculations = new List<TemplateMetadataModels.Calculation>()
                        {
                            new TemplateMetadataModels.Calculation()
                            {
                                Name = "Calculation 3",
                                TemplateCalculationId = 30,
                                Type = Common.TemplateMetadata.Enums.CalculationType.Weighting,
                                ValueFormat = Common.TemplateMetadata.Enums.CalculationValueFormat.Number,
                                AggregationType = Common.TemplateMetadata.Enums.AggregationType.None,
                            },
                        }
                    },
                }
            };

            List<CalculationMetadata> specificationCalculations = new List<CalculationMetadata>()
            {
                new CalculationMetadata()
                {
                    CalculationId = "calc1",
                    CalculationType = CalculationType.Template,
                    Name = "Calculation 1",
                    WasTemplateCalculation = false,
                },
                new CalculationMetadata()
                {
                    CalculationId = "calc2",
                    CalculationType = CalculationType.Template,
                    Name = "Calculation 2",
                    WasTemplateCalculation = false,
                },
                new CalculationMetadata()
                {
                    CalculationId = "calc3",
                    CalculationType = CalculationType.Template,
                    Name = "Calculation 3",
                    WasTemplateCalculation = false,
                }
            };

            calculationsRepository
                .GetCalculationsMetatadataBySpecificationId(Arg.Is(specificationId))
                .Returns(specificationCalculations);

            TemplateMapping existingTemplateMapping = new TemplateMapping()
            {
                FundingStreamId = fundingStreamId,
                SpecificationId = specificationId,
                TemplateMappingItems = new List<TemplateMappingItem>()
                {
                    new TemplateMappingItem()
                    {
                        CalculationId = "calc1",
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 1",
                        TemplateId = 1,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = "calc2",
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 2",
                        TemplateId = 2,
                    },
                }
            };

            calculationsRepository
                .GetTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId))
                .Returns(existingTemplateMapping);

            policiesApiClient
                .GetFundingTemplateContents(fundingStreamId, templateVersion)
                .Returns(new ApiResponse<TemplateMetadataContents>(HttpStatusCode.OK, fundingMetadataContents));

            ISpecificationsApiClient specificationsApiClient = CreateSpecificationsApiClient();
            specificationsApiClient
                .SetAssignedTemplateVersion(Arg.Is(specificationId), Arg.Is(templateVersion), Arg.Is(fundingStreamId))
                .Returns(HttpStatusCode.OK);

            TemplateMapping savedTemplateMapping = null;

            await calculationsRepository
               .UpdateTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId), Arg.Do<TemplateMapping>(r => savedTemplateMapping = r));

            CalculationService service = CreateCalculationService(
                calculationsRepository: calculationsRepository,
                specificationRepository: specificationsRepository,
                policiesApiClient: policiesApiClient,
                specificationsApiClient: specificationsApiClient);

            // Act
            var result = await service.AssociateTemplateIdWithSpecification(specificationId, templateVersion, fundingStreamId);

            // Assert
            await calculationsRepository
                .Received(1)
                .GetTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId));

            await policiesApiClient
                .Received(1)
                .GetFundingTemplateContents(Arg.Is(fundingStreamId), Arg.Is(templateVersion));

            await calculationsRepository
                .Received(1)
                .UpdateTemplateMapping(Arg.Is(specificationId), Arg.Is(fundingStreamId), Arg.Is(savedTemplateMapping));

            savedTemplateMapping
                .Should()
                .NotBeNull();

            TemplateMapping expectedTemplateMapping = new TemplateMapping()
            {
                FundingStreamId = fundingStreamId,
                SpecificationId = specificationId,
                TemplateMappingItems = new List<TemplateMappingItem>()
                {
                    new TemplateMappingItem()
                    {
                        CalculationId = "calc1",
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 1",
                        TemplateId = 1,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = "calc2",
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation Renamed",
                        TemplateId = 2,
                    },
                    new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = "Calculation 3",
                        TemplateId = 30,
                    },
                }
            };

            savedTemplateMapping
                .Should()
                .BeEquivalentTo(expectedTemplateMapping);
        }

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

            var fundingTemplateApiResponse = new ApiResponse<TemplateMetadataContents>(HttpStatusCode.NotFound, null);

            policiesApiClient
                .GetFundingTemplateContents(Arg.Is(fundingStreamId), Arg.Is(templateId))
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
    }
}
