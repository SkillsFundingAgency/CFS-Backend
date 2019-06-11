using System;
using System.Collections.Generic;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.Core.Options;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Azure.ServiceBus;
using Serilog;

namespace CalculateFunding.Services.CalcEngine
{
    public static class CalculationEngineServiceValidator
    {
        public static void ValidateConstruction(
            IValidator<ICalculatorResiliencePolicies> calculatorResiliencePoliciesValidator,
            EngineSettings engineSettings,
            ICalculatorResiliencePolicies resiliencePolicies,
            ICalculationsRepository calculationsRepository)
        {
            Guard.ArgumentNotNull(engineSettings, nameof(engineSettings));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(calculationsRepository, nameof(calculationsRepository));

            ValidationResult validationResult = calculatorResiliencePoliciesValidator.Validate(resiliencePolicies);
            if (!validationResult.IsValid)
            {
                throw new ArgumentNullException(null, string.Join(",", validationResult.Errors));
            }
        }

        public static void ValidateMessage(ILogger logger, Message message)
        {
            if (!message.UserProperties.ContainsKey("provider-summaries-partition-index"))
            {
                logger.Error("Provider summaries partition index key not found in message properties");

                throw new KeyNotFoundException("Provider summaries partition index key not found in message properties");
            }

            if (!message.UserProperties.ContainsKey("provider-summaries-partition-size"))
            {
                logger.Error("Provider summaries partition size key not found in message properties");

                throw new KeyNotFoundException("Provider summaries partition size key not found in message properties");
            }

            if (!message.UserProperties.ContainsKey("provider-cache-key"))
            {
                logger.Error("Provider cache key not found");

                throw new KeyNotFoundException("Provider cache key not found");
            }

            int partitionSize = int.Parse(message.UserProperties["provider-summaries-partition-size"].ToString());

            if (partitionSize <= 0)
            {
                logger.Error("Partition size is zero or less. {partitionSize}", partitionSize);

                throw new KeyNotFoundException($"Partition size is zero or less. {partitionSize}");
            }
        }
    }
}
