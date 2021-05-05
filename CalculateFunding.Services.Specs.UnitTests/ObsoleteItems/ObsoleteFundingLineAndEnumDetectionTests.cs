using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Graph;
using CalculateFunding.Common.ApiClient.Graph.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Graph;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type.Interfaces;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Specs.ObsoleteItems;
using CalculateFunding.Tests.Common.Builders;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog.Core;
using FundingLine = CalculateFunding.Common.ApiClient.Graph.Models.FundingLine;
using Specification = CalculateFunding.Common.ApiClient.Graph.Models.Specification;
using Enum = CalculateFunding.Common.ApiClient.Graph.Models.Enum;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type;

namespace CalculateFunding.Services.Specs.UnitTests.ObsoleteItems
{
    [TestClass]
    public class ObsoleteFundingLineAndEnumDetectionTests
    {
        private Mock<IJobManagement> _jobs;
        private Mock<ICalculationsApiClient> _calculations;
        private Mock<IPoliciesApiClient> _policiesApiClient;
        private Mock<IGraphApiClient> _graphApiClient;
        private Mock<IUniqueIdentifierProvider> _uniqueIdentifiers;
        private ITypeIdentifierGenerator _identifierGenerator;

        private ObsoleteFundingLineAndEnumDetection _detection;

        [TestInitialize]
        public void SetUp()
        {
            _jobs = new Mock<IJobManagement>();
            _calculations = new Mock<ICalculationsApiClient>();
            _policiesApiClient = new Mock<IPoliciesApiClient>();
            _graphApiClient = new Mock<IGraphApiClient>();
            _uniqueIdentifiers = new Mock<IUniqueIdentifierProvider>();
            _identifierGenerator = new VisualBasicTypeIdentifierGenerator();

            _detection = new ObsoleteFundingLineAndEnumDetection(_calculations.Object,
                _policiesApiClient.Object,
                _graphApiClient.Object,
                _uniqueIdentifiers.Object,
                new SpecificationsResiliencePolicies
                {
                    CalcsApiClient = Policy.NoOpAsync(),
                    PoliciesApiClient = Policy.NoOpAsync(),
                    GraphApiClient = Policy.NoOpAsync()
                },
                _identifierGenerator,
                _jobs.Object,
                Logger.None);
        }

        [TestMethod]
        public async Task CreateObsoleteItemsForFundingLinesIfTheSuppliedTemplateVersionsDiffersToTheSpecificationVersion()
        {
            string fundingStreamId = NewRandomString();
            string existingTemplateId = NewRandomString();
            string changedTemplateId = NewRandomString();
            string specificationId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            uint fundingLineOne = NewRandomUint();
            uint fundingLineTwo = NewRandomUint();

            string fundingLineNameOne = NewRandomString();
            string fundingLineIdentifierOne = NewRandomString();

            string fundingLineNameTwo = NewRandomString();
            string fundingLineIdentifierTwo = NewRandomString();

            string calculationIdOne = NewRandomString();
            string calculationIdTwo = NewRandomString();
            string calculationIdThree = NewRandomString();
            string[] allowedEnumValues = new[] { "enum 1", "enum 2", "enum 3" };

            uint templateCalculationId = NewRandomUint();
            string templateCalculationName = NewRandomString();

            string enumVariableName = _identifierGenerator.GenerateIdentifier($"{templateCalculationName}Options");
            string enumValue = _identifierGenerator.GenerateIdentifier(allowedEnumValues[0]);
            string enumVariableNameValueCodeReference = $"{enumVariableName}.{enumValue}";
            string enumId = $"{specificationId}-{fundingStreamId}-{enumVariableName}-{enumValue}";

            ObsoleteItem[] existingObsoleteItems = new[] { NewObsoleteItem(_ => _.WithFundingLineId(fundingLineTwo)
                .WithCalculationIds(calculationIdTwo)
                .WithSpecificationId(specificationId)
                .WithCodeReference(_identifierGenerator.GenerateIdentifier(fundingLineNameTwo))
                .WithTemplateCalculationId(templateCalculationId)
                .WithItemType(ObsoleteItemType.FundingLine)
                .WithFundingLineName(fundingLineNameTwo)) };

            ObsoleteItem[] obsoleteItems = new[] { NewObsoleteItem(_ => _.WithFundingLineId(fundingLineTwo)
                .WithCalculationIds(calculationIdTwo)
                .WithSpecificationId(specificationId)
                .WithCodeReference(_identifierGenerator.GenerateIdentifier(fundingLineNameTwo))
                .WithTemplateCalculationId(templateCalculationId)
                .WithItemType(ObsoleteItemType.FundingLine)
                .WithFundingLineName(fundingLineNameTwo)),
            NewObsoleteItem(_ => _.WithCalculationIds(calculationIdOne)
                .WithEnumValueName(allowedEnumValues[0])
                .WithSpecificationId(specificationId)
                .WithCodeReference(enumVariableNameValueCodeReference)
                .WithTemplateCalculationId(templateCalculationId)
                .WithItemType(ObsoleteItemType.EnumValue))};

            GivenMetadataForTemplateVersion(fundingStreamId, fundingPeriodId, existingTemplateId, NewTemplateMetadataDistinctContents(_ =>
                _.WithFundingLines(new[] {
                    new TemplateMetadataFundingLine()
                    {
                        FundingLineCode = fundingLineOne.ToString(),
                        TemplateLineId = fundingLineOne,
                        Name = fundingLineNameOne
                    },
                    new TemplateMetadataFundingLine()
                    {
                        FundingLineCode = fundingLineTwo.ToString(),
                        TemplateLineId = fundingLineTwo,
                        Name = fundingLineNameTwo
                    }
                })
                .WithCalculations(new[] {
                    new TemplateMetadataCalculation()
                    {
                        TemplateCalculationId = templateCalculationId,
                        Name = templateCalculationName,
                        Type = Common.TemplateMetadata.Enums.CalculationType.Enum,
                        AllowedEnumTypeValues = allowedEnumValues
                    }
                })));
            AndTheMetadataForTemplateVersion(fundingStreamId, fundingPeriodId, changedTemplateId, NewTemplateMetadataDistinctContents(_ =>
                _.WithFundingLines(new[] {
                    new TemplateMetadataFundingLine()
                    {
                        FundingLineCode = fundingLineOne.ToString(),
                        TemplateLineId = fundingLineOne
                    }
                }).WithCalculations(new[] {
                    new TemplateMetadataCalculation()
                    {
                        TemplateCalculationId = templateCalculationId,
                        Name = templateCalculationName,
                        Type = Common.TemplateMetadata.Enums.CalculationType.Enum,
                        AllowedEnumTypeValues = allowedEnumValues.Skip(1)
                    }
                })));
            AndGraphEntitiesForSpecification(specificationId, templateCalculationId.ToString(), templateCalculationName, calculationIdOne, calculationIdTwo);
            AndGraphEntitiesForEnum(enumId, templateCalculationId.ToString(), calculationIdOne);
            AndGraphEntitiesForFundingLine(specificationId, fundingLineTwo.ToString(), calculationIdTwo);
            AndTheObsoleteItemsAreRemovedSuccessfully(specificationId, existingObsoleteItems);
            AndTheObsoleteItemIsCreatedSuccessfully(obsoleteItems);

            await WhenTheFundingLineDetectionIsRun(specificationId,
                fundingStreamId,
                fundingPeriodId,
                existingTemplateId,
                changedTemplateId);

            ThenTheObsoleteItemsAreRemoved(existingObsoleteItems);
            ThenTheObsoleteItemCreated(obsoleteItems);
        }

