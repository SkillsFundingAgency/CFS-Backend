using System;
using CalculateFunding.Common.TemplateMetadata.Enums;

namespace CalculateFunding.Services.Policy
{
    public static class FundingStructureExtensionMethods
    {
        public static string AsFormattedMoney(this decimal money) => money % 1 == 0 ? $"£{money:###,###,###,###,##0}" : $"£{money:###,###,###,###,##0.00}";

        public static string AsFormattedNumber(this decimal number) => $"{number:###,###,###,###,##0.##########}";

        public static string AsFormattedPercentage(this decimal number) => $"{number}%";

        public static string AsFormatCalculationType(this object value,
            CalculationValueFormat calculationValueFormat)
        {
            if (decimal.TryParse(value?.ToString(), out decimal decimalValue))
            {
                return calculationValueFormat switch
                {
                    CalculationValueFormat.Number => decimalValue.AsFormattedNumber(),
                    CalculationValueFormat.Percentage => decimalValue.AsFormattedPercentage(),
                    CalculationValueFormat.Currency => decimalValue.AsFormattedMoney(),
                    _ => throw new InvalidOperationException("Unknown calculation type")
                };
            }

            return value?.ToString();
        }
    }
}