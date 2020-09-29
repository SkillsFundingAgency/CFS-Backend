using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TemplateCalculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;
using TemplateCalculationType = CalculateFunding.Common.TemplateMetadata.Enums.CalculationType;
using PublishingFundingLine = CalculateFunding.Models.Publishing.FundingLine;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using CalculateFunding.Services.Core.Extensions;

namespace CalculateFunding.Services.Publishing.UnitTests.Services
{
    [TestClass]
    public class PublishedProviderFundingStructureServiceTests : ServiceTestsBase
    {
        private ILogger _logger;
        private IPublishedFundingRepository _publishedFundingRepository;
        private ISpecificationService _specificationService;
        private IPoliciesService _policiesService;
        private ICalculationsService _calculationsService;
        private PublishedProviderFundingStructureService _service;

        [TestInitialize]
        public void SetUp()
        {
            _logger = Substitute.For<ILogger>();
            _publishedFundingRepository = Substitute.For<IPublishedFundingRepository>();
            _specificationService = Substitute.For<ISpecificationService>();
            _policiesService = Substitute.For<IPoliciesService>();
            _calculationsService = Substitute.For<ICalculationsService>();

            _service = new PublishedProviderFundingStructureService(
                _logger,
                _publishedFundingRepository,
                _specificationService,
                _policiesService,
                _calculationsService);
        }

        [TestMethod]
        public async Task GetFundingStructure_GivenNoPublishedProviderVersion_ReturnsNotFound()
        {
            string publishedProviderVersionId = NewRandomString();
            _publishedFundingRepository.GetPublishedProviderVersionById(publishedProviderVersionId)
                .Returns((PublishedProviderVersion)null);

            var result = await _service.GetPublishedProviderFundingStructure(publishedProviderVersionId);

            NotFoundObjectResult notFoundObjectResult = result.Should()
                                            .BeAssignableTo<NotFoundObjectResult>()
                                            .Which
                                            .As<NotFoundObjectResult>();

            notFoundObjectResult.Value
                .Should()
                .Be($"Published provider version not found for the given PublishedProviderVersionId - {publishedProviderVersionId}");
        }