        private Message NewMessage(Action<MessageBuilder> setUp = null)
        {
            MessageBuilder messageBuilder = new MessageBuilder();

            setUp?.Invoke(messageBuilder);

            return messageBuilder.Build();
        }

        private void AndTheMetadataForTemplateVersion(string fundingStreamId, string fundingPeriodId, string templateVersion, TemplateMetadataDistinctContents templateContents = null)
        {
            GivenMetadataForTemplateVersion(fundingStreamId, fundingPeriodId, templateVersion, templateContents);
        }

        private void GivenMetadataForTemplateVersion(string fundingStreamId, string fundingPeriodId, string templateVersion, TemplateMetadataDistinctContents templateContents = null)
        {
            _policiesApiClient.Setup(x => x.GetDistinctTemplateMetadataContents(fundingStreamId, fundingPeriodId, templateVersion))
                .ReturnsAsync(new ApiResponse<TemplateMetadataDistinctContents>(HttpStatusCode.OK, templateContents));
        }

        private void AndGraphEntitiesForSpecification(string specificationId, string templateCalculationId, string templatCalculationName, params string[] calculationIds)
        {
            IEnumerable<Relationship> calculationRelationships = calculationIds
                .Select(c => new Relationship()
                {
                    Type = SpecificationCalculationRelationships.FromIdField,
                    One = new Models.Graph.Calculation() { CalculationId = c, TemplateCalculationId = templateCalculationId, CalculationName = templatCalculationName }
                });

            _graphApiClient.Setup(x => x.GetAllEntitiesRelatedToSpecification(specificationId))
                .ReturnsAsync(new ApiResponse<IEnumerable<Entity<Specification>>>(HttpStatusCode.OK,
                    new[] { new Entity<Specification>() { Relationships = calculationRelationships } }));
        }

        private void AndGraphEntitiesForEnum(string enumId, string templateCalculationId, params string[] calculationIds)
        {
            IEnumerable<Relationship> calculationRelationships = calculationIds
                .Select(c => new Relationship()
                {
                    Type = CalculationEnumRelationship.ToIdField,
                    One = new Models.Graph.Calculation() { CalculationId = c, TemplateCalculationId = templateCalculationId }
                });

            _graphApiClient.Setup(x => x.GetAllEntitiesRelatedToEnum(enumId))
                .ReturnsAsync(new ApiResponse<IEnumerable<Entity<Enum>>>(HttpStatusCode.OK,
                    new[] { new Entity<Enum>() { Relationships = calculationRelationships } }));
        }

