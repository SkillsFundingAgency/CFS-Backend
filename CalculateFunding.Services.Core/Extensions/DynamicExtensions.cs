using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Core.Extensions
{
    public static class DynamicExtensions
    {
        public static bool PropertyExists(dynamic obj, string name)
        {
             return ((IDictionary<string, object>)obj).ContainsKey(name);
        }
    }
}
