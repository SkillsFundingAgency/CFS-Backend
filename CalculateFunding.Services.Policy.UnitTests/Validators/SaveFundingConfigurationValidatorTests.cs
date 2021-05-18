using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Policy.FundingPolicy;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Policy.Validators
{
    [TestClass]
    public class SaveFundingConfigurationValidatorTests
    {
        private FundingConfiguration _fundingConfiguration;
        private ValidationResult _validationResult;

        private IFundingTemplateService _fundingTemplateService;
        private SaveFundingConfigurationValidator _validator;
        private IPolicyRepository _policyRepository;

        [TestInitialize]
        public void SetUp()
        {
            _fundingTemplateService = Substitute.For<IFundingTemplateService>();
            _policyRepository = Substitute.For<IPolicyRepository>();

            _validator = new SaveFundingConfigurationValidator(_policyRepository,
                new PolicyResiliencePolicies
                {
                    PolicyRepository = Polly.Policy.NoOpAsync()
                },
                _fundingTemplateService);

           
            _policyRepository.GetFundingPeriodById(Arg.Any<string>())
                .Returns(new FundingPeriod());
        }

        [TestMethod]
        public void FailsValidationIfNoApprovalModeSupplied()
        {
            string defaultTemplateVersion = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            GivenTheFundingConfiguration(_ => _.WithApprovalMode(ApprovalMode.Undefined)
                .WithFundingStreamId(fundingStreamId)
                .WithDefaultTemplateVersion(defaultTemplateVersion));
            AndTheTemplateExistsCheck(fundingStreamId, fundingPeriodId, defaultTemplateVersion, true);
            AndTheFundingStreamExists(fundingStreamId);

            WhenTheFundingConfigurationIsValidated();
            
            ThenTheValidationResultShouldBe(false);
        }

        [TestMethod]
        [DynamicData(nameof(FlagExamples), DynamicDataSourceType.Method)]
        public void IsInvalidIfDefaultTemplateDoesNotExist(bool expectedFlag)
        {
            string defaultTemplateVersion = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            GivenTheFundingConfiguration(_ => _.WithFundingStreamId(fundingStreamId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithDefaultTemplateVersion(defaultTemplateVersion));
            AndTheTemplateExistsCheck(fundingStreamId, fundingPeriodId, defaultTemplateVersion, expectedFlag);
            AndTheFundingStreamExists(fundingStreamId);

            WhenTheFundingConfigurationIsValidated();

            ThenTheValidationResultShouldBe(expectedFlag);
        }

        [TestMethod]
        public void FailsValidationIfUpdateCoreProviderVersionIsNotManualForNonFDZProvicerSource()
        {
            string defaultTemplateVersion = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            GivenTheFundingConfiguration(_ => _.WithApprovalMode(ApprovalMode.All)
                .WithFundingStreamId(fundingStreamId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithDefaultTemplateVersion(defaultTemplateVersion)
                .WithUpdateCoreProviderVersion(UpdateCoreProviderVersion.ToLatest)
                .WithProviderSource(CalculateFunding.Models.Providers.ProviderSource.CFS));
            AndTheTemplateExistsCheck(fundingStreamId, fundingPeriodId, defaultTemplateVersion, true);
            AndTheFundingStreamExists(fundingStreamId);

            WhenTheFundingConfigurationIsValidated();

            ThenTheValidationResultShouldBe(false);
            AndTheValiationMessageShouldContain("UpdateCoreProviderVersion - ToLastet is not valid for provider source - CFS");
        }

        [TestMethod]
        public void FailValidationIfAllowedFundingSteamNotExists()
        {
            string defaultTemplateVersion = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string allowedPublishedFundingStreamsIdsToReference = NewRandomString();

            GivenTheFundingConfiguration(_ => _.WithApprovalMode(ApprovalMode.All)
                .WithFundingStreamId(fundingStreamId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithDefaultTemplateVersion(defaultTemplateVersion)
                .WithUpdateCoreProviderVersion(UpdateCoreProviderVersion.ToLatest)
                .WithProviderSource(CalculateFunding.Models.Providers.ProviderSource.FDZ)
                .WithAllowedPublishedFundingStreamsIdsToReference(allowedPublishedFundingStreamsIdsToReference));
            AndTheTemplateExistsCheck(fundingStreamId, fundingPeriodId, defaultTemplateVersion, true);
            AndTheFundingStreamExists(fundingStreamId);

            WhenTheFundingConfigurationIsValidated();

            ThenTheValidationResultShouldBe(false);
            AndTheValiationMessageShouldContain($"Funding stream {allowedPublishedFundingStreamsIdsToReference} not found for AllowedPublishedFundingStreamsIdsToReference");
        }

        [TestMethod]
        public void PassValidationIfFundingConfigurationDataIsValid()
        {
            string defaultTemplateVersion = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            GivenTheFundingConfiguration(_ => _.WithApprovalMode(ApprovalMode.All)
                .WithFundingStreamId(fundingStreamId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithDefaultTemplateVersion(defaultTemplateVersion)
                .WithUpdateCoreProviderVersion(UpdateCoreProviderVersion.ToLatest)
                .WithProviderSource(CalculateFunding.Models.Providers.ProviderSource.FDZ));
            AndTheTemplateExistsCheck(fundingStreamId, fundingPeriodId, defaultTemplateVersion, true);
            AndTheFundingStreamExists(fundingStreamId);

            WhenTheFundingConfigurationIsValidated();

            ThenTheValidationResultShouldBe(true);
        }

        public static IEnumerable<object[]> FlagExamples()
        {
            yield return new object[] { true };
            yield return new object[] { false };
        }

        private string NewRandomString() => new RandomString();

        private void ThenTheValidationResultShouldBe(bool expectedIsValidFlag)
        {
            _validationResult
                .IsValid
                .Should()
                .Be(expectedIsValidFlag);
        }

        private void AndTheValiationMessageShouldContain(string message)
        {
            _validationResult
                .Errors
                .SelectMany(x => x.ErrorMessage)
                .Should()
                .Contain(message);
        }

        private void WhenTheFundingConfigurationIsValidated()
        {
            _validationResult = _validator.Validate(_fundingConfiguration);
        }

        private void GivenTheFundingConfiguration(Action<FundingConfigurationBuilder> setUp = null)
        {
            FundingConfigurationBuilder fundingConfigurationBuilder = new FundingConfigurationBuilder();

            setUp?.Invoke(fundingConfigurationBuilder);

            _fundingConfiguration = fundingConfigurationBuilder.Build();
        }

        private void AndTheTemplateExistsCheck(string fundingStreamId, string fundingPeriodId, string templateVersion,
            bool templateExistsFlag)
        {
            _fundingTemplateService.TemplateExists(fundingStreamId, fundingPeriodId, templateVersion)
                .Returns(templateExistsFlag);
        }

        private void AndTheFundingStreamExists(string fundingStreamId)
        {
            _policyRepository.GetFundingStreamById(fundingStreamId)
                .Returns(new FundingStream());
        }
    }
}