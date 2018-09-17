using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CalculateFunding.Models.Datasets.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace CalculateFunding.Services.CodeGeneration.VisualBasic
{
    public abstract class VisualBasicTypeGenerator
    {
        public static string GenerateIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
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

            className = string.Join("", chars);

            if (!isValid)
            {
                // File name contains invalid chars, remove them
                Regex regex = new Regex(@"[^\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Nl}\p{Mn}\p{Mc}\p{Cf}\p{Pc}\p{Lm}]");
                className = regex.Replace(className, "");

                // Class name doesn't begin with a letter, insert an underscore
                if (!Char.IsLetter(className, 0))
                {
                    className = className.Insert(0, "_");
                }
            }

            return className.Replace(" ", String.Empty);
        }

        public static string IdentifierCamelCase(string value)
        {
            var titleCase = GenerateIdentifier(value);
            return Char.ToLowerInvariant(titleCase[0]) + titleCase.Substring(1);
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
                default:
                    propertyType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword));
                    break;
            }

            return propertyType;
        }

        protected static SyntaxList<ImportsStatementSyntax> StandardImports()
        {
            var imports = SyntaxFactory.List(new[] {
                SyntaxFactory.ImportsStatement(SyntaxFactory.SingletonSeparatedList<ImportsClauseSyntax>(SyntaxFactory.SimpleImportsClause(SyntaxFactory.ParseName("System")))),
                SyntaxFactory.ImportsStatement(SyntaxFactory.SingletonSeparatedList<ImportsClauseSyntax>(SyntaxFactory.SimpleImportsClause(SyntaxFactory.ParseName("System.Collections.Generic")))),
                SyntaxFactory.ImportsStatement(SyntaxFactory.SingletonSeparatedList<ImportsClauseSyntax>(SyntaxFactory.SimpleImportsClause(SyntaxFactory.ParseName("Microsoft.VisualBasic.CompilerServices"))))
            });
            var str = imports.ToFullString();
            return imports;
        }
    }
}