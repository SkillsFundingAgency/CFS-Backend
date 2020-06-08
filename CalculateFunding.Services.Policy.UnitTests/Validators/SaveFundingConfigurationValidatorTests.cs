using System;
using System.Collections.Generic;
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

        [TestInitialize]
        public void SetUp()
        {
            _fundingTemplateService = Substitute.For<IFundingTemplateService>();

            IPolicyRepository policyRepository = Substitute.For<IPolicyRepository>();

            _validator = new SaveFundingConfigurationValidator(policyRepository,
                new PolicyResiliencePolicies
                {
                    PolicyRepository = Polly.Policy.NoOpAsync()
                },
                _fundingTemplateService);

            policyRepository.GetFundingStreamById(Arg.Any<string>())
                .Returns(new FundingStream());
            policyRepository.GetFundingPeriodById(Arg.Any<string>())
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

            WhenTheFundingConfigurationIsValidated();

            ThenTheValidationResultShouldBe(expectedFlag);
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
    }
}