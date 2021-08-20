using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;
using CalculateFunding.Common.Utility;

namespace CalculateFunding.Services.Compiler
{
    public static class SourceCodeHelpers
    {
        public static bool HasReturn(string sourceCode)
        {
            // remove all comments from code
            string sourceCodeCommentsStripped = Regex.Replace(sourceCode, @"(""+\s*(\W |\w).+"")|(('|REM)+\s*(\W|\w).+)", string.Empty, RegexOptions.IgnoreCase);
            
            // make sure there is a return in active code
            return Regex.IsMatch(sourceCodeCommentsStripped, @"\s?return\s", RegexOptions.IgnoreCase);
        }

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
            return sourceCodes?.Any(sourceCode => GetCalculationAggregateFunctionParameters(sourceCode).Any()) == true;
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

        public static IEnumerable<string> GetReferencedReleasedDataCalculations(IEnumerable<string> calculationNames, string sourceCode)
        {
            if (string.IsNullOrWhiteSpace(sourceCode))
            {
                return Enumerable.Empty<string>();
            }

            IList<string> calcNamesFound = new List<string>();

            foreach (string calcName in calculationNames)
            {
                if (sourceCode.ToLower().Contains($" {calcName.ToLower()}"))
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

        public static string CommentOutCode(string sourceCode, string reasonForCommenting = "",  string exceptionMessage = "",  string exceptionType = "System.Exception", string commentSymbol = "'")
        {
            Guard.IsNullOrWhiteSpace(sourceCode, nameof(sourceCode));

            string commentHeader = $"{commentSymbol}System Commented";

            if (sourceCode.StartsWith(commentHeader))
            {
                return sourceCode;
            }

            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine(commentHeader);
            stringBuilder.AppendLine();

            if (!string.IsNullOrWhiteSpace(reasonForCommenting))
            {
                stringBuilder.AppendLine($"{commentSymbol}{reasonForCommenting}");
                stringBuilder.AppendLine();
                stringBuilder.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(exceptionMessage))
            {
                stringBuilder.AppendLine($"Throw New {exceptionType}(\"{exceptionMessage}\")");
                stringBuilder.AppendLine();
                stringBuilder.AppendLine();
            }

            using (StringReader reader = new StringReader(sourceCode))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith($"{commentSymbol}"))
                    {
                        stringBuilder.AppendLine($"{line}");
                    }
                    else
                    {
                        stringBuilder.AppendLine($"{commentSymbol}{line}");
                    }
                }
            }

            return stringBuilder.ToString();
        }

        public static bool CodeContainsFullyQualifiedDatasetFieldIdentifier(string sourceCode, IEnumerable<string> datasetFieldIdentifiers)
        {
            //ensure equal spacing of 1
            sourceCode = Regex.Replace(sourceCode, @"\s+", " ");

            foreach (string datasetFieldIdentifier in datasetFieldIdentifiers)
            {
                if (Regex.IsMatch(sourceCode, $"\\b{datasetFieldIdentifier}\\b", RegexOptions.IgnoreCase | RegexOptions.Multiline))
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<string> GetAggregateFunctionParameter(string sourceCode)
        {
            MatchCollection matchCollection = Regex.Matches(sourceCode, @"(\b((Min|Max|Avg|Sum)[\s]*\()([a-zA-Z0-9_.])+?\))(\b|\s|$)", RegexOptions.IgnoreCase);

            IEnumerable<Match> matches = matchCollection.Where(
                m => m.Value.TrimStart().StartsWith(m.Groups[2].ToString(), StringComparison.InvariantCultureIgnoreCase));

            IEnumerable<string> parameters = matches
             .Select(m => m.Groups.Count > 0 ? m.Groups[0].Value.Trim().Replace(m.Groups[2].ToString(), "", StringComparison.InvariantCultureIgnoreCase).Replace(")", "") : "");

            return parameters;
        }
    }
}

