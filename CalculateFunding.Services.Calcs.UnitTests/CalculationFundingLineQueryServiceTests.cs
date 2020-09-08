using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Services;
using CalculateFunding.Services.Calcs.UnitTests.Services;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Calculation = CalculateFunding.Models.Calcs.Calculation;
using TemplateCalculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;
using TemplateFundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;

namespace CalculateFunding.Services.Calcs.UnitTests
{
    [TestClass]
    public class CalculationFundingLineQueryServiceTests
    {
        private Mock<IPoliciesApiClient> _templates;
        private Mock<ICalculationsRepository> _calculations;
        private Mock<ICacheProvider> _cache;
        private Mock<ISpecificationsApiClient> _specifications;

        private CalculationFundingLineQueryService _queryService;

        [TestInitialize]
        public void SetUp()
        {
            _templates = new Mock<IPoliciesApiClient>();
            _cache = new Mock<ICacheProvider>();
            _calculations = new Mock<ICalculationsRepository>();
            _specifications = new Mock<ISpecificationsApiClient>();

            _queryService = new CalculationFundingLineQueryService(_templates.Object,
                _calculations.Object,
                _cache.Object,
                _specifications.Object,
                new ResiliencePolicies
                {
                    SpecificationsApiClient = Policy.NoOpAsync(),
                    PoliciesApiClient = Policy.NoOpAsync(),
                    CacheProviderPolicy = Policy.NoOpAsync(),
                    CalculationsRepository = Policy.NoOpAsync()
                });
        }

