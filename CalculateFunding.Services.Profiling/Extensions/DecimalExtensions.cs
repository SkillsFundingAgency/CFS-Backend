namespace CalculateFunding.Services.Profiling.Extensions
{
	using System;

	public static class DecimalExtensions
    {
        public static decimal RoundToDecimalPlaces(this decimal toBeRounded, int decimalPlaces)
        {
            return decimal.Round(toBeRounded, decimalPlaces, MidpointRounding.AwayFromZero);
        }
    }
}