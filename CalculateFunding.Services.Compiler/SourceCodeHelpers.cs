using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace CalculateFunding.Services.Compiler
{
    public static class SourceCodeHelpers
    {
        public static IEnumerable<string> GetAggregateFunctionParameter(string sourceCode)
        {
            if(string.IsNullOrWhiteSpace(sourceCode))
            {
                return Enumerable.Empty<string>();
            }

            return Regex.Matches(sourceCode, "( Min|Avg|Max|Sum\\()(.*?)(\\))")
             .OfType<Match>()
             .Select(m => m.Groups.Count > 0 ? m.Groups[0].Value.Replace("Sum(", "").Replace("Min(", "").Replace("Max(", "").Replace("Avg(", "").Replace(")", "") : "");
        }
    }
}
