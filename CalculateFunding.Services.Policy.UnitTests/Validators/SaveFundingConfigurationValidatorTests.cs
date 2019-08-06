using System;
using System.Collections.Generic;
using CalculateFunding.Models.FundingPolicy;
using CalculateFunding.Models.Policy;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Services.Providers.Validators;
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
                .Returns(new Period());
        }

        [TestMethod]
        [DynamicData(nameof(FlagExamples), DynamicDataSourceType.Method)]
        public void IsInvalidIfDefaultTemplateVersionNotSupplied(bool expectedFlag)
        {
            string defaultTemplateVersion = NewRandomString();
            string fundingStreamId = NewRandomString();
            
            GivenTheFundingConfiguration(_ => _.WithFundingStreamId(fundingStreamId)
                .WithDefaultTemplateVersion(defaultTemplateVersion));
            AndTheTemplateExistsCheck(fundingStreamId, defaultTemplateVersion, expectedFlag);
            
            WhenTheFundingConfigurationIsValidated();
            
            ThenTheValidationResultShouldBe(expectedFlag);
        }

        [TestMethod]
        [DataRow("a template version", true)]
        [DataRow(" ", false)]
        [DataRow(null, false)]
        public void IsInvalidIfDefaultTemplateDoesNotExist(string defaultTemplateVersion,
            bool expectedFlag)
        {
            string fundingStreamId = NewRandomString();
            
            GivenTheFundingConfiguration(_ => _.WithDefaultTemplateVersion(defaultTemplateVersion)
                .WithFundingStreamId(fundingStreamId));
            AndTheTemplateExistsCheck(fundingStreamId, defaultTemplateVersion, true);
            
            WhenTheFundingConfigurationIsValidated();

            ThenTheValidationResultShouldBe(expectedFlag);
        }

        public static IEnumerable<object[]> FlagExamples()
        {
            yield return new object[] {true};
            yield return new object[] {false};
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

        private void AndTheTemplateExistsCheck(string fundingStreamId, string templateVersion,
            bool templateExistsFlag)
        {
            _fundingTemplateService.TemplateExists(fundingStreamId, templateVersion)
                .Returns(templateExistsFlag);
        }
    }
}