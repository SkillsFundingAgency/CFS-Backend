using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace CalculateFunding.Services.CodeGeneration.VisualBasic
{
    public abstract class VisualBasicTypeGenerator
    {
        private static readonly IEnumerable<string> exemptValues = new[] { "Nullable(Of Decimal)", "Nullable(Of Integer)" };

        private readonly ITypeIdentifierGenerator _typeIdentifierGenerator;
        public VisualBasicTypeGenerator()
        {
            _typeIdentifierGenerator = new VisualBasicTypeIdentifierGenerator();
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
                    SyntaxFactory.SimpleImportsClause(SyntaxFactory.ParseName("System.Linq")))),
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

        protected StatementSyntax CreateProperty(string name, string type = null, SyntaxList<AttributeListSyntax> attributes = new SyntaxList<AttributeListSyntax>())
        {
            return SyntaxFactory.PropertyStatement(_typeIdentifierGenerator.GenerateIdentifier(name))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithAttributeLists(attributes)
                .WithAsClause(
                    SyntaxFactory.SimpleAsClause(SyntaxFactory.IdentifierName(_typeIdentifierGenerator.GenerateIdentifier(type ?? name))));
        }
    }
}