using System;
using System.CodeDom.Compiler;
using System.Text.RegularExpressions;
using Allocations.Models.Specs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Allocations.Services.Compiler.VisualBasic
{
    public abstract class VisualBasicTypeGenerator
    {
        public static string Identifier(string value)
        {
            string className = value;
            bool isValid = CodeDomProvider.CreateProvider("VisualBasic").IsValidIdentifier(className);

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
            var titleCase = Identifier(value);
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
                    propertyType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ULongKeyword));
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
            var imports = SyntaxFactory.SingletonList(
                SyntaxFactory.ImportsStatement(SyntaxFactory.SingletonSeparatedList<ImportsClauseSyntax>(SyntaxFactory.SimpleImportsClause(SyntaxFactory.ParseName("System")))));
            var str = imports.ToFullString();
            return imports;
        }

        //protected static SyntaxList<ImportsStatementSyntax> StandardUsings()
        //{
        //    return SyntaxFactory.List(
        //        new[]{
        //            SyntaxFactory.SimpleImportsClause(
        //                SyntaxFactory.IdentifierName("System")),
        //            });
        //}

    }
}