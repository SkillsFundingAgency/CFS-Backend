using CalculateFunding.Models.Calcs;
using System;

namespace CalculateFunding.Services.CalcEngine
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
    }
}
