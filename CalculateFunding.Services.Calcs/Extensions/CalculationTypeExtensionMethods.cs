using System;

namespace CalculateFunding.Common.TemplateMetadata.Enums
{
    public static class CalculationTypeExtensionMethods
    {
        public static CalculateFunding.Models.Calcs.CalculationDataType ToCalculationDataType(this CalculationType calculationType)
        {
            switch (calculationType)
            {
                case CalculationType.Boolean:
                    return CalculateFunding.Models.Calcs.CalculationDataType.Boolean;
                case CalculationType.Adjustment:
                case CalculationType.Cash:
                case CalculationType.Drilldown:
                case CalculationType.Information:
                case CalculationType.LumpSum:
                case CalculationType.Number:
                case CalculationType.PerPupilFunding:
                case CalculationType.ProviderLedFunding:
                case CalculationType.PupilNumber:
                case CalculationType.Rate:
                case CalculationType.Scope:
                case CalculationType.Weighting:
                    return CalculateFunding.Models.Calcs.CalculationDataType.Decimal;
                case CalculationType.Enum:
                    return CalculateFunding.Models.Calcs.CalculationDataType.Enum;
                default:
                    throw new InvalidCastException("Unable to resolve template data type to cfs calculation type");
            }
        }
    }
}
