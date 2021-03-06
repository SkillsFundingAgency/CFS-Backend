﻿using CalculateFunding.Common.TemplateMetadata.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Publishing.Extensions
{
    public static class PublishedProviderFundingStructureExtensions
    {
        public static string AsFormattedMoney(this decimal money) => money % 1 == 0 ? $"£{money:###,###,###,###,##0}" : $"£{money:###,###,###,###,##0.00}";

        public static string AsFormattedNumber(this decimal number) => $"{number:###,###,###,###,##0.##########}";

        public static string AsFormattedPercentage(this decimal number) => $"{number}%";

        public static string AsFormatCalculationType(this object value,
            CalculationValueFormat calculationValueFormat)
        {
            if (value != null)
            {
                switch (calculationValueFormat)
                {
                    case CalculationValueFormat.Boolean:
                    case CalculationValueFormat.String:
                        return value?.ToString();

                    case CalculationValueFormat.Number:
                    case CalculationValueFormat.Percentage:
                    case CalculationValueFormat.Currency:
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

                        break;
                    default:
                        throw new InvalidOperationException("Unknown calculation value format");
                }
            }

            return "Excluded";
        }
    }
}
