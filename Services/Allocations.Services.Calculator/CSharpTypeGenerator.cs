using System;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Allocations.Services.Calculator
{
    public abstract class CSharpTypeGenerator
    {
        protected static string Identifier(string value)
        {
            string className = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value);
            bool isValid = CodeDomProvider.CreateProvider("C#").IsValidIdentifier(className);

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

        protected static string IdentifierCamelCase(string value)
        {
            var titleCase = Identifier(value);
            return Char.ToLowerInvariant(titleCase[0]) + titleCase.Substring(1);
        }

        protected static TypeSyntax GetType(TypeCode type)
        {
            TypeSyntax propertyType;
            switch (type)
            {
                case TypeCode.Boolean:
                    propertyType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword));
                    break;
                case TypeCode.Char:
                    propertyType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.CharKeyword));
                    break;
                case TypeCode.SByte:
                    propertyType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.SByteKeyword));
                    break;
                case TypeCode.Byte:
                    propertyType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ByKeyword));
                    break;
                case TypeCode.Int16:
                    propertyType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ShortKeyword));
                    break;
                case TypeCode.UInt16:
                    propertyType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.UShortKeyword));
                    break;
                case TypeCode.Int32:
                    propertyType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword));
                    break;
                case TypeCode.UInt32:
                    propertyType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.UIntKeyword));
                    break;
                case TypeCode.Int64:
                    propertyType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ULongKeyword));
                    break;
                case TypeCode.UInt64:
                    propertyType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.LoadKeyword));
                    break;
                case TypeCode.Single:
                    propertyType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.FloatKeyword));
                    break;
                case TypeCode.Double:
                    propertyType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DoubleKeyword));
                    break;
                case TypeCode.Decimal:
                    propertyType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DecimalKeyword));
                    break;
                case TypeCode.DateTime:
                    propertyType = SyntaxFactory.IdentifierName("DateTime");
                    break;
                default:
                    propertyType = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword));
                    break;
            }
            return propertyType;
        }

        protected static SyntaxList<UsingDirectiveSyntax> StandardUsings()
        {
            return SyntaxFactory.List(
                new[]{
                    SyntaxFactory.UsingDirective(
                        SyntaxFactory.QualifiedName(
                            SyntaxFactory.IdentifierName("System"),
                            SyntaxFactory.IdentifierName("ComponentModel"))),
                    SyntaxFactory.UsingDirective(
                        SyntaxFactory.QualifiedName(
                            SyntaxFactory.QualifiedName(
                                SyntaxFactory.IdentifierName("Allocations"),
                                SyntaxFactory.IdentifierName("Models")),
                            SyntaxFactory.IdentifierName("Datasets"))),
                    SyntaxFactory.UsingDirective(
                        SyntaxFactory.QualifiedName(
                            SyntaxFactory.QualifiedName(
                                SyntaxFactory.IdentifierName("Allocations"),
                                SyntaxFactory.IdentifierName("Models")),
                            SyntaxFactory.IdentifierName("Framework"))),
                    SyntaxFactory.UsingDirective(
                        SyntaxFactory.QualifiedName(
                            SyntaxFactory.IdentifierName("Newtonsoft"),
                            SyntaxFactory.IdentifierName("Json")))});
        }

    }
}