using Microsoft.CodeAnalysis.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CalculateFunding.Services.CodeGeneration.VisualBasic.Type
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
                if (!char.IsLetter(className, 0))
                {
                    className = className.Insert(0, "_");
                }
            }

            return className.Replace(" ", string.Empty);
        }

        //protected static SyntaxList<ImportsStatementSyntax> StandardImports()
        //{
        //    return SyntaxFactory.List(new[] {
        //        SyntaxFactory.ImportsStatement(SyntaxFactory.SingletonSeparatedList<ImportsClauseSyntax>(
        //            SyntaxFactory.SimpleImportsClause(SyntaxFactory.ParseName("System")))),
        //        SyntaxFactory.ImportsStatement(SyntaxFactory.SingletonSeparatedList<ImportsClauseSyntax>(
        //            SyntaxFactory.SimpleImportsClause(SyntaxFactory.ParseName("System.Linq")))),
        //        SyntaxFactory.ImportsStatement(SyntaxFactory.SingletonSeparatedList<ImportsClauseSyntax>(
        //            SyntaxFactory.SimpleImportsClause(SyntaxFactory.ParseName("System.Collections.Generic")))),
        //        SyntaxFactory.ImportsStatement(SyntaxFactory.SingletonSeparatedList<ImportsClauseSyntax>(
        //            SyntaxFactory.SimpleImportsClause(SyntaxFactory.ParseName("Microsoft.VisualBasic.CompilerServices"))))
        //    });
        //}

        //protected static StatementSyntax ParseSourceCodeToStatementSyntax(string sourceCode)
        //{
        //    return SyntaxFactory.ParseSyntaxTree(sourceCode)
        //        .GetRoot()
        //        .DescendantNodes()
        //        .OfType<StatementSyntax>()
        //        .FirstOrDefault();     
        //}
        
        //protected static StatementSyntax ParseSourceCodeToStatementSyntax(StringBuilder sourceCode)
        //{
        //    return ParseSourceCodeToStatementSyntax(sourceCode.ToString());
        //}

        
        //protected static StatementSyntax CreateProperty(string name, string type = null, SyntaxList<AttributeListSyntax> attributes = new SyntaxList<AttributeListSyntax>())
        //{
        //    return SyntaxFactory.PropertyStatement(GenerateIdentifier(name))
        //        .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
        //        .WithAttributeLists(attributes)
        //        .WithAsClause(
        //            SyntaxFactory.SimpleAsClause(SyntaxFactory.IdentifierName(GenerateIdentifier(type ?? name))));
        //}
    }
}