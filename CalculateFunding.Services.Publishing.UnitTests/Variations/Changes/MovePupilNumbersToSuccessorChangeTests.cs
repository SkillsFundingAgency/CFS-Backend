using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Variations.Changes;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NSubstitute;
using TemplateFundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;
using TemplateCalculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;
using TemplateCalculationType = CalculateFunding.Common.TemplateMetadata.Enums.CalculationType;
using CalculateFunding.Services.Core.Caching;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    [TestClass]
    public class MovePupilNumbersToSuccessorChangeTests : VariationChangeTestBase
    {
        private Mock<ICacheProvider> _caching;
        private Mock<IPoliciesApiClient> _policies;
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private string _templateVersion;
        private string _cacheKey;

        [TestInitialize]
        public void SetUp()
        {
            _fundingStreamId = NewRandomString();
            _fundingPeriodId = NewRandomString();
            _templateVersion = NewRandomString();

            _cacheKey = $"{CacheKeys.PupilNumberTemplateCalculationIds}{_fundingStreamId}:{_fundingPeriodId}:{_templateVersion}";
            
            PublishedProviderVersion refreshState = VariationContext.RefreshState;
            
            refreshState.FundingStreamId = _fundingStreamId;
            refreshState.TemplateVersion = _templateVersion;
            refreshState.FundingPeriodId = _fundingPeriodId;
            
            _caching = new Mock<ICacheProvider>();
            _policies = new Mock<IPoliciesApiClient>();

            VariationsApplication.CacheProvider
                .Returns(_caching.Object);
            VariationsApplication.PoliciesApiClient
                .Returns(_policies.Object);
            
            Change = new MovePupilNumbersToSuccessorChange(VariationContext);
            
            VariationContext.SuccessorRefreshState = VariationContext.RefreshState.DeepCopy();
        }

        [TestMethod]
        public void GuardsAgainstMissingVariationsApplication()
        {
            VariationsApplication = null;

            Func<Task> invocation = WhenTheChangeIsApplied;

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("variationsApplications");
        }

        [TestMethod]
        public async Task CachesPupilNumberTemplateCalculationsIfNotCachedForContext()
        {
            uint calculationOneId = NewRandomUint();
            uint calculationTwoId = NewRandomUint();
            uint calculationThreeId = NewRandomUint();

            TemplateMetadataContents templateMapping = NewTemplateMetadataContents(_ =>
                _.WithFundingLines(NewTemplateFundingLine(fl =>
                        fl.WithCalculations(NewTemplateCalculation(cl =>
                                cl.WithTemplateCalculationId(calculationOneId)
                                    .WithType(TemplateCalculationType.PupilNumber)),
                            NewTemplateCalculation(cl =>
                                cl.WithTemplateCalculationId(calculationTwoId)
                                    .WithType(NewCalculationTypeExcept(TemplateCalculationType.PupilNumber))))
                            .WithFundingLines(NewTemplateFundingLine(fl2 =>
                                fl2.WithCalculations(NewTemplateCalculation(cl =>
                                    cl.WithTemplateCalculationId(calculationThreeId)
                                        .WithType(TemplateCalculationType.PupilNumber))))))));
            
            GivenTheTemplateMetadataContents(templateMapping);
            AndTheFundingCalculations(NewFundingCalculation(_ => _.WithTemplateCalculationId(calculationOneId)
                    .WithValue(calculationOneId)),
                NewFundingCalculation(),
                NewFundingCalculation(_ => _.WithTemplateCalculationId(calculationTwoId)
                    .WithValue(calculationThreeId)));
            AndTheSuccessorFundingCalculations(NewFundingCalculation(_ => _.WithTemplateCalculationId(calculationOneId)
                    .WithValue(calculationOneId)),
                NewFundingCalculation(),
                NewFundingCalculation(_ => _.WithTemplateCalculationId(calculationTwoId)
                    .WithValue(calculationThreeId)));
            
            await WhenTheChangeIsApplied();
            
            ThenTheTemplateCalculationIdsWereCached(calculationThreeId, calculationOneId);
        }

        [TestMethod]
        public async Task MovesPredecessorPupilNumbersOntoSuccessor()
        {
            uint calculationOneId = NewRandomUint();
            uint calculationTwoId = NewRandomUint();

            int predecessorPupilNumberOne = NewRandomPupilNumber();
            int predecessorPupilNumberTwo = NewRandomPupilNumber();
            int successorPupilNumberOne = NewRandomPupilNumber();
            int successorPupilNumberTwo = NewRandomPupilNumber();
            int successorPupilNumberThree = NewRandomPupilNumber();
            int successorPupilNumberFour = NewRandomPupilNumber();

            GivenThePupilNumberCalculationIds(calculationOneId, calculationTwoId);
            AndTheFundingCalculations(NewFundingCalculation(_ => _.WithTemplateCalculationId(calculationOneId)
                    .WithValue(predecessorPupilNumberOne)),
                NewFundingCalculation(),
                NewFundingCalculation(_ => _.WithTemplateCalculationId(calculationTwoId)
                    .WithValue(predecessorPupilNumberTwo)));

            FundingCalculation successorCalculationOne = NewFundingCalculation(_ => _.WithTemplateCalculationId(calculationOneId)
                .WithValue(successorPupilNumberOne));
            FundingCalculation successorCalculationTwo = NewFundingCalculation(_ => _.WithValue(successorPupilNumberTwo));
            FundingCalculation successorCalculationThree = NewFundingCalculation(_ => _.WithTemplateCalculationId(calculationTwoId)
                .WithValue(successorPupilNumberThree));
            FundingCalculation successorCalculationFour = NewFundingCalculation(_ => _.WithValue(successorPupilNumberFour));
            
            AndTheSuccessorFundingCalculations(successorCalculationOne,
                successorCalculationTwo,
                successorCalculationThree,
                successorCalculationFour);

            await WhenTheChangeIsApplied();
            
            ThenCalculationValueShouldBe(successorCalculationOne, predecessorPupilNumberOne + successorPupilNumberOne);
            AndCalculationValueShouldBe(successorCalculationTwo, successorPupilNumberTwo);
            AndCalculationValueShouldBe(successorCalculationThree, successorPupilNumberThree + predecessorPupilNumberTwo);
            AndCalculationValueShouldBe(successorCalculationFour, successorPupilNumberFour);
        }

        private void ThenCalculationValueShouldBe(FundingCalculation fundingCalculation, int expectedValue)
        {
            fundingCalculation
                .Value
                .Should()
                .Be(expectedValue);
        }

        private void AndCalculationValueShouldBe(FundingCalculation fundingCalculation, int expectedValue)
        {
            ThenCalculationValueShouldBe(fundingCalculation, expectedValue);
        }
        
        private int NewRandomPupilNumber() => new RandomNumberBetween(1, 300);

        private void ThenTheTemplateCalculationIdsWereCached(params uint[] templateCalculationIds)
        {
            _caching.Verify(_ => _.SetAsync(_cacheKey,
                It.Is<IEnumerable<uint>>(ids => ids.SequenceEqual(templateCalculationIds)), 
                TimeSpan.FromHours(24), 
                false, 
                null),
                Times.Once);
        }

        private void GivenThePupilNumberCalculationIds(params uint[] templateCalculationIds)
        {
            _caching.Setup(_ => _.KeyExists<IEnumerable<uint>>(_cacheKey))
                .ReturnsAsync(true);
            _caching.Setup(_ => _.GetAsync<IEnumerable<uint>>(_cacheKey, null))
                .ReturnsAsync(templateCalculationIds);
        }
        
        private void GivenTheTemplateMetadataContents(TemplateMetadataContents templateMetadataContents)
        {
            _policies.Setup(_ => _.GetFundingTemplateContents(_fundingStreamId, _fundingPeriodId, _templateVersion, null))
                .ReturnsAsync(new ApiResponse<TemplateMetadataContents>(HttpStatusCode.OK, templateMetadataContents));
        }

        private TemplateMetadataContents NewTemplateMetadataContents(Action<TemplateMetadataContentsBuilder> setUp = null)
        {
            TemplateMetadataContentsBuilder templateMetadataContentsBuilder = new TemplateMetadataContentsBuilder();

            setUp?.Invoke(templateMetadataContentsBuilder);
            
            return templateMetadataContentsBuilder.Build();
        }

        private TemplateFundingLine NewTemplateFundingLine(Action<TemplateFundingLineBuilder> setUp = null)
        {
            TemplateFundingLineBuilder fundingLineBuilder = new TemplateFundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);
            
            return fundingLineBuilder.Build();
        }

        private TemplateCalculation NewTemplateCalculation(Action<TemplateCalculationBuilder> setUp = null)
        {
            TemplateCalculationBuilder calculationBuilder = new TemplateCalculationBuilder();

            setUp?.Invoke(calculationBuilder);
            
            return calculationBuilder.Build();
        }
        
        private uint NewRandomUint() => (uint) new RandomNumberBetween(1, int.MaxValue);
        
        private TemplateCalculationType NewCalculationTypeExcept(params TemplateCalculationType[] except) 
            => new RandomEnum<TemplateCalculationType>(except);
    }
}