using CalculateFunding.Models.Calcs;
using System;

namespace CalculateFunding.Services.Calcs
{
    public static class CalculationValueTypeExtensions
    {
        public static CalculationDataType ToCalculationDataType(this CalculationValueType calculationValueType)
        {
            return calculationValueType switch
            {
                CalculationValueType.Number => CalculationDataType.Decimal,
                CalculationValueType.Percentage => CalculationDataType.Decimal,
                CalculationValueType.Currency => CalculationDataType.Decimal,
                CalculationValueType.Boolean => CalculationDataType.Boolean,
                CalculationValueType.String => CalculationDataType.String,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        public static string GetDefaultSourceCode(this CalculationValueType calculationValueType)
        {
            return calculationValueType switch
            {
                CalculationValueType.Number => "Return 0",
                CalculationValueType.Percentage => "Return 0",
                CalculationValueType.Currency => "Return 0",
                CalculationValueType.Boolean => "Return False",
                CalculationValueType.String => "Return Nothing",
                _ => throw new NotImplementedException(),
            };
        }
    }
}