        [TestMethod]
        public void GuardsAgainstMissingCalculationId()
        {
            Func<Task<IActionResult>> invocation = () => WhenTheCalculationFundingLinesAreQueried(null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("calculationId");
        }

        [TestMethod]
        public async Task Returns404IfNoCalculationMatchingTheSuppliedId()
        {
            NotFoundResult result = await WhenTheCalculationFundingLinesAreQueried(NewRandomString()) as NotFoundResult;

            result
                .Should()
                .NotBeNull();
        }

        [TestMethod]
        public async Task ReturnsCacheContentsIfAvailable()
        {
            string calculationId = NewRandomString();
            string specificationId = NewRandomString();

            Calculation calculation = NewCalculation(_ => _.WithId(calculationId)
                .WithSpecificationId(specificationId));
            CalculationFundingLine[] cachedCalculationFundingLines = new CalculationFundingLine[0];

            GivenTheCalculation(calculationId, calculation);
            AndTheCacheContents(GetCacheKey(specificationId, calculationId), cachedCalculationFundingLines);

            OkObjectResult result = await WhenTheCalculationFundingLinesAreQueried(calculationId) as OkObjectResult;

            result?
                .Value
                .Should()
                .BeSameAs(cachedCalculationFundingLines);
        }

        [TestMethod]
        public void GuardsAgainstNoSpecificationForCalculation()
        {
            string calculationId = NewRandomString();
            string specificationId = NewRandomString();

            Calculation calculation = NewCalculation(_ => _.WithId(calculationId)
                .WithSpecificationId(specificationId));
            GivenTheCalculation(calculationId, calculation);

            Func<Task<IActionResult>> invocation = () => WhenTheCalculationFundingLinesAreQueried(calculationId);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("specificationSummary");
        }

        [TestMethod]
        public void GuardsAgainstNoTemplateVersionSetupForTheCalculationFundingStream()
        {
            string calculationId = NewRandomString();
            string specificationId = NewRandomString();

            Calculation calculation = NewCalculation(_ => _.WithId(calculationId)
                .WithSpecificationId(specificationId));
            GivenTheCalculation(calculationId, calculation);
            AndTheSpecificationSummary(specificationId, NewSpecificationSummary());

            Func<Task<IActionResult>> invocation = () => WhenTheCalculationFundingLinesAreQueried(calculationId);

            invocation
                .Should()
                .Throw<ArgumentOutOfRangeException>()
                .Which
                .ParamName
                .Should()
                .Be("fundingStreamId");
        }

        [TestMethod]
        public void GuardsAgainstNoTemplateMetadataForTemplateVersion()
        {
            string calculationId = NewRandomString();
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string templateVersion = NewRandomString();

            Calculation calculation = NewCalculation(_ => _.WithId(calculationId)
                .WithSpecificationId(specificationId)
                .WithFundingStreamId(fundingStreamId));
            GivenTheCalculation(calculationId, calculation);
            AndTheSpecificationSummary(specificationId,
                NewSpecificationSummary(_ => _.WithTemplateVersions((fundingStreamId, templateVersion))));

            Func<Task<IActionResult>> invocation = () => WhenTheCalculationFundingLinesAreQueried(calculationId);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("template");
        }

        [TestMethod]
        public void GuardsAgainstNoTemplateMappingForSpecificationAndFundingStream()
        {
            string calculationId = NewRandomString();
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string templateVersion = NewRandomString();

            Calculation calculation = NewCalculation(_ => _.WithId(calculationId)
                .WithSpecificationId(specificationId)
                .WithFundingStreamId(fundingStreamId));
            GivenTheCalculation(calculationId, calculation);
            AndTheSpecificationSummary(specificationId,
                NewSpecificationSummary(_ => _.WithTemplateVersions((fundingStreamId, templateVersion))
                    .WithFundingPeriodId(fundingPeriodId)));
            AndTheTemplate(fundingStreamId, fundingPeriodId, templateVersion, NewTemplate());

            Func<Task<IActionResult>> invocation = () => WhenTheCalculationFundingLinesAreQueried(calculationId);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("templateMapping");
        }

        [TestMethod]
        public void GuardsAgainstNoTemplateMappingItemForTheCalculation()
        {
            string calculationId = NewRandomString();
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string templateVersion = NewRandomString();

            Calculation calculation = NewCalculation(_ => _.WithId(calculationId)
                .WithSpecificationId(specificationId)
                .WithFundingStreamId(fundingStreamId));
            GivenTheCalculation(calculationId, calculation);
            AndTheSpecificationSummary(specificationId,
                NewSpecificationSummary(_ => _.WithTemplateVersions((fundingStreamId, templateVersion))
                    .WithFundingPeriodId(fundingPeriodId)));
            AndTheTemplate(fundingStreamId, fundingPeriodId, templateVersion, NewTemplate());
            AndTheTemplateMapping(fundingStreamId, specificationId, NewTemplateMapping());

            Func<Task<IActionResult>> invocation = () => WhenTheCalculationFundingLinesAreQueried(calculationId);

            invocation
                .Should()
                .Throw<InvalidOperationException>()
                .Which
                .Message
                .Should()
                .Be($"Did not locate a template mapping item for CalculationId {calculationId}");
        }

        [TestMethod]
        public async Task LocatesRootFundingLinesContainingTheCalculation()
        {
            string calculationId = NewRandomString();
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string templateVersion = NewRandomString();
            uint templateCalculationId = NewRandomNumber();

            uint rootFundingLineOneId = NewRandomNumber();
            string rootFundingLineOneName = NewRandomString();
            uint rootFundingLineTwoId = NewRandomNumber();
            string rootFundingLineTwoName = NewRandomString();

            Calculation calculation = NewCalculation(_ => _.WithId(calculationId)
                .WithSpecificationId(specificationId)
                .WithFundingStreamId(fundingStreamId));
            GivenTheCalculation(calculationId, calculation);
            AndTheSpecificationSummary(specificationId,
                NewSpecificationSummary(_ => _.WithTemplateVersions((fundingStreamId, templateVersion))
                    .WithFundingPeriodId(fundingPeriodId)));
            AndTheTemplateMapping(fundingStreamId,
                specificationId,
                NewTemplateMapping(_ => _.WithItems(
                    NewTemplateMappingItem(ti => ti.WithCalculationId(calculationId)
                        .WithTemplateId(templateCalculationId)),
                    NewTemplateMappingItem(),
                    NewTemplateMappingItem())));
            AndTheTemplate(fundingStreamId,
                fundingPeriodId,
                templateVersion,
                NewTemplate(_ => _.WithFundingLines(
                    NewTemplateFundingLine(fl => fl.WithTemplateId(rootFundingLineOneId)
                        .WithName(rootFundingLineOneName)
                        .WithCalculations(NewTemplateCalculation(cal =>
                            cal.WithTemplateCalculationId(templateCalculationId)))
                        .WithFundingLines(NewTemplateFundingLine())),
                    NewTemplateFundingLine(fl => fl.WithFundingLines(NewTemplateFundingLine(fl1 => fl1.WithCalculations(
                        NewTemplateCalculation(cal => cal.WithCalculations(NewTemplateCalculation())))))),
                    NewTemplateFundingLine(fl => fl.WithTemplateId(rootFundingLineTwoId)
                        .WithName(rootFundingLineTwoName)
                        .WithFundingLines(NewTemplateFundingLine(fl1 => fl1.WithCalculations(
                            NewTemplateCalculation(cal =>
                                cal.WithCalculations(NewTemplateCalculation(cal1 =>
                                    cal1.WithTemplateCalculationId(templateCalculationId)))))))))));

            OkObjectResult result = await WhenTheCalculationFundingLinesAreQueried(calculationId) as OkObjectResult;

            CalculationFundingLine[] expectedCalculationFundingLines = new[]
            {
                new CalculationFundingLine
                {
                    TemplateId = rootFundingLineOneId,
                    Name = rootFundingLineOneName
                },
                new CalculationFundingLine
                {
                    TemplateId = rootFundingLineTwoId,
                    Name = rootFundingLineTwoName
                }
            };
            
            result?
                .Value
                .Should()
                .BeEquivalentTo(expectedCalculationFundingLines);
            
            AndTheCalculationFundingLinesWereCached(GetCacheKey(specificationId, calculationId), expectedCalculationFundingLines);
        }

        private async Task<IActionResult> WhenTheCalculationFundingLinesAreQueried(string calculationId)
            => await _queryService.GetCalculationFundingLines(calculationId);

        private static string GetCacheKey(string specificationId,
            string calculationId)
            => $"{CacheKeys.CalculationFundingLines}{specificationId}:{calculationId}";

        private void GivenTheCalculation(string calculationId,
            Calculation calculation)
            => _calculations.Setup(_ => _.GetCalculationById(calculationId))
                .ReturnsAsync(calculation);

        private void AndTheSpecificationSummary(string specificationId,
            SpecificationSummary specificationSummary)
            => _specifications.Setup(_ => _.GetSpecificationSummaryById(specificationId))
                .ReturnsAsync(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, specificationSummary));

