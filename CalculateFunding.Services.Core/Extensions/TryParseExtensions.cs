using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace CalculateFunding.Services.Core.Extensions
{
    public static class TryParseExtensions
    {
        public delegate bool ParseDelegate<T>(string s, out T result);
        public delegate bool ParseDelegateWithNumberStyle<T>(string s, System.Globalization.NumberStyles ns, IFormatProvider? provider, out T result);

        private static T? TryParse<T>(this string value, ParseDelegate<T> parse) where T : struct
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            T result;
            if (parse(value, out result))
            {
                return result;
            }

            return null;
        }

        private static T? TryParse<T>(this string value, System.Globalization.NumberStyles numberStyle, ParseDelegateWithNumberStyle<T> parse) where T : struct
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            T result;
            if (parse(value, numberStyle, null,  out result))
            {
                return result;
            }

            return null;
        }

        public static int? TryParseInt32(this string value)
        {
            return TryParse<int>(value, int.TryParse);
        }

        public static Int64? TryParseInt64(this string value)
        {
            return TryParse<Int64>(value, Int64.TryParse);
        }

        public static bool? TryParseBoolean(this string value)
        {
            return TryParse<bool>(value, bool.TryParse);
        }

        public static Double? TryParseDouble(this string value)
        {
            return TryParse<Double>(value, Double.TryParse);
        }

        public static Decimal? TryParseDecimal(this string value)
        {
            return TryParse<Decimal>(value, System.Globalization.NumberStyles.Float, Decimal.TryParse);
        }

        public static DateTime? TryParseDateTime(this string value)
        {
            DateTime? dateTimeFromLong = TryParseDateTimeFromLong(value);
            return dateTimeFromLong ?? TryParse<DateTime>(value, DateTime.TryParse);
        }

        public static DateTime? TryParseDateTimeFromLong(this string value)
        {
            return TryParse(value, (string s, out DateTime result) =>
            {
                result = DateTime.MinValue;
                long? tryParseInt64 = TryParseInt64(value);
                if (tryParseInt64 != null)
                {
                    try
                    {
                        result = new DateTime(tryParseInt64.Value);
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
                return false;
            });
        }

        public static char? TryParseChar(this string value)
        {
            return TryParse<char>(value, char.TryParse);
        }

        public static float? TryParseFloat(this string value)
        {
            return TryParse<float>(value, float.TryParse);
        }

        public static byte? TryParseByte(this string value)
        {
            return TryParse<byte>(value, byte.TryParse);
        }

        public static bool TryParseNullable(this object value, out int? parsed)
        {
            parsed = null;
            try
            {
                if (value == null || string.IsNullOrWhiteSpace(value.ToString()) || value.ToString().ToLower() == "null")
                    return true;

                int parsedValue;
                if (int.TryParse(value.ToString(), out parsedValue))
                {
                    parsed = (int?)parsedValue;
                    return true;
                };

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool TryParseNullable(this object value, out decimal? parsed)
        {
            parsed = null;
            try
            {
                if (value == null || string.IsNullOrWhiteSpace(value.ToString()) || value.ToString().ToLower() == "null")
                    return true;

                if (decimal.TryParse(value.ToString(), System.Globalization.NumberStyles.Float, null, out decimal parsedValue))
                {
                    parsed = parsedValue;
                    return true;
                };

                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool TryParseEnum<T>(this string value, out T parsed)
        {
            Type type = typeof(T);

            parsed = default(T);

            if (!type.IsEnum)
            {
                throw new InvalidOperationException();
            }

            foreach (FieldInfo field in type.GetFields())
            {
                if ((EnumMemberAttribute)Attribute.GetCustomAttribute(field, typeof(EnumMemberAttribute)) is EnumMemberAttribute attribute)
                {
                    if (attribute.Value == value)
                    {
                        parsed = (T)field.GetValue(null);
                        return true;
                    }
                }

                if (field.Name == value)
                {
                    parsed = (T)field.GetValue(null);
                    return true;
                }
            }

            return false;
        }
    }
}
