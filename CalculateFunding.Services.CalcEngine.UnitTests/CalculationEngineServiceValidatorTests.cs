using System;
using System.Collections.Generic;
using System.Text;
using CalculateFunding.Services.CalcEngine;
using CalculateFunding.Services.CalcEngine.Validators;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Common.Caching;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Options;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Polly.NoOp;
using Serilog;

namespace CalculateFunding.Services.Calculator
{
    [TestClass]
    public class CalculationEngineServiceValidatorTests
    {
        private const string KeyNotFoundInMessagePropertiesErrMessage = "key not found in message properties";
        private const string PartitionIndexKey = "provider-summaries-partition-index";
        private const string PartitionSizeKey = "provider-summaries-partition-size";
        private const string PartitionCacheKeyKey = "provider-cache-key";

        [TestMethod]
        public void ValidateConstruction_WhenValidatorReturnsFalse_ShouldThrowArgumentNullExceptionWithListOfErrors()
        {
            // Arrange
            EngineSettings nullEngineSettings = Substitute.For<EngineSettings>();
            ICalculationsRepository mockCalculationRepository = Substitute.For<ICalculationsRepository>();
            ICalculatorResiliencePolicies mockCalculatorResiliencePolicies = Substitute.For<ICalculatorResiliencePolicies>();
            var mockMessengerPolicy = Policy.NoOpAsync();
            var mockProviderResultsRepositoryPolicy = Policy.NoOpAsync();
            mockCalculatorResiliencePolicies.CacheProvider.Returns((Policy)null);
            mockCalculatorResiliencePolicies.Messenger.Returns(mockMessengerPolicy);
            mockCalculatorResiliencePolicies.ProviderSourceDatasetsRepository.Returns((Policy)null);
            mockCalculatorResiliencePolicies.ProviderResultsRepository.Returns(mockProviderResultsRepositoryPolicy);
            mockCalculatorResiliencePolicies.CalculationsRepository.Returns((Policy)null);
            IValidator<ICalculatorResiliencePolicies> validator = new CalculatorResiliencePoliciesValidator();

            // Act
            Action validateAction = () =>
            {
                CalculationEngineServiceValidator.ValidateConstruction(validator, nullEngineSettings, mockCalculatorResiliencePolicies, mockCalculationRepository);
            };

            // Assert
            validateAction
                .Should()
                .ThrowExactly<ArgumentNullException>()
                .And.Message
                .Should().Contain("CacheProvider")
                .And.Contain("ProviderSourceDatasetsRepository")
                .And.Contain("CalculationRepository");
        }

        [TestMethod]
        public void ValidateConstruction_WhenEngineSettingsIsNull_ShouldThrowException()
        {
            // Arrange
            EngineSettings nullEngineSettings = null;
            ICalculationsRepository mockCalculationRepository = Substitute.For<ICalculationsRepository>();
            ICalculatorResiliencePolicies mockCalculatorResiliencePolicies = Substitute.For<ICalculatorResiliencePolicies>();
            IValidator<ICalculatorResiliencePolicies> validator = Substitute.For<IValidator<ICalculatorResiliencePolicies>>();
            validator.Validate(mockCalculatorResiliencePolicies).Returns(new ValidationResult());

            // Act
            Action validateAction = () =>
            {
                CalculationEngineServiceValidator.ValidateConstruction(validator, nullEngineSettings, mockCalculatorResiliencePolicies, mockCalculationRepository);
            };

            // Assert
            validateAction
                .Should()
                .ThrowExactly<ArgumentNullException>()
                .And.Message
                .Should()
                .Contain("Parameter name: engineSettings");
        }

        [TestMethod]
        public void ValidateConstruction_WhenEverythingIsSetupCorrectly_ShouldReturnWithoutThrowingException()
        {
            // Arrange
            EngineSettings mockEngineSettings = Substitute.For<EngineSettings>();
            ICalculationsRepository mockCalculationRepository = Substitute.For<ICalculationsRepository>();
            ICalculatorResiliencePolicies mockCalculatorResiliencePolicies = Substitute.For<ICalculatorResiliencePolicies>();
            IValidator<ICalculatorResiliencePolicies> validator = Substitute.For<IValidator<ICalculatorResiliencePolicies>>();
            validator.Validate(mockCalculatorResiliencePolicies).Returns(new ValidationResult());

            // Act
            CalculationEngineServiceValidator.ValidateConstruction(validator, mockEngineSettings, mockCalculatorResiliencePolicies, mockCalculationRepository);
        }

        [TestMethod]
        public void ValidateMessage_WhenMessageContainsAllComponents_ShouldNotThrowException()
        {
            // Arrange
            ILogger mockLogger = Substitute.For<ILogger>();

            const string cacheKey = "Cache-key";
            const int partitionIndex = 0;
            const int partitionSize = 100;

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;
            messageUserProperties.Add(PartitionIndexKey, partitionIndex);
            messageUserProperties.Add(PartitionSizeKey, partitionSize);
            messageUserProperties.Add(PartitionCacheKeyKey, cacheKey);

            // Act, Assert
            CalculationEngineServiceValidator.ValidateMessage(mockLogger, message);
        }

