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
        public IDictionary<string, string> Variances => _variances;

        private readonly IDictionary<string, string> _variances;

        public ProviderComparer()
        {
            _variances = new Dictionary<string, string>();
        }

        public bool Equals(Provider x, Provider y)
        {
            PropertyInfo[] props = x.GetType().GetProperties();
            foreach (PropertyInfo prop in props.Where(_ => _.Name != "ProviderVersionId"))
            {
                var propA = prop.GetValue(x);
                var propB = prop.GetValue(y);
                if (propA != null)
                {
                    if (propB != null)
                    {
                        if (!propB.Equals(propA))
                        {
                            _variances.Add(prop.Name, $"{propA} != {propB}");
                            return false;
                        }
                    }
                    else
                    {
                        _variances.Add(prop.Name, "compare to empty");
                        return false;
                    }
                }
                else
                {
                    if (propB != null)
                    {
                        _variances.Add(prop.Name, "compare from empty");
                        return false;
                    }
                }
            }

            return true;
        }

        public int GetHashCode(Provider obj)
        {
            throw new NotImplementedException();
        }
    }
}
