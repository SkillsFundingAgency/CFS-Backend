using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace CalculateFunding.Services.Core.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value, bool nameIfNull = true)
        {
            Type genericEnumType = value.GetType();
            MemberInfo[] memberInfo = genericEnumType.GetMember(value.ToString());
            if (!memberInfo.IsNullOrEmpty())
            {
                object[] attributes = memberInfo[0].GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false);
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
                DescriptionAttribute attribute = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;

                if (attribute != null)
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
