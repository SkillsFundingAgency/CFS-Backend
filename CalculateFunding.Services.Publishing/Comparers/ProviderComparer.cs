using CalculateFunding.Models.Publishing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

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
                        _variances.Add(property.Name, $"{propA} != {propB}");
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
                            _variances.Add(property.Name, $"{propA} != {propB}");
                            return false;
                        }
                    }
                    else
                    {
                        _variances.Add(property.Name, "compare to empty");
                        return false;
                    }
                }
                else
                {
                    if (propB != null)
                    {
                        _variances.Add(property.Name, "compare from empty");
                        return false;
                    }
                }
            }

            return true;
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
