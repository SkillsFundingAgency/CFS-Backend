using System.Collections.Generic;
using System.Linq;
using System.Text;
using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CalculateFunding.Services.CodeGeneration.CSharp
{
    public class ProductTypeGenerator : CSharpTypeGenerator
    {
        public IEnumerable<SourceFile> GenerateCalcs(Implementation budget)
        {
            var syntaxTree = SyntaxFactory.CompilationUnit()
                .WithUsings(StandardUsings())
                .WithMembers(
                    SyntaxFactory.List<MemberDeclarationSyntax>(
                        Classes(budget)
                        ))
                .NormalizeWhitespace();

            yield return new SourceFile{FileName = "Calculations.cs", SourceCode = syntaxTree.ToFullString()};
        }

        private static IEnumerable<ClassDeclarationSyntax> Classes(Implementation budget)
        {
            var members = new List<MemberDeclarationSyntax>();
            members.AddRange(budget.DatasetDefinitions.Select(GetMembers));
            members.AddRange(budget.Calculations.Select(GetMethod));

                yield return SyntaxFactory.ClassDeclaration(Identifier("Calculations"))
                    .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("BaseCalculation")))
                    .WithModifiers(
                        SyntaxFactory.TokenList(
                            SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                            SyntaxFactory.Token(SyntaxKind.PartialKeyword)))
                    .WithMembers(
                        SyntaxFactory.List(members)
                        );

            
        }

        private static MethodDeclarationSyntax GetMethod(Calculation calc)
        {
            var builder = new StringBuilder();
            //builder.AppendLine($"[Calculation(Id = \"{calc.Id}\")]");
            //if (calc.CalculationSpecification != null)
            //{
            //    builder.AppendLine($"[CalculationSpecification(Id = \"{calc.CalculationSpecification.Id}\", Name = \"{calc.CalculationSpecification.Name}\")]");
            //}
            //if (calc.AllocationLine != null)
            //{
            //    builder.AppendLine($"[AllocationLine(Id = \"{calc.AllocationLine.Id}\", Name = \"{calc.AllocationLine.Name}\")]");
            //}
            //if (calc.PolicySpecifications != null)
            //{
            //    foreach (var policySpecification in calc.PolicySpecifications)
            //    {
            //        builder.AppendLine($"P[olicySpecification(Id = \"{policySpecification.Id}\", Name = \"{policySpecification.Name}\")]");
            //    }
            //}

            builder.AppendLine($"public decimal {Identifier(calc.Name)}()");
            builder.AppendLine("{");
            builder.Append(calc.SourceCode ?? "return decimal.MinValue;");
            builder.AppendLine("}");
            //builder.Append(calc.SourceCode ?? $"Throw new NotImplementedException(\"{calc.Name} is not implemented\")");
            builder.AppendLine();
            var tree = SyntaxFactory.ParseSyntaxTree(builder.ToString());


            return tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()
                .First().WithAttributeLists(GetMethodAttributes(calc));
        }


        private static SyntaxList<AttributeListSyntax> GetMethodAttributes(Calculation calc)
        {
            var list = new List<AttributeSyntax>
            {
                SyntaxFactory.Attribute(
                    SyntaxFactory.ParseName("Calculation"),
                    SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(new[]
                    {
                        SyntaxFactory.AttributeArgument(SyntaxFactory.NameEquals("Id"), null,
                            SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal(calc.Id)))
                    })))
            };

            if (calc.CalculationSpecification != null)
            {
                list.Add(GetAttribute("CalculationSpecification", calc.CalculationSpecification));
            }
            if (calc.AllocationLine != null)
            {
                list.Add(GetAttribute("AllocationLine", calc.AllocationLine));
            }
            if (calc.PolicySpecifications != null)
            {
                foreach (var policySpecification in calc.PolicySpecifications)
                {
                    list.Add(GetAttribute("PolicySpecification", policySpecification));
                }
            }


            return SyntaxFactory.SingletonList(SyntaxFactory.AttributeList(
                SyntaxFactory.SeparatedList(list)));
        }

        private static AttributeSyntax GetAttribute(string attributeName, Reference reference)
        {
            return SyntaxFactory.Attribute(
                SyntaxFactory.ParseName(attributeName),
                SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.AttributeArgument(SyntaxFactory.NameEquals("Id"), null,
                        SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal(reference.Id))),
                    SyntaxFactory.AttributeArgument(SyntaxFactory.NameEquals("Name"), null,
                        SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal(reference.Name))),
                })));
        }


        private static PropertyDeclarationSyntax GetMembers(DatasetDefinition datasetDefinition)
        {
            return SyntaxFactory.PropertyDeclaration(
                    SyntaxFactory.IdentifierName(Identifier($"{datasetDefinition.Name}Dataset")), Identifier(datasetDefinition.Name))
                 .WithModifiers(
                    SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithAccessorList(
                    SyntaxFactory.AccessorList(
                        SyntaxFactory.List(
                            new[]
                            {
                                SyntaxFactory.AccessorDeclaration(
                                        SyntaxKind.GetAccessorDeclaration)
                                    .WithSemicolonToken(
                                        SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                                SyntaxFactory.AccessorDeclaration(
                                        SyntaxKind.SetAccessorDeclaration)
                                    .WithSemicolonToken(
                                        SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                            })));
        }

    }
}
