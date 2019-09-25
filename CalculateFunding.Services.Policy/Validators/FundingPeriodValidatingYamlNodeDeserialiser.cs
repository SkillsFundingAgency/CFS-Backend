using System;
using CalculateFunding.Models.Policy;
using FluentValidation.Results;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace CalculateFunding.Services.Providers.Validators
{
    public class FundingPeriodValidatingYamlNodeDeserialiser : INodeDeserializer
    {
        private readonly INodeDeserializer _nodeDeserializer;
        private readonly IFundingPeriodValidator _fundingPeriodValidator;

        public FundingPeriodValidatingYamlNodeDeserialiser(INodeDeserializer nodeDeserializer,
            IFundingPeriodValidator fundingPeriodValidator)
        {
            _nodeDeserializer = nodeDeserializer;
            _fundingPeriodValidator = fundingPeriodValidator;
        }

        public bool Deserialize(IParser reader,
            Type expectedType,
            Func<IParser, Type, object> nestedObjectDeserializer,
            out object value)
        {
            if (!_nodeDeserializer.Deserialize(reader, expectedType, nestedObjectDeserializer, out value)) return false;

            if (value is FundingPeriodsJsonModel) return true;
            
            ValidationResult validationResult = _fundingPeriodValidator.Validate((FundingPeriod) value);

            return validationResult.IsValid;
        }
    }
}