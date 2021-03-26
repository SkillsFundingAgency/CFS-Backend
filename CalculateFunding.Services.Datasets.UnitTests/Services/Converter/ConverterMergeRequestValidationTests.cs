using System;
using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Services.Datasets.Converter;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Datasets.Services
{
    [TestClass]
    public class ConverterMergeRequestValidationTests
    {
        private ValidationResult _result;

        private ConverterMergeRequestValidation _validation;

        [TestInitialize]
        public void SetUp()
        {
            _validation = new ConverterMergeRequestValidation();
        }

        [TestMethod]
        public void MissingProviderVersionIdIsAValidationError()
        {
            WhenTheRequestIsValidated(NewConverterMergeRequest(_ => _.WithoutProviderVersionId()));
            
            ThenTheValidationResultsContainsTheErrors(("ProviderVersionId", "'Provider Version Id' must not be empty."));
        }
        
        [TestMethod]
        public void MissingDatasetIdIsAValidationError()
        {
            WhenTheRequestIsValidated(NewConverterMergeRequest(_ => _.WithoutDatasetId()));
            
            ThenTheValidationResultsContainsTheErrors(("DatasetId", "'Dataset Id' must not be empty."));
        }
        
        [TestMethod]
        public void MissingVersionIsAValidationError()
        {
            WhenTheRequestIsValidated(NewConverterMergeRequest(_ => _.WithoutVersion()));
            
            ThenTheValidationResultsContainsTheErrors(("Version", "'Version' must not be empty."));
        }
        
        [TestMethod]
        public void MissingDatasetRelationshipIdIsAValidationError()
        {
            WhenTheRequestIsValidated(NewConverterMergeRequest(_ => _.WithoutDatasetRelationshipId()));
            
            ThenTheValidationResultsContainsTheErrors(("DatasetRelationshipId", "'Dataset Relationship Id' must not be empty."));
        }

        private void WhenTheRequestIsValidated(ConverterMergeRequest request)
            => _result = _validation.Validate(request);

        private void ThenTheValidationResultsContainsTheErrors(params (string, string)[] errors)
        {
            _result.Errors.Count
                .Should()
                .Be(errors.Length);
            
            foreach ((string name, string message) error in errors)
            {
                _result.Errors
                    .Should()
                    .Contain(_ => _.PropertyName == error.name &&
                                  _.ErrorMessage == error.message, 
                        $"Expected validation errors to contain {error.name}:{error.message}");
            }    
        }

        private ConverterMergeRequest NewConverterMergeRequest(Action<ConverterMergeRequestBuilder> setUp = null)
        {
            ConverterMergeRequestBuilder converterMergeRequestBuilder = new ConverterMergeRequestBuilder();

            setUp?.Invoke(converterMergeRequestBuilder);
            
            return converterMergeRequestBuilder.Build();
        }
    }
}