﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Calculation = CalculateFunding.Models.Calcs.Calculation;

namespace CalculateFunding.Services.CodeGeneration.VisualBasic
{
    public class ProductTypeGenerator : VisualBasicTypeGenerator
    {

        public IEnumerable<SourceFile> GenerateCalcs(BuildProject budget)
        {
            var syntaxTree = SyntaxFactory.CompilationUnit()
                .WithImports(StandardImports())
                
                .WithMembers(SyntaxFactory.SingletonList<StatementSyntax>(
            SyntaxFactory.ClassBlock(
                SyntaxFactory.ClassStatement(
                        Identifier("Calculations")
                    )
                    
                    .WithModifiers(
                        SyntaxFactory.TokenList(
                            SyntaxFactory.Token(SyntaxKind.PublicKeyword))),
                new SyntaxList<InheritsStatementSyntax>(SyntaxFactory.InheritsStatement(SyntaxFactory.ParseTypeName("BaseCalculation"))),
                new SyntaxList<ImplementsStatementSyntax>(),
                SyntaxFactory.List(Methods(budget)),
                SyntaxFactory.EndClassStatement()
            )
  
                    ))
                .NormalizeWhitespace();

            yield return new SourceFile {FileName = "Calculations.vb", SourceCode = syntaxTree.ToFullString()};
        }

        private static IEnumerable<StatementSyntax> Methods(BuildProject buildProject)
        {
            yield return GetStandardProperties();
            foreach (var calc in buildProject.Calculations)
            {
                yield return GetMethod(calc);
            }
        }


        private static StatementSyntax GetMethod(Calculation calc)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"<Calculation(Id := \"{calc.Id}\")>");
            if (calc.CalculationSpecification!= null)
            {
                builder.AppendLine($"<CalculationSpecification(Id := \"{calc.CalculationSpecification.Id}\", Name := \"{calc.CalculationSpecification.Name}\")>");
            }
            if (calc.AllocationLine != null)
            {
                builder.AppendLine($"<AllocationLine(Id := \"{calc.AllocationLine.Id}\", Name := \"{calc.AllocationLine.Name}\")>");
            }
            if (calc.Policies != null)
            {
                foreach (var policySpecification in calc.Policies)
                {
                    builder.AppendLine($"<PolicySpecification(Id := \"{policySpecification.Id}\", Name := \"{policySpecification.Name}\")>");
                }
            }

            builder.AppendLine($"Public Function {Identifier(calc.Name)} As Decimal");
	        builder.AppendLine($"#ExternalSource(\"{calc.Id}\", 1)");
			builder.Append(calc.Published?.SourceCode ?? "Return Decimal.MinValue");
            //builder.Append(calc.SourceCode ?? $"Throw new NotImplementedException(\"{calc.Name} is not implemented\")");
            builder.AppendLine();
	        builder.AppendLine("#End ExternalSource");
			builder.AppendLine("End Function");
            builder.AppendLine();
            var tree = SyntaxFactory.ParseSyntaxTree(builder.ToString());

            
            return tree.GetRoot().DescendantNodes().OfType<StatementSyntax>()
                .FirstOrDefault();
        }



        private static StatementSyntax GetStandardProperties()
        {
            return SyntaxFactory.PropertyStatement(Identifier("Datasets"))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                .WithAsClause(
                    SyntaxFactory.SimpleAsClause(SyntaxFactory.IdentifierName(Identifier("Datasets"))));
        }


    }
}
