using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace CalculateFunding.Services.Compiler
{
    public static class SourceCodeHelpers
    {
        public static IEnumerable<string> GetCalculationAggregateFunctionParameters(string sourceCode)
        {
            if (string.IsNullOrWhiteSpace(sourceCode))
            {
                return Enumerable.Empty<string>();
            }

            IEnumerable<string> aggregateParameters = GetAggregateFunctionParameter(sourceCode);

            if (aggregateParameters == null || !aggregateParameters.Any())
            {
                return Enumerable.Empty<string>();
            }

            return aggregateParameters.Where(m => !m.StartsWith("Datasets", StringComparison.InvariantCultureIgnoreCase));
        }

        public static IEnumerable<string> GetDatasetAggregateFunctionParameters(string sourceCode)
        {
            if (string.IsNullOrWhiteSpace(sourceCode))
            {
                return Enumerable.Empty<string>();
            }

            IEnumerable<string> aggregateParameters = GetAggregateFunctionParameter(sourceCode);

            if (aggregateParameters == null || !aggregateParameters.Any())
            {
                return Enumerable.Empty<string>();
            }

            return aggregateParameters.Where(m => m.StartsWith("Datasets", StringComparison.InvariantCultureIgnoreCase));
        }

        public static bool HasCalculationAggregateFunctionParameters(IEnumerable<string> sourceCodes)
        {
            foreach(string sourceCode in sourceCodes)
            {
                if (GetCalculationAggregateFunctionParameters(sourceCode).Any())
                {
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<string> GetReferencedCalculations(IEnumerable<string> calculationNames, string sourceCode)
        {
            if (string.IsNullOrWhiteSpace(sourceCode))
            {
                return Enumerable.Empty<string>();
            }

            IList<string> calcNamesFound = new List<string>();

            foreach(string calcName in calculationNames)
            {
                if (sourceCode.ToLower().Contains($" {calcName.ToLower()}()"))
                {
                    calcNamesFound.Add(calcName);
                }
            }

            return calcNamesFound;
        }

        public static bool IsCalcReferencedInAnAggregate(IDictionary<string, string> functions, string calcNameToCheck)
        {
            foreach (KeyValuePair<string,string> function in functions.Where(m => m.Key != calcNameToCheck))
            {
                IEnumerable<string> aggregateParameters = GetCalculationAggregateFunctionParameters(function.Value);

                if (aggregateParameters.Contains(calcNameToCheck))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool CheckSourceForExistingCalculationAggregates(IDictionary<string, string> functions, string sourceCode, int level = 1)
        {
            IEnumerable<string> aggregateParameters = GetCalculationAggregateFunctionParameters(sourceCode);

            if (!aggregateParameters.Any())
            {
                level += 1;

                foreach (KeyValuePair<string, string> function in functions)
                {
                    if (GetReferencedCalculations(new[] { function.Key }, sourceCode).Any())
                    {
                        IEnumerable<string> nestedAggregateParameters = GetCalculationAggregateFunctionParameters(functions[function.Key]);

                        if (nestedAggregateParameters.Any())
                        {
                            return true;
                        }

                        return CheckSourceForExistingCalculationAggregates(functions, functions[function.Key], level);
                    }
                }

                return false;
            }

            if(level > 1)
            {
                return true;
            }

            level += 1;

            bool containsAggregate = false;

            foreach (string aggregateParemeter in aggregateParameters)
            {
                if(CheckSourceForExistingCalculationAggregates(functions, functions[aggregateParemeter.Replace("\"", "")], level))
                {
                    return true;
                }
                containsAggregate = false;
            }

            return containsAggregate;
        }

        private static IEnumerable<string> GetAggregateFunctionParameter(string sourceCode)
        {
            return Regex.Matches(sourceCode, "( Min|Avg|Max|Sum\\()(.*?)(\\))")
             .OfType<Match>()
             .Select(m => m.Groups.Count > 0 ? m.Groups[0].Value.Trim().Replace("Sum(", "").Replace("Min(", "").Replace("Max(", "").Replace("Avg(", "").Replace(")", "") : "");
        }
    }
}
