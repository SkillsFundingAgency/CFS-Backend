using CalculateFunding.Models.Calcs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Services.CodeGeneration.VisualBasic
{
    public class FundingLineNamespaceBuilder : VisualBasicTypeGenerator
    {
        public NamespaceBuilderResult BuildNamespacesForFundingLines(IDictionary<string, Funding> funding)
        {
            NamespaceBuilderResult result = new NamespaceBuilderResult();

            result.PropertiesDefinitions = new StatementSyntax[0];

            foreach (string @namespace in funding.Keys)
            {
                ClassBlockSyntax @class = SyntaxFactory.ClassBlock(
                                    SyntaxFactory.ClassStatement(
                                            GenerateIdentifier($"{@namespace}FundingLines")
                                        )
                                        .WithModifiers(
                                            SyntaxFactory.TokenList(
                                                SyntaxFactory.Token(SyntaxKind.PublicKeyword))),
                                    new SyntaxList<InheritsStatementSyntax>(),
                                    new SyntaxList<ImplementsStatementSyntax>(),
                                    SyntaxFactory.List(CreateFundingLineClass(funding[@namespace].FundingLines.DistinctBy(_ => _.Id), @namespace)),
                                    SyntaxFactory.EndClassStatement()
                                );

                result.InnerClasses.Add(new NamespaceClassDefinition(GenerateIdentifier(@namespace), @class, "FundingLines", "FundingLines"));
            }

            return result;
        }


        private IEnumerable<StatementSyntax> CreateFundingLineClass(IEnumerable<FundingLine> fundingLines, string @namespace)
        {
            if (fundingLines == null) yield break;

            foreach (FundingLine fundingLine in fundingLines)
            {
                StringBuilder sourceCode = new StringBuilder();
                fundingLine.SourceCodeName ??= GenerateIdentifier(fundingLine.Name);
                sourceCode.AppendLine($"<FundingLine(FundingStream := \"{@namespace}\", Id := \"{fundingLine.Id}\", Name := \"{fundingLine.SourceCodeName}\")>");
                sourceCode.AppendLine($"Public {fundingLine.SourceCodeName} As Func(Of decimal?) = Nothing");
                sourceCode.AppendLine();
                yield return ParseSourceCodeToStatementSyntax(sourceCode);
            }

            yield return ParseSourceCodeToStatementSyntax($"Public Property {GenerateIdentifier(@namespace)} As {GenerateIdentifier(@namespace)}Calculations");

            // create funding line initialise method
            yield return CreateInitialiseMethod(fundingLines.Where(_ => _.Namespace == @namespace), @namespace);
        }

        private StatementSyntax CreateInitialiseMethod(IEnumerable<FundingLine> fundingLines, string @namespace)
        {
            StringBuilder sourceCode = new StringBuilder();

            sourceCode.AppendLine("Public Sub Initialise(calculationContext As CalculationContext)");
            sourceCode.AppendLine($"{GenerateIdentifier(@namespace)} = calculationContext.{GenerateIdentifier(@namespace)}");
            sourceCode.AppendLine();

            foreach (FundingLine fundingLine in fundingLines.Where(_ => !_.Calculations.IsNullOrEmpty()))
            {
                fundingLine.SourceCodeName ??= GenerateIdentifier(fundingLine.Name);

                sourceCode.AppendLine($"{fundingLine.SourceCodeName} = Function() As Decimal?");
                sourceCode.AppendLine();
                sourceCode.AppendLine("Dim existingCacheItem as String() = Nothing");
                sourceCode.AppendLine($"If calculationContext.FundingLineDictionary.TryGetValue(\"{@namespace}-{fundingLine.Id}\", existingCacheItem) Then");
                sourceCode.AppendLine("Dim existingFundingLineResultDecimal As Decimal? = Nothing");
                sourceCode.AppendLine($"   If calculationContext.FundingLineDictionaryValues.TryGetValue(\"{@namespace}-{fundingLine.Id}\", existingFundingLineResultDecimal) Then");
                sourceCode.AppendLine("        Return existingFundingLineResultDecimal");
                sourceCode.AppendLine("    End If");

                sourceCode.AppendLine("    If existingCacheItem.Length > 2 Then");
                sourceCode.AppendLine("       Dim exceptionType as String = existingCacheItem(1)");
                sourceCode.AppendLine("        If Not String.IsNullOrEmpty(exceptionType) then");
                sourceCode.AppendLine("            Dim exceptionMessage as String = existingCacheItem(2)");
                sourceCode.AppendLine($"           Throw New ReferencedFundingLineFailedException(\"{fundingLine.Name} failed due to exception type:\" + exceptionType  + \" with message: \" + exceptionMessage)");
                sourceCode.AppendLine("        End If");
                sourceCode.AppendLine("    End If");
                sourceCode.AppendLine("End If");

                sourceCode.AppendLine($"Dim userCalculationCodeImplementation As Func(Of Decimal?) = Function() As Decimal?");
                sourceCode.AppendLine($"Dim calcs As List(Of Decimal?) = New List(Of Decimal?)");

                foreach (FundingLineCalculation calculation in fundingLine.Calculations)
                {
                    sourceCode.AppendLine($"calcs.Add({GenerateIdentifier(calculation.Namespace)}.{calculation.SourceCodeName}())");
                }

                sourceCode.AppendLine($"Return calcs.Sum()");
                sourceCode.AppendLine("End Function");

                sourceCode.AppendLine("Try");
                sourceCode.AppendLine($"Dim executedFundingLineResult As Decimal?  = userCalculationCodeImplementation()");
                sourceCode.AppendLine();
                sourceCode.AppendLine(
                    $"calculationContext.FundingLineDictionary.Add(\"{@namespace}-{fundingLine.Id}\", {{If(executedFundingLineResult.HasValue, executedFundingLineResult.ToString(), \"\")}})");
                sourceCode.AppendLine($"calculationContext.FundingLineDictionaryValues.Add(\"{@namespace}-{fundingLine.Id}\", executedFundingLineResult)");
                sourceCode.AppendLine("Return executedFundingLineResult");
                sourceCode.AppendLine("Catch ex as System.Exception");
                sourceCode.AppendLine($"   calculationContext.FundingLineDictionary.Add(\"{@namespace}-{fundingLine.Id}\", {{\"\", ex.GetType().Name, ex.Message}})");
                sourceCode.AppendLine("    Throw");
                sourceCode.AppendLine("End Try");
                sourceCode.AppendLine();

                sourceCode.AppendLine("End Function");
            }

            sourceCode.AppendLine();
            sourceCode.AppendLine("End Sub");

            return ParseSourceCodeToStatementSyntax(sourceCode);
        }
    }
}