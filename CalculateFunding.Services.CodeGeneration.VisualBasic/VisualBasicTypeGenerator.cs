using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CalculateFunding.Models.Datasets.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace CalculateFunding.Services.CodeGeneration.VisualBasic
{
    public abstract class VisualBasicTypeGenerator
    {
        private static readonly IEnumerable<string> exemptValues = new[] { "Nullable(Of Decimal)", "Nullable(Of Integer)" };

        public static string GenerateIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (exemptValues.Contains(value, StringComparer.InvariantCultureIgnoreCase))
            {
                return value;
            }

            string className = value;

            className = className.Replace("<", "LessThan");
            className = className.Replace(">", "GreaterThan");
            className = className.Replace("%", "Percent");
            className = className.Replace("£", "Pound");
            className = className.Replace("=", "Equals");
            className = className.Replace("+", "Plus");

            bool isValid = SyntaxFacts.IsValidIdentifier(className);

            List<string> chars = new List<string>();
            for (int i = 0; i < className.Length; i++)
            {
                chars.Add(className.Substring(i, 1));
            }

            // Convert "my function name" to "My Function Name"
            Regex convertToSentenceCase = new Regex("\\b[a-z]");
            MatchCollection matches = convertToSentenceCase.Matches(className);
            for (int i = 0; i < matches.Count; i++)
            {
                chars[matches[i].Index] = chars[matches[i].Index].ToString().ToUpperInvariant();
            }

            className = string.Join(string.Empty, chars);

            if (!isValid)
            {
                // File name contains invalid chars, remove them
                Regex regex = new Regex(@"[^\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Nl}\p{Mn}\p{Mc}\p{Cf}\p{Pc}\p{Lm}]");
                className = regex.Replace(className, string.Empty);

                // Class name doesn't begin with a letter, insert an underscore
                if (!Char.IsLetter(className, 0))
                {
                    className = className.Insert(0, "_");
                }
            }

            return className.Replace(" ", String.Empty);
        }

        public static TypeSyntax GetType(FieldType type)
        {
            TypeSyntax propertyType;
            switch (type)
            {
                case FieldType.Boolean:
                    propertyType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BooleanKeyword));
                    break;
                case FieldType.Char:
                    propertyType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.CharKeyword));
                    break;
                case FieldType.Integer:
                    propertyType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntegerKeyword));
                    break;
                case FieldType.Float:
                    propertyType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DoubleKeyword));
                    break;
                case FieldType.Decimal:
                    propertyType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DecimalKeyword));
                    break;
                case FieldType.DateTime:
                    propertyType = SyntaxFactory.IdentifierName("DateTime");
                    break;
                case FieldType.NullableOfDecimal:
                    propertyType = SyntaxFactory.IdentifierName("Nullable(Of Decimal)");
                    break;
                case FieldType.NullableOfInteger:
                    propertyType = SyntaxFactory.IdentifierName("Nullable(Of Integer)");
                    break;
                default:
                    propertyType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword));
                    break;
            }

            return propertyType;
        }

        protected static SyntaxList<ImportsStatementSyntax> StandardImports()
        {
            return SyntaxFactory.List(new[] {
                SyntaxFactory.ImportsStatement(SyntaxFactory.SingletonSeparatedList<ImportsClauseSyntax>(
                    SyntaxFactory.SimpleImportsClause(SyntaxFactory.ParseName("System")))),
                SyntaxFactory.ImportsStatement(SyntaxFactory.SingletonSeparatedList<ImportsClauseSyntax>(
                    SyntaxFactory.SimpleImportsClause(SyntaxFactory.ParseName("System.Collections.Generic")))),
                SyntaxFactory.ImportsStatement(SyntaxFactory.SingletonSeparatedList<ImportsClauseSyntax>(
                    SyntaxFactory.SimpleImportsClause(SyntaxFactory.ParseName("Microsoft.VisualBasic.CompilerServices"))))
            });
        }

        protected static StatementSyntax ParseSourceCodeToStatementSyntax(string sourceCode)
        {
            return SyntaxFactory.ParseSyntaxTree(sourceCode)
                .GetRoot()
                .DescendantNodes()
                .OfType<StatementSyntax>()
                .FirstOrDefault();     
        }
        
        protected static StatementSyntax ParseSourceCodeToStatementSyntax(StringBuilder sourceCode)
        {
            return ParseSourceCodeToStatementSyntax(sourceCode.ToString());
        }

        public static string QuoteAggregateFunctionCalls(string sourceCode)
        {
            Regex x = new Regex(@"(\b(?<!Math.)Min\b|\b(?<!Math.)Avg\b|\b(?<!Math.)Max\b|\b(?<!Math.)Sum\b)()(.*?\))");

            foreach (Match match in x.Matches(sourceCode))
            {
                if (!match.Value.EndsWith("()"))
                {
                    string strippedText = Regex.Replace(match.Value, @"\s+", string.Empty);

                    string result = strippedText
                        .Replace("Sum(", "Sum(\"")
                        .Replace("Max(", "Max(\"")
                        .Replace("Min(", "Min(\"")
                        .Replace("Avg(", "Avg(\"")
                        .Replace(")", "\")");

                    if (match.Success)
                    {
                        sourceCode = sourceCode.Replace(match.Value, result);
                    }
                }
            }

            return sourceCode;
        }

        protected static StatementSyntax CreateProperty(string name, string type = null, SyntaxList<AttributeListSyntax> attributes = new SyntaxList<AttributeListSyntax>())
        {
            return SyntaxFactory.PropertyStatement(GenerateIdentifier(name))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithAttributeLists(attributes)
                .WithAsClause(
                    SyntaxFactory.SimpleAsClause(SyntaxFactory.IdentifierName(GenerateIdentifier(type ?? name))));
        }
    }
}