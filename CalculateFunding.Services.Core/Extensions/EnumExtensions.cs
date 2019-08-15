using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace CalculateFunding.Services.Core.Extensions
{
    public static class EnumExtensions
    {
        public static TTargetEnum AsMatchingEnum<TTargetEnum>(this Enum value)
            where TTargetEnum : struct
        {
            return value.ToString().AsEnum<TTargetEnum>();
        }

        public static TTargetEnum AsEnum<TTargetEnum>(this string enumLiteral)
            where TTargetEnum : struct
        {
            return Enum.Parse<TTargetEnum>(enumLiteral);
        }
        
        public static string GetDescription(this Enum value, bool nameIfNull = true)
        {
            Type genericEnumType = value.GetType();
            MemberInfo[] memberInfo = genericEnumType.GetMember(value.ToString());
            if (!memberInfo.IsNullOrEmpty())
            {
                object[] attributes = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (!attributes.IsNullOrEmpty())
                {
                    return ((DescriptionAttribute)attributes.ElementAt(0)).Description;
                }
            }
            if (nameIfNull)
            {
                return value.ToString();
            }

            return string.Empty;
        }

        public static T GetEnumValueFromDescription<T>(this string description)
        {
            Type type = typeof(T);
            if (!type.IsEnum)
            {
                throw new InvalidOperationException();
            }

            foreach (FieldInfo field in type.GetFields())
            {
                if ((DescriptionAttribute)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
                {
                    if (attribute.Description == description)
                        return (T)field.GetValue(null);
                }
                else
                {
                    if (field.Name == description)
                        return (T)field.GetValue(null);
                }
            }

            throw new ArgumentException($"Enum not found for description {description}.");
        }
    }
}
