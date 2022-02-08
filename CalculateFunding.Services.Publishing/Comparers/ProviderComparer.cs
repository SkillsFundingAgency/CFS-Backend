using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CalculateFunding.Services.Publishing.Comparers
{
    public class ProviderComparer : IEqualityComparer<Provider>
    {
        //reflection is pretty expensive so where possible we should not repeat the introspection
        private static readonly PropertyInfo[] ProviderProperties = typeof(Provider)
            .GetProperties()
            .Where(_ => _.Name != "ProviderVersionId")
            .ToArray();

        private static readonly string[] EmptyStringArray = new string[0];
        
        private readonly IDictionary<string, string> _variances = new Dictionary<string, string>();

        public IDictionary<string, string> Variances => _variances;

        public bool Equals(Provider x, Provider y)
        {
            foreach (PropertyInfo property in ProviderProperties)
            {
                object propA = property.GetValue(x);
                object propB = property.GetValue(y);

                if (IsStringCollection(property))
                {
                    if (!CompareStringCollectionsIgnoreOrder((IEnumerable<string>) propA, (IEnumerable<string>) propB))
                    {
                        RecordVariance(property, $"{propA?.AsJson()} != {propB?.AsJson()}", true);
                        return false;
                    }
                    else
                    {
                        continue;
                    }
                }
                
                if (propA != null)
                {
                    if (propB != null)
                    {
                        if (!propB.Equals(propA))
                        {
                            RecordVariance(property, $"{propA} != {propB}");
                            return false;
                        }
                    }
                    else
                    {
                        RecordVariance(property, "compare to empty");
                        return false;
                    }
                }
                else
                {
                    if (propB != null)
                    {
                        RecordVariance(property, "compare from empty");
                        return false;
                    }
                }
            }

            return true;
        }

        private void RecordVariance(PropertyInfo property, string value, bool skipAttributeCheck = false)
        {
            if (skipAttributeCheck || property.GetCustomAttribute(typeof(VariationReasonValueAttribute)) != null)
            {
                _variances.Add(property.Name, value);
            }
        }

        private static bool CompareStringCollectionsIgnoreOrder(IEnumerable<string> collectionA,
            IEnumerable<string> collectionB)
        {
            string[] arrayA = AsSortedNullSafeStringArray(collectionA);
            string[] arrayB = AsSortedNullSafeStringArray(collectionB);

            return arrayA.Length == arrayB.Length &&
                   arrayA.SequenceEqual(arrayB);
        }

        private static string[] AsSortedNullSafeStringArray(IEnumerable<string> collectionA)
        {
            return collectionA?.Select(_ => _.ToLowerInvariant()).OrderBy(_ => _).ToArray() ?? EmptyStringArray;
        }

        private static bool IsStringCollection(PropertyInfo propertyInfo)
            => propertyInfo.PropertyType == typeof(IEnumerable<string>);

        public int GetHashCode(Provider obj)
        {
            //refused bequest (this is a code smell)
            throw new NotImplementedException();
        }
    }
}