        [TestMethod]
        public async Task GetFundingStructure_GivenNoSpecificationFoundForPublishedProviderVersion_ReturnsNotFound()
        {
            string publishedProviderVersionId = NewRandomString();
            string specificationId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string providerId = NewRandomString();
            string fundingStreamId = NewRandomString();
            int templateVersion = NewRandomNumber();

            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion(_ => _
                .WithFundingStreamId(fundingStreamId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithProviderId(providerId)
                .WithSpecificationId(specificationId));

            _publishedFundingRepository.GetPublishedProviderVersionById(publishedProviderVersionId)
                .Returns(publishedProviderVersion);
            _specificationService.GetSpecificationSummaryById(specificationId)
                .Returns((SpecificationSummary)null);

            var result = await _service.GetPublishedProviderFundingStructure(publishedProviderVersionId);

            NotFoundObjectResult notFoundObjectResult = result.Should()
                                            .BeAssignableTo<NotFoundObjectResult>()
                                            .Which
                                            .As<NotFoundObjectResult>();

            notFoundObjectResult.Value
                .Should()
                .Be($"Specification not found for SpecificationId - {specificationId}");
        }

        [TestMethod]
        public async Task GetFundingStructure_GivenNoTemplateVersionForFundingStream_ReturnsServerError()
        {
            string publishedProviderVersionId = NewRandomString();
            string specificationId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string providerId = NewRandomString();
            string fundingStreamId = NewRandomString();
            int templateVersion = NewRandomNumber();

            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion(_ => _
                .WithFundingStreamId(fundingStreamId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithProviderId(providerId)
                .WithSpecificationId(specificationId));

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithId(specificationId));

            _publishedFundingRepository.GetPublishedProviderVersionById(publishedProviderVersionId)
                .Returns(publishedProviderVersion);
            _specificationService.GetSpecificationSummaryById(specificationId)
                .Returns(specificationSummary);

            var result = await _service.GetPublishedProviderFundingStructure(publishedProviderVersionId);

            InternalServerErrorResult notFoundObjectResult = result.Should()
                                            .BeAssignableTo<InternalServerErrorResult>()
                                            .Which
                                            .As<InternalServerErrorResult>();

            notFoundObjectResult.Value
                .Should()
                .Be($"Specification contains no matching template version for funding stream '{fundingStreamId}'");
        }

        [TestMethod]
        public async Task GetFundingStructure_GivenNoTemplateMetadataContents_ReturnsServerError()
        {
            string publishedProviderVersionId = NewRandomString();
            string specificationId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string providerId = NewRandomString();
            string fundingStreamId = NewRandomString();
            int templateVersion = NewRandomNumber();

            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion(_ => _
                .WithFundingStreamId(fundingStreamId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithProviderId(providerId)
                .WithSpecificationId(specificationId));

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _
                                                        .WithId(specificationId)
                                                        .WithTemplateIds((fundingStreamId, templateVersion.ToString()))); ;

            _publishedFundingRepository.GetPublishedProviderVersionById(publishedProviderVersionId)
                .Returns(publishedProviderVersion);
            _specificationService.GetSpecificationSummaryById(specificationId)
                .Returns(specificationSummary);
            _policiesService.GetTemplateMetadataContents(fundingStreamId, fundingPeriodId, templateVersion.ToString())
               .Returns((TemplateMetadataContents)null);

            var result = await _service.GetPublishedProviderFundingStructure(publishedProviderVersionId);

            InternalServerErrorResult notFoundObjectResult = result.Should()
                                            .BeAssignableTo<InternalServerErrorResult>()
                                            .Which
                                            .As<InternalServerErrorResult>();

            notFoundObjectResult.Value
                .Should()
                .Be($"Unable to locate funding template contents for {fundingStreamId} {fundingPeriodId} {templateVersion}");
        }

        [TestMethod]
        public async Task GetFundingStructures_ReturnsFlatStructureWithCorrectLevelsAndInCorrectOrder()
        {
            string publishedProviderVersionId = NewRandomString();
            string specificationId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string providerId = NewRandomString();
            string fundingStreamId = NewRandomString();
            int templateVersion = NewRandomNumber();

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _
                                                        .WithId(specificationId)
                                                        .WithTemplateIds((fundingStreamId, templateVersion.ToString())));
            uint templateLineId1 = NewRandomUInt();
            uint templateLineId2 = NewRandomUInt();
            uint templateLineId21 = NewRandomUInt();
            uint templateLineId211 = NewRandomUInt();
            uint templateLineId3 = NewRandomUInt();
            uint templateLineId31 = NewRandomUInt();

            int fundingLineValue1 = NewRandomNumber();
            int fundingLineValue2 = NewRandomNumber();
            int fundingLineValue21 = NewRandomNumber();
            int fundingLineValue211 = NewRandomNumber();
            int fundingLineValue3 = NewRandomNumber();
            int fundingLineValue31 = NewRandomNumber();

            string calculationId1 = NewRandomString();
            string calculationId2 = NewRandomString();
            string calculationId3 = NewRandomString();

            int calculationValue1 = NewRandomNumber();
            int calculationValue2 = NewRandomNumber();
            int calculationValue3 = NewRandomNumber();

            TemplateMetadataContents templateMetadataContents =
                NewTemplateMetadataContents(_ => _
                .WithFundingLines(
                NewTemplateFundingLine(_1 => _1.WithName("f1").WithTemplateLineId(templateLineId1)
                    .WithCalculations(new TemplateCalculation() { TemplateCalculationId = templateLineId1, Type = TemplateCalculationType.Cash })),
                NewTemplateFundingLine(_1 => _1.WithName("f2").WithTemplateLineId(templateLineId2)
                    .WithCalculations(new TemplateCalculation() { TemplateCalculationId = templateLineId2, Type = TemplateCalculationType.Number })
                                .WithFundingLines(
                                        NewTemplateFundingLine(_2 => _2.WithName("f21").WithTemplateLineId(templateLineId21)
                                            .WithCalculations(new TemplateCalculation() { TemplateCalculationId = templateLineId21, Type = TemplateCalculationType.Rate })
                                            .WithFundingLines(NewTemplateFundingLine(_3 => _3.WithName("f22").WithTemplateLineId(templateLineId211)))))),
                NewTemplateFundingLine(_1 => _1.WithName("f3").WithTemplateLineId(templateLineId3)
                                .WithCalculations(new TemplateCalculation() { TemplateCalculationId = templateLineId3, Type = TemplateCalculationType.Boolean })
                                .WithFundingLines(
                                    NewTemplateFundingLine(_2 => _2.WithName("f31").WithTemplateLineId(templateLineId31)
                                    .WithCalculations(new TemplateCalculation() { TemplateCalculationId = templateLineId31, Type = TemplateCalculationType.PupilNumber })
                                    )))));

            TemplateMapping templateMapping = NewTemplateMapping(_ => _.WithItems(
                new TemplateMappingItem() { TemplateId = templateLineId1, CalculationId = calculationId1 },
                new TemplateMappingItem() { TemplateId = templateLineId2, CalculationId = calculationId2 },
                new TemplateMappingItem() { TemplateId = templateLineId21, CalculationId = calculationId1 },
                new TemplateMappingItem() { TemplateId = templateLineId211, CalculationId = calculationId3 },
                new TemplateMappingItem() { TemplateId = templateLineId31, CalculationId = calculationId1 },
                new TemplateMappingItem() { TemplateId = templateLineId3, CalculationId = calculationId3 }));

            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion(_ => _
                .WithFundingStreamId(fundingStreamId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithProviderId(providerId)
                .WithSpecificationId(specificationId)
                .WithFundingLines(
                new PublishingFundingLine() { TemplateLineId = templateLineId1, Value = fundingLineValue1 },
                new PublishingFundingLine() { TemplateLineId = templateLineId2, Value = fundingLineValue2 },
                new PublishingFundingLine() { TemplateLineId = templateLineId21, Value = fundingLineValue21 },
                new PublishingFundingLine() { TemplateLineId = templateLineId211, Value = fundingLineValue211 },
                new PublishingFundingLine() { TemplateLineId = templateLineId3, Value = fundingLineValue3 },
                new PublishingFundingLine() { TemplateLineId = templateLineId31, Value = fundingLineValue31 })
                .WithFundingCalculations(
                new FundingCalculation() { TemplateCalculationId = templateLineId1, Value = calculationValue1 },
                new FundingCalculation() { TemplateCalculationId = templateLineId2, Value = calculationValue2 },
                new FundingCalculation() { TemplateCalculationId = templateLineId21, Value = calculationValue1 },
                new FundingCalculation() { TemplateCalculationId = templateLineId211, Value = calculationValue3 },
                new FundingCalculation() { TemplateCalculationId = templateLineId3, Value = calculationValue3 },
                new FundingCalculation() { TemplateCalculationId = templateLineId31, Value = calculationValue1 })
                );

            _publishedFundingRepository.GetPublishedProviderVersionById(publishedProviderVersionId)
                .Returns(publishedProviderVersion);
            _specificationService.GetSpecificationSummaryById(specificationId)
                .Returns(specificationSummary);
            _policiesService.GetTemplateMetadataContents(fundingStreamId, fundingPeriodId, templateVersion.ToString())
                .Returns(templateMetadataContents);
            _calculationsService.GetTemplateMapping(specificationId, fundingStreamId)
                .Returns(templateMapping);

            var result = await _service.GetPublishedProviderFundingStructure(publishedProviderVersionId);

            PublishedProviderFundingStructure fundingStructure = result.Should()
                                                                .BeAssignableTo<OkObjectResult>()
                                                                .Which
                                                                .Value
                                                                .As<PublishedProviderFundingStructure>();

            fundingStructure
                .Items
                .Count()
                .Should()
                .Be(3);

            fundingStructure.Items.First(x => x.Name == "f1")
                .FundingStructureItems.Count.Should().Be(1);
            fundingStructure.Items.First(x => x.Name == "f2")
                .FundingStructureItems.Count.Should().Be(2);
            fundingStructure.Items.First(x => x.Name == "f2")
                .FundingStructureItems.First(x => x.Name == "f21")
                .FundingStructureItems.Count.Should().Be(2);
            fundingStructure.Items.First(x => x.Name == "f2")
                .FundingStructureItems.First(x => x.Name == "f21")
                .FundingStructureItems.First(x => x.Name == "f22")
                .FundingStructureItems.Should().BeNull();
            fundingStructure.Items.First(x => x.Name == "f3")
                .FundingStructureItems.Count.Should().Be(2);
            fundingStructure.Items.First(x => x.Name == "f3")
                .FundingStructureItems.First(x => x.Name == "f31")
                .FundingStructureItems.Count.Should().Be(1);
        }
    }
}