        [TestMethod]
        public void ValidateMessage_WhenMessageDoesNotContainPartitionIndex_ShouldThrowException()
        {
            // Arrange
            ILogger mockLogger = Substitute.For<ILogger>();

            const string cacheKey = "Cache-key";
            const int partitionSize = 100;

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;
            messageUserProperties.Add(PartitionSizeKey, partitionSize);
            messageUserProperties.Add(PartitionCacheKeyKey, cacheKey);
            
            // Act
            Action validateMethod = () =>
            {
                CalculationEngineServiceValidator.ValidateMessage(mockLogger, message);
            };

            // Assert
            validateMethod
                .Should()
                .ThrowExactly<KeyNotFoundException>()
                .And.Message
                .Should()
                .BeEquivalentTo(GenerateExpectedErrorMessage(PartitionIndexKey));
        }

        [TestMethod]
        public void ValidateMessage_WhenMessageDoesNotContainPartitionSize_ShouldThrowException()
        {
            // Arrange
            ILogger mockLogger = Substitute.For<ILogger>();

            const string cacheKey = "Cache-key";
            const int partitionIndex = 0;

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;
            messageUserProperties.Add(PartitionIndexKey, partitionIndex);
            messageUserProperties.Add(PartitionCacheKeyKey, cacheKey);

            // Act
            Action validateMethod = () =>
            {
                CalculationEngineServiceValidator.ValidateMessage(mockLogger, message);
            };

            // Assert
            validateMethod
                .Should()
                .ThrowExactly<KeyNotFoundException>()
                .And.Message
                .Should()
                .BeEquivalentTo(GenerateExpectedErrorMessage(PartitionSizeKey));
        }

        [TestMethod]
        public void ValidateMessage_WhenMessageDoesNotContainProviderCacheKey_ShouldThrowException()
        {
            // Arrange
            ILogger mockLogger = Substitute.For<ILogger>();

            const int partitionSize = 100;
            const int partitionIndex = 0;

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;
            messageUserProperties.Add(PartitionIndexKey, partitionIndex);
            messageUserProperties.Add(PartitionSizeKey, partitionSize);

            // Act
            Action validateMethod = () =>
            {
                CalculationEngineServiceValidator.ValidateMessage(mockLogger, message);
            };

            // Assert
            validateMethod
                .Should()
                .ThrowExactly<KeyNotFoundException>()
                .And.Message
                .Should()
                .BeEquivalentTo("Provider cache key not found");
        }

        [TestMethod]
        public void ValidateMessage_WhenPartitionSizeIsZero_ShouldThrowException()
        {
            // Arrange
            ILogger mockLogger = Substitute.For<ILogger>();

            const int partitionSize = 0;
            const int partitionIndex = 0;
            const string cacheKey = "Cache-key";

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;
            messageUserProperties.Add(PartitionIndexKey, partitionIndex);
            messageUserProperties.Add(PartitionSizeKey, partitionSize);
            messageUserProperties.Add(PartitionCacheKeyKey, cacheKey);

            // Act
            Action validateMethod = () =>
            {
                CalculationEngineServiceValidator.ValidateMessage(mockLogger, message);
            };

            // Assert
            validateMethod
                .Should()
                .ThrowExactly<KeyNotFoundException>()
                .And.Message
                .Should()
                .BeEquivalentTo(GeneratePartitionSizeErrorMessage(partitionSize));
        }

        [TestMethod]
        public void ValidateMessage_WhenPartitionSizeIsLessThanZero_ShouldThrowException()
        {
            // Arrange
            ILogger mockLogger = Substitute.For<ILogger>();

            const int partitionSize = -1;
            const int partitionIndex = 0;
            const string cacheKey = "Cache-key";

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;
            messageUserProperties.Add(PartitionIndexKey, partitionIndex);
            messageUserProperties.Add(PartitionSizeKey, partitionSize);
            messageUserProperties.Add(PartitionCacheKeyKey, cacheKey);

            // Act
            Action validateMethod = () =>
            {
                CalculationEngineServiceValidator.ValidateMessage(mockLogger, message);
            };

            // Assert
            validateMethod
                .Should()
                .ThrowExactly<KeyNotFoundException>()
                .And.Message
                .Should()
                .BeEquivalentTo(GeneratePartitionSizeErrorMessage(partitionSize));
        }

        private static string GenerateExpectedErrorMessage(string missingComponentName)
        {
            StringBuilder stringBuilder = new StringBuilder(missingComponentName);
            stringBuilder.Replace('-', ' ');
            stringBuilder[0] = char.ToUpper(stringBuilder[0]);
            

            return $"{stringBuilder} {KeyNotFoundInMessagePropertiesErrMessage}";
        }

        private static string GeneratePartitionSizeErrorMessage(int partitionSize)
        {
            return $"Partition size is zero or less. {partitionSize}";
        }
    }
}