        private void AndTheTemplate(string fundingStreamId,
            string fundingPeriodId,
            string templateVersion,
            TemplateMetadataContents template)
            => _templates.Setup(_ => _.GetFundingTemplateContents(fundingStreamId,
                    fundingPeriodId,
                    templateVersion,
                    null))
                .ReturnsAsync(new ApiResponse<TemplateMetadataContents>(HttpStatusCode.OK, template));

        private void AndTheTemplateMapping(string fundingStreamId,
            string specificationId,
            TemplateMapping templateMapping)
            => _calculations.Setup(_ => _.GetTemplateMapping(specificationId,
                    fundingStreamId))
                .ReturnsAsync(templateMapping);

        private void AndTheCacheContents(string cacheKey,
            CalculationFundingLine[] calculationFundingLines)
        {
            _cache.Setup(_ => _.KeyExists<CalculationFundingLine[]>(cacheKey))
                .ReturnsAsync(true);
            _cache.Setup(_ => _.GetAsync<CalculationFundingLine[]>(cacheKey, null))
                .ReturnsAsync(calculationFundingLines);
        }

        private void AndTheCalculationFundingLinesWereCached(string cacheKey,
            params CalculationFundingLine[] calculationFundingLines)
            => _cache.Verify(_ => _.SetAsync(cacheKey,
                    It.Is<CalculationFundingLine[]>(fundingLines =>
                        fundingLines.Length == calculationFundingLines.Length &&
                        fundingLines.All(fl => calculationFundingLines.Count(cfl =>
                            cfl.TemplateId == fl.TemplateId &&
                            cfl.Name == fl.Name) == 1)),
                    null),
                Times.Once);

        private Calculation NewCalculation(Action<CalculationBuilder> setUp = null)
        {
            CalculationBuilder calculationBuilder = new CalculationBuilder();

            setUp?.Invoke(calculationBuilder);

            return calculationBuilder.Build();
        }

        private SpecificationSummary NewSpecificationSummary(Action<SpecificationSummaryBuilder> setUp = null)
        {
            SpecificationSummaryBuilder specificationSummaryBuilder = new SpecificationSummaryBuilder();

            setUp?.Invoke(specificationSummaryBuilder);

            return specificationSummaryBuilder.Build();
        }

        private TemplateMetadataContents NewTemplate(Action<TemplateMetadataContentsBuilder> setUp = null)
        {
            TemplateMetadataContentsBuilder templateBuilder = new TemplateMetadataContentsBuilder();

            setUp?.Invoke(templateBuilder);

            return templateBuilder.Build();
        }

        private TemplateMapping NewTemplateMapping(Action<TemplateMappingBuilder> setUp = null)
        {
            TemplateMappingBuilder templateMappingBuilder = new TemplateMappingBuilder();

            setUp?.Invoke(templateMappingBuilder);

            return templateMappingBuilder.Build();
        }

        private TemplateMappingItem NewTemplateMappingItem(Action<TemplateMappingItemBuilder> setUp = null)
        {
            TemplateMappingItemBuilder templateMappingItemBuilder = new TemplateMappingItemBuilder();

            setUp?.Invoke(templateMappingItemBuilder);

            return templateMappingItemBuilder.Build();
        }

        private TemplateFundingLine NewTemplateFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);

            return fundingLineBuilder.Build();
        }

        private TemplateCalculation NewTemplateCalculation(Action<TemplateCalculationBuilder> setUp = null)
        {
            TemplateCalculationBuilder calculationBuilder = new TemplateCalculationBuilder();

            setUp?.Invoke(calculationBuilder);

            return calculationBuilder.Build();
        }

        private static string NewRandomString() => new RandomString();

        private static uint NewRandomNumber() => (uint) new RandomNumberBetween(1, int.MaxValue);
    }
}