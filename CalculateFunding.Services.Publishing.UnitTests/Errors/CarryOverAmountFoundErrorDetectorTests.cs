using CalculateFunding.Services.Publishing.Errors;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;

namespace CalculateFunding.Services.Publishing.UnitTests.Errors
{
    [TestClass]
    public class CarryOverAmountFoundErrorDetectorTests : PublishedProviderErrorDetectorTest
    {
        private CarryOverAmountFoundErrorDetector _errorDetector;

        [TestInitialize]
        public void SetUp()
        {
            _errorDetector = new CarryOverAmountFoundErrorDetector();
        }

        [TestMethod]
        public void RunsForPreVariations()
        {
            _errorDetector.IsPostVariationCheck
                .Should()
                .BeTrue();

            _errorDetector.IsPreVariationCheck
                .Should()
                .BeFalse();

            _errorDetector.IsForAllFundingConfigurations
                .Should()
                .BeFalse();

            _errorDetector.IsAssignProfilePatternCheck
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public async Task DetectsErrorIfEnableCarryForwardIsDisabledAndProviderHasCarryOver()
        {
            string fundingStreamId = NewRandomString();
            string fundingLineCode = NewRandomString();
            decimal carryOverAmount = NewRandomNumber();

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(
                NewPublishedProviderVersion(ppv =>
                    ppv.WithFundingStreamId(fundingStreamId)
                        .WithCarryOvers(NewProfilingCarryOver(co => 
                            co.WithFundingLineCode(fundingLineCode).WithAmount(carryOverAmount))))));

            FundingConfiguration fundingConfiguration = NewFundingConfiguration(_ => _.WithEnableCarryForward(false));

            PublishedProvidersContext publishedProvidersContext = new()
            {
                FundingConfiguration = fundingConfiguration
            };

            await WhenErrorsAreDetectedOnThePublishedProvider(publishedProvider, publishedProvidersContext);

            publishedProvider.Current
                .Errors
                .Should()
                .NotBeNullOrEmpty();

            AndPublishedProviderShouldHaveTheErrors(publishedProvider.Current,
                NewError(_ => _.WithFundingLine(fundingLineCode)
                    .WithIdentifier(fundingLineCode)
                    .WithType(PublishedProviderErrorType.FundingLineValueProfileMismatch)
                    .WithSummaryErrorMessage("A funding line has carry over amount even though Enable Carry Forward option is not enabled")
                    .WithDetailedErrorMessage("Fundling line profile doesn't Enable Carry Forward setting. " +
                        $"Carry over amount is £{carryOverAmount}, but Enable Carry Forward option is set to false")
                    .WithFundingStreamId(fundingStreamId)));
        }

        [TestMethod]
        public async Task DoesNotDetectsErrorIfEnableCarryForwardIsEnabled()
        {
            string fundingStreamId = NewRandomString();
            string fundingLineCode = NewRandomString();
            decimal carryOverAmount = NewRandomNumber();

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(
                NewPublishedProviderVersion(ppv =>
                    ppv.WithFundingStreamId(fundingStreamId)
                        .WithCarryOvers(NewProfilingCarryOver(co =>
                            co.WithFundingLineCode(fundingLineCode).WithAmount(carryOverAmount))))));

            FundingConfiguration fundingConfiguration = NewFundingConfiguration(_ => _.WithEnableCarryForward(true));

            PublishedProvidersContext publishedProvidersContext = new()
            {
                FundingConfiguration = fundingConfiguration
            };

            await WhenErrorsAreDetectedOnThePublishedProvider(publishedProvider, publishedProvidersContext);

            publishedProvider.Current
                .Errors
                .Should()
                .BeNullOrEmpty();
        }

        private async Task WhenErrorsAreDetectedOnThePublishedProvider(PublishedProvider publishedProvider, PublishedProvidersContext publishedProvidersContext)
        {
            await _errorDetector.DetectErrors(publishedProvider, publishedProvidersContext);
        }
    }
}
