using System;
using CalculateFunding.Models.Calcs;

namespace CalculateFunding.Services.Calcs
{
    public static class CalculationValueTypeExtensions
    {
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
