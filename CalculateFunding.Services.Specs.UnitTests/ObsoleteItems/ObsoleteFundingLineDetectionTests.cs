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

namespace CalculateFunding.Services.Specs.UnitTests.ObsoleteItems
{
    [TestClass]
    public class ObsoleteFundingLineDetectionTests
    {
        private Mock<IJobManagement> _jobs;
        private Mock<ICalculationsApiClient> _calculations;
        private Mock<IPoliciesApiClient> _policiesApiClient;
        private Mock<IGraphApiClient> _graphApiClient;
        private Mock<IUniqueIdentifierProvider> _uniqueIdentifiers;
        private Mock<ITypeIdentifierGenerator> _identifierGenerator;

        private ObsoleteFundingLineDetection _detection;
        
        [TestInitialize]
        public void SetUp()
        {
            _jobs = new Mock<IJobManagement>();
            _calculations = new Mock<ICalculationsApiClient>();
            _policiesApiClient = new Mock<IPoliciesApiClient>();
            _graphApiClient = new Mock<IGraphApiClient>();
            _uniqueIdentifiers = new Mock<IUniqueIdentifierProvider>();
            _identifierGenerator = new Mock<ITypeIdentifierGenerator>();

            _detection = new ObsoleteFundingLineDetection(_calculations.Object,
                _policiesApiClient.Object,
                _graphApiClient.Object,
                _uniqueIdentifiers.Object,
                new SpecificationsResiliencePolicies
                {
                    CalcsApiClient = Policy.NoOpAsync(),
                    PoliciesApiClient = Policy.NoOpAsync(),
                    GraphApiClient = Policy.NoOpAsync()
                },
                _identifierGenerator.Object,
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

            string templateCalculationId = NewRandomNumber().ToString();

            ObsoleteItem obsoleteItem = NewObsoleteItem(_ => _.WithFundingLineId(fundingLineTwo)
                .WithCalculationIds(calculationIdTwo)
                .WithSpecificationId(specificationId)
                .WithCodeReference(fundingLineIdentifierTwo)
                .WithItemType(ObsoleteItemType.FundingLine));

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
                    },
                })));
            AndTheMetadataForTemplateVersion(fundingStreamId, fundingPeriodId, changedTemplateId, NewTemplateMetadataDistinctContents(_ =>
                _.WithFundingLines(new[] {
                    new TemplateMetadataFundingLine()
                    {
                        FundingLineCode = fundingLineOne.ToString(),
                        TemplateLineId = fundingLineOne
                    }
                })));
            AndGraphEntitiesForSpecification(specificationId, templateCalculationId, calculationIdOne, calculationIdTwo);
            AndGraphEntitiesForFundingLine(fundingLineTwo.ToString(), calculationIdTwo);
            AndTheObsoleteItemIsCreatedSuccessfully(obsoleteItem);
            AndTheIdentifierName(fundingLineNameTwo, fundingLineIdentifierTwo);

            await WhenTheFundingLineDetectionIsRun(specificationId,
                fundingStreamId,
                fundingPeriodId,
                existingTemplateId,
                changedTemplateId);

            ThenTheObsoleteItemCreated(obsoleteItem);
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

        private void AndGraphEntitiesForSpecification(string specificationId, string templateCalculationId, params string[] calculationIds)
        {
            IEnumerable<Relationship> calculationRelationships = calculationIds
                .Select(c => new Relationship()
                {
                    Type = SpecificationCalculationRelationships.FromIdField,
                    One = new Models.Graph.Calculation() { CalculationId = c, TemplateCalculationId =  templateCalculationId}
                });

            _graphApiClient.Setup(x => x.GetAllEntitiesRelatedToSpecification(specificationId))
                .ReturnsAsync(new ApiResponse<IEnumerable<Entity<Specification>>>(HttpStatusCode.OK,
                    new[] { new Entity<Specification>() { Relationships = calculationRelationships } }));
        }

        private void AndGraphEntitiesForFundingLine(string fundingLineCode, params string[] calculationIds)
        {
            IEnumerable<Relationship> calculationRelationships = calculationIds
                .Select(c => new Relationship()
                {
                    Type = FundingLineCalculationRelationship.FromIdField,
                    Two = new Models.Graph.Calculation() { CalculationId = c }
                });

            _graphApiClient.Setup(x => x.GetAllEntitiesRelatedToFundingLine(fundingLineCode))
                .ReturnsAsync(new ApiResponse<IEnumerable<Entity<FundingLine>>>(HttpStatusCode.OK,
                    new[] { new Entity<FundingLine>() { Relationships = calculationRelationships } }));
        }

        private void AndTheIdentifierName(string value,
            string identifier)
            => _identifierGenerator.Setup(_ => _.GenerateIdentifier(value))
                .Returns(identifier);

        private void AndTheObsoleteItemIsCreatedSuccessfully(ObsoleteItem obsoleteItem)
        {
            _calculations.Setup(_ =>
                    _.CreateObsoleteItem(It.IsAny<ObsoleteItem>()))
                .ReturnsAsync(new ApiResponse<ObsoleteItem>(HttpStatusCode.OK, obsoleteItem));
        }

        private void ThenTheObsoleteItemCreated(ObsoleteItem obsoleteItem)
        {
            _calculations.Verify(_ =>
                    _.CreateObsoleteItem(It.Is<ObsoleteItem>(obs =>
                        obs.SpecificationId == obsoleteItem.SpecificationId &&
                        obs.FundingLineId == obsoleteItem.FundingLineId &&
                        obs.ItemType == obsoleteItem.ItemType &&
                        obs.CodeReference != null &&
                        obs.CodeReference.Equals(obsoleteItem.CodeReference) &&
                        obs.CalculationIds.SequenceEqual(obsoleteItem.CalculationIds)))
                , Times.Once);
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