        private void AndGraphEntitiesForFundingLine(string specificationId, string fundingLineCode, params string[] calculationIds)
        {
            IEnumerable<Relationship> calculationRelationships = calculationIds
                .Select(c => new Relationship()
                {
                    Type = FundingLineCalculationRelationship.FromIdField,
                    Two = new Models.Graph.Calculation() { CalculationId = c }
                });

            _graphApiClient.Setup(x => x.GetAllEntitiesRelatedToFundingLine($"{specificationId}-{fundingLineCode}"))
                .ReturnsAsync(new ApiResponse<IEnumerable<Entity<FundingLine>>>(HttpStatusCode.OK,
                    new[] { new Entity<FundingLine>() { Relationships = calculationRelationships } }));
        }

        private void AndTheObsoleteItemsAreRemovedSuccessfully(string specificationId, params ObsoleteItem[] obsoleteItems)
        {
            _calculations.Setup(_ =>
                        _.GetObsoleteItemsForSpecification(specificationId))
                    .ReturnsAsync(new ApiResponse<IEnumerable<ObsoleteItem>>(HttpStatusCode.OK, obsoleteItems.ToList()));

            foreach (ObsoleteItem obsoleteItem in obsoleteItems)
            {
                obsoleteItem.CalculationIds.ForEach(calc => _calculations.Setup(_ =>
                        _.RemoveObsoleteItem(obsoleteItem.Id, calc))
                        .ReturnsAsync(HttpStatusCode.OK));
            }
        }

        private void ThenTheObsoleteItemsAreRemoved(params ObsoleteItem[] obsoleteItems)
        {
            foreach (ObsoleteItem obsoleteItem in obsoleteItems)
            {
                obsoleteItem.CalculationIds.ForEach(calc => _calculations.Verify(_ =>
                        _.RemoveObsoleteItem(obsoleteItem.Id, calc)));
            }
        }

        private void AndTheObsoleteItemIsCreatedSuccessfully(params ObsoleteItem[] obsoleteItems)
        {
            foreach (ObsoleteItem obsoleteItem in obsoleteItems)
            {
                _calculations.Setup(_ =>
                        _.CreateObsoleteItem(It.Is<ObsoleteItem>(_ => _.ItemType == obsoleteItem.ItemType)))
                    .ReturnsAsync(new ApiResponse<ObsoleteItem>(HttpStatusCode.OK, obsoleteItem));
            }
        }

        private void ThenTheObsoleteItemCreated(params ObsoleteItem[] obsoleteItems)
        {
            foreach (ObsoleteItem obsoleteItem in obsoleteItems)
            {
                _calculations.Verify(_ =>
                        _.CreateObsoleteItem(It.Is<ObsoleteItem>(obs =>
                            obs.SpecificationId == obsoleteItem.SpecificationId &&
                            obs.FundingLineId == obsoleteItem.FundingLineId &&
                            obs.TemplateCalculationId == obsoleteItem.TemplateCalculationId &&
                            obs.EnumValueName == obsoleteItem.EnumValueName &&
                            obs.ItemType == obsoleteItem.ItemType &&
                            obs.CodeReference != null &&
                            obs.CodeReference.Equals(obsoleteItem.CodeReference) &&
                            obs.CalculationIds.SequenceEqual(obsoleteItem.CalculationIds) &&
                            (obsoleteItem.FundingLineName == null || obs.FundingLineName.Equals(obsoleteItem.FundingLineName))))
                    , Times.Once);
            }
        }

        private string NewRandomString() => new RandomString();

        private int NewRandomNumber() => new RandomNumberBetween(0, int.MaxValue);
        
        private uint NewRandomUint() => (uint) NewRandomNumber();

        private async Task WhenTheFundingLineDetectionIsRun(string specificationId,
            string fundingStreamId,
            string fundingPeriodId,
            string previousTemplateVersionId,
            string templateVersionId)
            => await _detection.Process(NewMessage(_ => _.WithUserProperty(FundingLineDetectionParameters.SpecificationIdKey, specificationId)
                .WithUserProperty(FundingLineDetectionParameters.FundingPeriodIdKey, fundingPeriodId)
                .WithUserProperty(FundingLineDetectionParameters.FundingStreamIdKey, fundingStreamId)
                .WithUserProperty(FundingLineDetectionParameters.TemplateVersionIdKey, templateVersionId)
                .WithUserProperty(FundingLineDetectionParameters.PreviousTemplateVersionIdKey, previousTemplateVersionId)));

        private TemplateMetadataDistinctContents NewTemplateMetadataDistinctContents(Action<TemplateMetadataDistinctContentsBuilder> setup = null)
        {
            TemplateMetadataDistinctContentsBuilder builder = new TemplateMetadataDistinctContentsBuilder();
            setup?.Invoke(builder);

            return builder.Build();
        }

        private ObsoleteItem NewObsoleteItem(Action<ObsoleteItemBuilder> setup = null)
        {
            ObsoleteItemBuilder builder = new ObsoleteItemBuilder();
            setup?.Invoke(builder);

            return builder.Build();
        }
    }
}