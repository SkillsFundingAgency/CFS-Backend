using System;

namespace CalculateFunding.Generators.Schema10
{
    public static class DecimalExtensions
    {
        public static object DecimalAsObject(this decimal? value)
        {
            if (!value.HasValue)
            {
                return value;
            }

            return value.Value.DecimalAsObject();
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
