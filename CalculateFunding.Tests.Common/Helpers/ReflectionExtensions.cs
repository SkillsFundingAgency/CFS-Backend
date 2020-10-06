using System;
using System.Linq.Expressions;
using System.Reflection;

namespace CalculateFunding.Tests.Common.Helpers
{
    public static class ReflectionExtensions
    {
        public static void SetWithNonePublicSetter<TTarget, TPropertyType>(this TTarget target,
            Expression<Func<TTarget, TPropertyType>> property,
            TPropertyType value)
        {
            PropertyInfo propertyInfo = GetPropertyInfo(property);
            
            propertyInfo.SetValue(target, value);
        }

        private static PropertyInfo GetPropertyInfo<TTarget, TPropertyType>(Expression<Func<TTarget, TPropertyType>> property)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            if (property.Body is UnaryExpression unaryExp)
            {
                if (unaryExp.Operand is MemberExpression memberExp)
                {
                    return (PropertyInfo) memberExp.Member;
                }
            }
            else if (property.Body is MemberExpression memberExp)
            {
                return (PropertyInfo) memberExp.Member;
            }

            throw new ArgumentException($"The expression doesn't indicate a valid property. [ {property} ]");
        }
    }
}