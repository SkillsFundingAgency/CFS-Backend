using System;

namespace CalculateFunding.Services.Core.Extensions
{
    public static class DecimalExtensions
    {
        public static object DecimalAsObject(this decimal? value)
        {
            return value?.DecimalAsObject();
        }

        public static object DecimalAsObject(this decimal value)
        {
            bool isWholeNumber = value % 1M == 0M;

            return isWholeNumber
                && value <= int.MaxValue
                && value >= int.MinValue ? Convert.ToInt32(value) : (object)value;
        }
    }
}
