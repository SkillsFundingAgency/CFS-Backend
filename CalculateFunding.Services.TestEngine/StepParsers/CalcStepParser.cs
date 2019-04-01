using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Code;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Services.CodeMetadataGenerator.Interfaces;

namespace CalculateFunding.Services.TestRunner.StepParsers
{
    public abstract class CalcStepParser
    {
        private readonly ICodeMetadataGeneratorService _codeMetadataGeneratorService;
        private IEnumerable<TypeInformation> _typeInformation;

        public CalcStepParser()
        {
        }

        public CalcStepParser(ICodeMetadataGeneratorService codeMetadataGeneratorService)
        {
            Guard.ArgumentNotNull(codeMetadataGeneratorService, nameof(codeMetadataGeneratorService));

            _codeMetadataGeneratorService = codeMetadataGeneratorService;
        }

        protected static IDictionary<ComparisonOperator, string> ComparisonOperators = new Dictionary<ComparisonOperator, string>
        {
            { ComparisonOperator.GreaterThan, "is greater than" },
            { ComparisonOperator.GreaterThanOrEqualTo, "is greater than or equal to" },
            { ComparisonOperator.LessThan, "is less than" },
            { ComparisonOperator.LessThanOrEqualTo, "is less than or equal to" },
            { ComparisonOperator.EqualTo, "is equal to" },
            { ComparisonOperator.NotEqualTo, "is not equal to" },
        };

        protected MethodInformation FindCalculationMethod(byte[] assembly, string calcName)
        {
            EnsureTypeInformation(assembly);

            return _typeInformation.FirstOrDefault(m => m.Type == "Calculations")?.Methods.FirstOrDefault(m => m.FriendlyName != null && m.FriendlyName.Equals(calcName.Replace("'", ""), StringComparison.InvariantCultureIgnoreCase));
        }

        protected PropertyInformation FindCalculationProperty(byte[] assembly, string propertyName, string propertyType)
        {
            EnsureTypeInformation(assembly);

            return _typeInformation.FirstOrDefault(m => m.Type == propertyType)?.Properties.FirstOrDefault(m => m.FriendlyName != null && m.FriendlyName.Equals(propertyName.Replace("'", ""), StringComparison.InvariantCultureIgnoreCase));
        }

        private void EnsureTypeInformation(byte[] assembly)
        {
            if (_typeInformation == null)
            {
                if (_codeMetadataGeneratorService == null)
                {
                    throw new NullReferenceException("Code Metadata Generator Service has not been set");
                }

                _typeInformation = _codeMetadataGeneratorService.GetTypeInformation(assembly);
            }
        }
    }
}
