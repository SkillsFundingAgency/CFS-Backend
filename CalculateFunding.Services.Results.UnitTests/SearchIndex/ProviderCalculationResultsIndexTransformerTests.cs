using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.SearchIndex;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.UnitTests.SearchIndex
{
    [TestClass]
    public class ProviderCalculationResultsIndexTransformerTests
    {
        private IFeatureToggle _featureToggle;
        private ProviderCalculationResultsIndexTransformer _transformer;

        [TestInitialize]
        public void Setup()
        {
            _featureToggle = Substitute.For<IFeatureToggle>();
            _transformer = new ProviderCalculationResultsIndexTransformer(_featureToggle);
        }

        [TestMethod]
        public async Task ShouldThrowException_IfConextIsNotProviderCalculationResultsIndexProcessorContextType()
        {
            ProviderResult providerResult = new ProviderResult();
            ISearchIndexProcessorContext context = new DefaultSearchIndexProcessorContext(new Message());

            Func<Task> test = async () => await _transformer.Transform(providerResult, context);

            test
               .Should()
               .ThrowExactly<ArgumentNullException>()
               .Which
               .Message
               .Should()
               .Be("Value cannot be null. (Parameter 'ProviderCalculationResultsIndexProcessorContext')");
        }

        [TestMethod]
        public async Task ShouldCreateIndex_WhenTransform_GivenProviderResultAndConext()
        {
            string specificationName = new RandomString();
            string specificationId = new RandomString();
            string providerId = new RandomString();

            var providerResult = NewProviderResult(_ => _
            .WithSpecificationId(specificationId)
            .WithProviderSummary(
                        NewProviderSummary(
                            ps => ps.WithId(providerId)
                                .WithName("two")
                                .WithEstablishmentNumber("en2")
                                .WithLACode("lacode2")
                                .WithAuthority("laname2")
                                .WithURN("urn2")
                                .WithUKPRN("ukprn2")
                                .WithLocalProviderType("pt2")
                                .WithLocalProviderSubType("pst2")))
                    .WithCalculationResults(
                        NewCalculationResult(cr => cr.WithCalculationType(CalculationType.Template)
                            .WithCalculation(NewReference(rf => rf.WithName("calc1")))
                            .WithValue(123M)),
                        NewCalculationResult(cr => cr.WithCalculationType(CalculationType.Template)
                            .WithCalculation(NewReference(rf => rf.WithName("calc2")))
                            .WithValue(null)))
                    .WithFundingLineResults(
                        NewFundingLineResult(flr => flr
                            .WithFundingLine(NewReference(rf => rf.WithName("fundingLine1")))
                            .WithValue(333M)),
                        NewFundingLineResult(flr => flr
                            .WithFundingLine(NewReference(rf => rf.WithName("fundingLine2")))
                            .WithValue(555M))));

            Message message = new Message();
            message.UserProperties.Add("specification-id", specificationId);
            message.UserProperties.Add("specification-name", specificationName);
            message.Body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new[] { providerId }));

            ISearchIndexProcessorContext context = new ProviderCalculationResultsIndexProcessorContext(message);

            var index = await _transformer.Transform(providerResult, context);

            index.Should().NotBeNull();
            index.SpecificationId.Should().Be(specificationId);
            index.SpecificationName.Should().Be(specificationName);
            index.ProviderId.Should().Be(providerId);
            index.CalculationId.Length.Should().Be(2);
            index.FundingLineId.Length.Should().Be(2);
        }


        private static ProviderResult NewProviderResult(Action<ProviderResultBuilder> setUp = null)
        {
            ProviderResultBuilder providerResultBuilder = new ProviderResultBuilder();

            setUp?.Invoke(providerResultBuilder);

            return providerResultBuilder.Build();
        }

        private static ProviderSummary NewProviderSummary(Action<ProviderSummaryBuilder> setUp = null)
        {
            ProviderSummaryBuilder providerSummaryBuilder = new ProviderSummaryBuilder();

            setUp?.Invoke(providerSummaryBuilder);

            return providerSummaryBuilder.Build();
        }

        private static CalculationResult NewCalculationResult(Action<CalculationResultBuilder> setUp = null)
        {
            CalculationResultBuilder calculationResultBuilder = new CalculationResultBuilder();

            setUp?.Invoke(calculationResultBuilder);

            return calculationResultBuilder.Build();
        }

        private static FundingLineResult NewFundingLineResult(Action<FundingLineResultBuilder> setUp = null)
        {
            FundingLineResultBuilder fundingLineResultBuilder = new FundingLineResultBuilder();

            setUp?.Invoke(fundingLineResultBuilder);

            return fundingLineResultBuilder.Build();
        }

        private static Reference NewReference(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder referenceBuilder = new ReferenceBuilder();

            setUp?.Invoke(referenceBuilder);

            return referenceBuilder.Build();
        }
    }
}
