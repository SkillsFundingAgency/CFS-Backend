using System;
using CalculateFunding.Models.Result.ViewModels;
using CalculateFunding.Services.Results.Validators;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Results.UnitTests.Validators
{
    [TestClass]
    public class UpdateFundingStructureLastModifiedRequestValidatorTests
    {
        private UpdateFundingStructureLastModifiedRequestValidator _validator;

        [TestInitialize]
        public void SetUp()
        {
            _validator = new UpdateFundingStructureLastModifiedRequestValidator();
        }

        [TestMethod]
        public void GuardsAgainstMissingLastModifiedDate()
        {
            UpdateFundingStructureLastModifiedRequest request = NewOtherwiseValidRequest(_ => _.WithLastModified(DateTimeOffset.MinValue));

            ValidationResult validationResult = WhenTheRequestIsValidated(request);
            
            ThenTheValidationResultsContainsTheErrors(validationResult, ("LastModified", "'Last Modified' must be greater than"));
        }
        
        [TestMethod]
        public void GuardsAgainstMissingSpecificationId()
        {
            UpdateFundingStructureLastModifiedRequest request = NewOtherwiseValidRequest(_ => _.WithSpecificationId(null));

            ValidationResult validationResult = WhenTheRequestIsValidated(request);
            
            ThenTheValidationResultsContainsTheErrors(validationResult, ("SpecificationId", "'Specification Id' must not be empty."));
        }
        
        [TestMethod]
        public void GuardsAgainstMissingFundingStreamId()
        {
            UpdateFundingStructureLastModifiedRequest request = NewOtherwiseValidRequest(_ => _.WithFundingStreamId(null));

            ValidationResult validationResult = WhenTheRequestIsValidated(request);
            
            ThenTheValidationResultsContainsTheErrors(validationResult, ("FundingStreamId", "'Funding Stream Id' must not be empty."));
        }
        
        [TestMethod]
        public void GuardsAgainstMissingFundingPeriodId()
        {
            UpdateFundingStructureLastModifiedRequest request = NewOtherwiseValidRequest(_ => _.WithFundingPeriodId(null));

            ValidationResult validationResult = WhenTheRequestIsValidated(request);
            
            ThenTheValidationResultsContainsTheErrors(validationResult, ("FundingPeriodId", "'Funding Period Id' must not be empty."));
        }
        
        private void ThenTheValidationResultsContainsTheErrors(ValidationResult result, params (string, string)[] errors)
        {
            result.Errors.Count
                .Should()
                .Be(errors.Length);

            foreach ((string, string) error in errors)
            {
                result.Errors
                    .Should()
                    .Contain(_ => _.PropertyName == error.Item1 &&
                                  _.ErrorMessage.StartsWith(error.Item2),
                        $"Expected validation errors to contain {error.Item1}:{error.Item2}");
            }
        }
        
        private ValidationResult WhenTheRequestIsValidated(UpdateFundingStructureLastModifiedRequest request)
            => _validator.Validate(request);
        
        private UpdateFundingStructureLastModifiedRequest NewOtherwiseValidRequest(Action<UpdateFundingStructureLastModifiedRequestBuilder> overrides = null)
        {
            UpdateFundingStructureLastModifiedRequestBuilder requestBuilder = new UpdateFundingStructureLastModifiedRequestBuilder()
                .WithLastModified(NewRandomDateTime())
                .WithSpecificationId(NewRandomString())
                .WithFundingPeriodId(NewRandomString())
                .WithFundingStreamId(NewRandomString());
            
            overrides?.Invoke(requestBuilder);

            return requestBuilder.Build();
        }
        
        private DateTimeOffset NewRandomDateTime() => new RandomDateTime();
        
        private string NewRandomString() => new RandomString();
    }
}