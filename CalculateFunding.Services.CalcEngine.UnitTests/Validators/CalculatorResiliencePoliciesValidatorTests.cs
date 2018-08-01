using CalculateFunding.Services.CalcEngine.Validators;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;

namespace CalculateFunding.Services.Calculator.Validators
{
    [TestClass]
    public class CalculatorResiliencePoliciesValidatorTests
    {
        [TestMethod]
        public void ValidateConstruction_WhenModelIsValid_ResultShouldBeValid()
        {
            // Arrange
            CalculatorResiliencePolicies model = new CalculatorResiliencePolicies()
            {
                CalculationsRepository = Policy.NoOpAsync(),
                ProviderResultsRepository = Policy.NoOpAsync(),
                ProviderSourceDatasetsRepository = Policy.NoOpAsync(),
                CacheProvider = Policy.NoOpAsync(),
                Messenger = Policy.NoOpAsync()
            };
            CalculatorResiliencePoliciesValidator validator = new CalculatorResiliencePoliciesValidator();

            // Act
            ValidationResult result = validator.Validate(model);

            // Assert
            result
                .IsValid
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public void ValidateConstruction_WhenModelHasNullCalculationRepositoryPolicy_ShouldShouldBeInvalid()
        {
            // Arrange
            CalculatorResiliencePolicies model = new CalculatorResiliencePolicies()
            {
                CalculationsRepository = null,
                ProviderResultsRepository = Policy.NoOpAsync(),
                ProviderSourceDatasetsRepository = Policy.NoOpAsync(),
                CacheProvider = Policy.NoOpAsync(),
                Messenger = Policy.NoOpAsync()
            };
            CalculatorResiliencePoliciesValidator validator = new CalculatorResiliencePoliciesValidator();

            // Act
            ValidationResult result = validator.Validate(model);

            // Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors
                .Count
                .Should().Be(1);

            result
                .Errors[0]
                .ErrorMessage
                .Should().Contain("CalculationRepository");
        }

        [TestMethod]
        public void ValidateConstruction_WhenModelHasNullProviderResultsRepositoryPolicy_ShouldShouldBeInvalid()
        {
            // Arrange
            CalculatorResiliencePolicies model = new CalculatorResiliencePolicies()
            {
                CalculationsRepository = Policy.NoOpAsync(),
                ProviderResultsRepository = null,
                ProviderSourceDatasetsRepository = Policy.NoOpAsync(),
                CacheProvider = Policy.NoOpAsync(),
                Messenger = Policy.NoOpAsync()
            };
            CalculatorResiliencePoliciesValidator validator = new CalculatorResiliencePoliciesValidator();

            // Act
            ValidationResult result = validator.Validate(model);

            // Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors
                .Count
                .Should().Be(1);

            result
                .Errors[0]
                .ErrorMessage
                .Should().Contain("ProviderResultsRepository");
        }

        [TestMethod]
        public void ValidateConstruction_WhenModelHasNullProviderSourceDatasetsRepositoryPolicy_ShouldShouldBeInvalid()
        {
            // Arrange
            CalculatorResiliencePolicies model = new CalculatorResiliencePolicies()
            {
                CalculationsRepository = Policy.NoOpAsync(),
                ProviderResultsRepository = Policy.NoOpAsync(),
                ProviderSourceDatasetsRepository = null,
                CacheProvider = Policy.NoOpAsync(),
                Messenger = Policy.NoOpAsync()
            };
            CalculatorResiliencePoliciesValidator validator = new CalculatorResiliencePoliciesValidator();

            // Act
            ValidationResult result = validator.Validate(model);

            // Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors
                .Count
                .Should().Be(1);

            result
                .Errors[0]
                .ErrorMessage
                .Should().Contain("ProviderSourceDatasetsRepository");
        }

        [TestMethod]
        public void ValidateConstruction_WhenModelHasNullCacheProviderPolicy_ShouldShouldBeInvalid()
        {
            // Arrange
            CalculatorResiliencePolicies model = new CalculatorResiliencePolicies()
            {
                CalculationsRepository = Policy.NoOpAsync(),
                ProviderResultsRepository = Policy.NoOpAsync(),
                ProviderSourceDatasetsRepository = Policy.NoOpAsync(),
                CacheProvider = null,
                Messenger = Policy.NoOpAsync()
            };
            CalculatorResiliencePoliciesValidator validator = new CalculatorResiliencePoliciesValidator();

            // Act
            ValidationResult result = validator.Validate(model);

            // Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors
                .Count
                .Should().Be(1);

            result
                .Errors[0]
                .ErrorMessage
                .Should().Contain("CacheProvider");
        }

        [TestMethod]
        public void ValidateConstruction_WhenModelHasNullMessengerPolicy_ShouldShouldBeInvalid()
        {
            // Arrange
            CalculatorResiliencePolicies model = new CalculatorResiliencePolicies()
            {
                CalculationsRepository = Policy.NoOpAsync(),
                ProviderResultsRepository = Policy.NoOpAsync(),
                ProviderSourceDatasetsRepository = Policy.NoOpAsync(),
                CacheProvider = Policy.NoOpAsync(),
                Messenger = null
            };
            CalculatorResiliencePoliciesValidator validator = new CalculatorResiliencePoliciesValidator();

            // Act
            ValidationResult result = validator.Validate(model);

            // Assert
            result
                .IsValid
                .Should()
                .BeFalse();

            result
                .Errors
                .Count
                .Should().Be(1);

            result
                .Errors[0]
                .ErrorMessage
                .Should().Contain("Messenger");
        }
    }
}
