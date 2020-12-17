using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CalculateFunding.Models.Calcs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace CalculateFunding.Services.CodeGeneration.VisualBasic
{
    public class CalculationNamespaceBuilder : VisualBasicTypeGenerator
    {
        private readonly CompilerOptions _compilerOptions;

        public CalculationNamespaceBuilder(CompilerOptions compilerOptions)
        {
            _compilerOptions = compilerOptions;
        }

        public NamespaceBuilderResult BuildNamespacesForCalculations(IEnumerable<Calculation> calculations)
        {
            NamespaceBuilderResult result = new NamespaceBuilderResult();

            IEnumerable<IGrouping<string, Calculation>> fundingStreamCalculationGroups
                = calculations.Where(_ => _.Current.Namespace == CalculationNamespace.Template)
                    .GroupBy(_ => _.FundingStreamId)
                    .ToArray();
            IEnumerable<string> fundingStreamNamespaces = fundingStreamCalculationGroups.Select(_ => _.Key).ToArray();
            IEnumerable<Calculation> additionalCalculations = calculations.Where(_ => _.Current.Namespace == CalculationNamespace.Additional)
                .ToArray();

            IEnumerable<string> propertyAssignments = CreatePropertyAssignments(fundingStreamNamespaces);
            IEnumerable<(StatementSyntax Syntax, bool IsFundingLines, string Namespace)> propertyDefinitions = CreateProperties(fundingStreamNamespaces);

            result.PropertiesDefinitions = propertyDefinitions.Where(_ => !_.IsFundingLines).Select(_ => _.Syntax).ToArray();
            
            result.EnumsDefinitions = CreateEnums(calculations);

            foreach (IGrouping<string, Calculation> fundingStreamCalculationGroup in fundingStreamCalculationGroups)
                result.InnerClasses.Add(CreateNamespaceDefinition(GenerateIdentifier(fundingStreamCalculationGroup.Key),
                    fundingStreamCalculationGroup,
                    propertyDefinitions.Where(_ => _.Namespace == fundingStreamCalculationGroup.Key || string.IsNullOrWhiteSpace(_.Namespace)).Select(_ => _.Syntax),
                    propertyAssignments));

            result.InnerClasses.Add(CreateNamespaceDefinition("Calculations",
                additionalCalculations,
                propertyDefinitions.Where(_ => !_.IsFundingLines).Select(_ => _.Syntax),
                propertyAssignments,
                "AdditionalCalculations"));

            return result;
        }

        private NamespaceClassDefinition CreateNamespaceDefinition(
            string @namespace,
            IEnumerable<Calculation> calculationsInNamespace,
            IEnumerable<StatementSyntax> propertyDefinitions,
            IEnumerable<string> propertyAssignments,
            string className = null)
        {
            ClassStatementSyntax classStatement = SyntaxFactory
                .ClassStatement(className ?? $"{@namespace}Calculations")
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));

            SyntaxList<InheritsStatementSyntax> inherits = SyntaxFactory.SingletonList(SyntaxFactory.InheritsStatement(_compilerOptions.UseLegacyCode
                ? SyntaxFactory.ParseTypeName("LegacyBaseCalculation")
                : SyntaxFactory.ParseTypeName("BaseCalculation")));

            IEnumerable<StatementSyntax> namespaceFunctionPointers = CreateNamespaceFunctionPointers(calculationsInNamespace);

            StatementSyntax initialiseMethodDefinition = CreateInitialiseMethod(calculationsInNamespace, propertyAssignments, className);

            ClassBlockSyntax classBlock = SyntaxFactory.ClassBlock(classStatement,
                inherits,
                new SyntaxList<ImplementsStatementSyntax>(),
                SyntaxFactory.List(propertyDefinitions
                    .Concat(namespaceFunctionPointers)
                    .Concat(new[]
                    {
                        initialiseMethodDefinition
                    }).ToArray()),
                SyntaxFactory.EndClassStatement());

            return new NamespaceClassDefinition(@namespace, classBlock);
        }

        private static IEnumerable<string> CreatePropertyAssignments(IEnumerable<string> namespaces)
        {
            return namespaces.Select(@namespace => string.Format("{0} = calculationContext.{0}", GenerateIdentifier(@namespace)))
                .Concat(new[]
                {
                    "Calculations = calculationContext.Calculations"
                })
                .ToArray();
        }

        private static IEnumerable<(StatementSyntax Syntax, bool IsFundingLines, string @namespace)> CreateProperties(IEnumerable<string> namespaces)
        {
            yield return (CreateProperty("Provider"), false, null);
            yield return (CreateProperty("Datasets"), false, null);

            SyntaxList<AttributeListSyntax> list = new SyntaxList<AttributeListSyntax>(SyntaxFactory.AttributeList(
                            SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Attribute(null, SyntaxFactory.IdentifierName("IsAggregable"),
                                SyntaxFactory.ArgumentList(
                                    SyntaxFactory.SeparatedList<ArgumentSyntax>(new[]
                                    {
                                    SyntaxFactory.SimpleArgument(
                                        SyntaxFactory.NameColonEquals(SyntaxFactory.IdentifierName("IsAggregable")),
                                        SyntaxFactory.LiteralExpression(
                                            SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(@"True"))
                                        )
                                    }))))));

            yield return (CreateProperty("Calculations", "AdditionalCalculations", list), false, null);

            foreach (var @namespace in namespaces)
            {
                yield return (CreateProperty(@namespace, $"{@namespace}Calculations", list), false, @namespace);
                yield return (CreateProperty("FundingLines", $"{@namespace}FundingLines"), true, @namespace);
            }
        }

        private static IEnumerable<StatementSyntax> CreateEnums(IEnumerable<Calculation> calculations)
        {
            foreach (Calculation calculation in calculations.Where(c => c.Current.DataType == CalculationDataType.Enum))
            {
                StringBuilder sourceCode = new StringBuilder();

                sourceCode.AppendLine($"Public Enum {GetEnumVariableName(calculation.Name)}");
                if (calculation.Current.AllowedEnumTypeValues.AnyWithNullCheck())
                {
                    foreach (string value in calculation.Current.AllowedEnumTypeValues)
                    {
                        sourceCode.AppendLine($"    {VisualBasicTypeGenerator.GenerateIdentifier(value)}");
                    }
                }
                else
                {
                    sourceCode.AppendLine($"    None");
                }
                sourceCode.AppendLine("End Enum");
                sourceCode.AppendLine();

                yield return ParseSourceCodeToStatementSyntax(sourceCode);
            }
        }

        private static IEnumerable<StatementSyntax> CreateNamespaceFunctionPointers(IEnumerable<Calculation> calculations)
        {
            foreach (Calculation calculation in calculations)
            {
                StringBuilder sourceCode = new StringBuilder();

                CalculationVersion currentCalculationVersion = calculation.Current;

                if (string.IsNullOrWhiteSpace(currentCalculationVersion.SourceCodeName)) throw new InvalidOperationException($"Calculation source code name is not populated for calc {calculation.Id}");

                // Add attributes to describe calculation and calculation specification
                sourceCode.AppendLine($"<Calculation(Id := \"{calculation.Id}\", Name := \"{calculation.Name}\", CalculationDataType := \"{calculation.Current.DataType}\")>");

                // Add attribute for calculation description
                if (currentCalculationVersion.Description.IsNotNullOrWhitespace()) sourceCode.AppendLine($"<Description(Description := \"{currentCalculationVersion.Description?.Replace("\"", "\"\"")}\")>");

                sourceCode.AppendLine($"Public {currentCalculationVersion.SourceCodeName} As Func(Of {GetDataType(calculation.Current.DataType, calculation.Name)}) = Nothing");

                sourceCode.AppendLine();

                yield return ParseSourceCodeToStatementSyntax(sourceCode);
            }
        }

        private StatementSyntax CreateInitialiseMethod(IEnumerable<Calculation> calculations, IEnumerable<string> propertyAssignments, string className)
        {
            StringBuilder sourceCode = new StringBuilder();

            sourceCode.AppendLine("Public Initialise As Action(Of CalculationContext) = Sub(calculationContext)");
            sourceCode.AppendLine("Datasets = calculationContext.Datasets");
            sourceCode.AppendLine("Provider = calculationContext.Provider");
            sourceCode.AppendLine();

            foreach (var propertyAssignment in propertyAssignments) sourceCode.AppendLine(propertyAssignment);

            sourceCode.AppendLine();

            foreach (Calculation calculation in calculations)
            {
                if (!string.IsNullOrWhiteSpace(calculation.Current?.SourceCode)) calculation.Current.SourceCode = QuoteAggregateFunctionCalls(calculation.Current.SourceCode);

                sourceCode.AppendLine();

                sourceCode.AppendLine($"{calculation.Current.SourceCodeName} = Function() As {GetDataType(calculation.Current.DataType, calculation.Name)}");
                sourceCode.AppendLine("Dim existingCacheItem as String() = Nothing");
                sourceCode.AppendLine($"If calculationContext.Dictionary.TryGetValue(\"{calculation.Id}\", existingCacheItem) Then");
                sourceCode.AppendLine($"   Dim existingCalculationResult{GetVariableName(calculation.Current.DataType)} As {GetExistingCalculationResultDataType(calculation.Current.DataType)} = Nothing");
                sourceCode.AppendLine($"   If calculationContext.Dictionary{GetVariableName(calculation.Current.DataType)}Values.TryGetValue(\"{calculation.Id}\", existingCalculationResult{GetVariableName(calculation.Current.DataType)}) Then");
                sourceCode.AppendLine(GetConditionalExistingCalculationResult(calculation));
                sourceCode.AppendLine("    End If");

                sourceCode.AppendLine("    If existingCacheItem.Length > 2 Then");
                sourceCode.AppendLine("       Dim exceptionType as String = existingCacheItem(1)");
                sourceCode.AppendLine("        If Not String.IsNullOrEmpty(exceptionType) then");
                sourceCode.AppendLine("            Dim exceptionMessage as String = existingCacheItem(2)");
                sourceCode.AppendLine(
                    $"           Throw New ReferencedCalculationFailedException(\"{calculation.Name} failed due to exception type:\" + exceptionType  + \" with message: \" + exceptionMessage)");
                sourceCode.AppendLine("        End If");
                sourceCode.AppendLine("    End If");
                sourceCode.AppendLine("End If");
                sourceCode.AppendLine($"Dim userCalculationCodeImplementation As Func(Of {GetDataType(calculation.Current.DataType, calculation.Name)}) = Function() as {GetDataType(calculation.Current.DataType, calculation.Name)}");
                sourceCode.AppendLine("Dim frameCount = New System.Diagnostics.StackTrace().FrameCount");
                sourceCode.AppendLine("If frameCount > calculationContext.StackFrameStartingCount + 40 Then");
                sourceCode.AppendLine(
                    $"   Throw New CalculationStackOverflowException(\"The system detected a stackoverflow from {calculation.Name}, this is probably due to recursive methods stuck in an infinite loop\")");
                sourceCode.AppendLine("End If");

                sourceCode.AppendLine($"#ExternalSource(\"{calculation.Id}|{calculation.Name}\", 1)");
                sourceCode.AppendLine();
                sourceCode.AppendLine(GetSourceCodeOrDefault(calculation));
                sourceCode.AppendLine();
                sourceCode.AppendLine("#End ExternalSource");

                sourceCode.AppendLine("End Function");
                sourceCode.AppendLine();

                sourceCode.AppendLine("Try");
                sourceCode.AppendLine($"Dim executedUserCodeCalculationResult As {GetDataType(calculation.Current.DataType, calculation.Name)} = userCalculationCodeImplementation()");
                sourceCode.AppendLine();
                sourceCode.AppendLine(
                    $"calculationContext.Dictionary.Add(\"{calculation.Id}\", {GetConditionalDictionaryAddSourceCode(calculation.Current.DataType)})");
                sourceCode.AppendLine(GetConditionalAddValuesToDictionarySourceCode(calculation));
                sourceCode.AppendLine("Return executedUserCodeCalculationResult");
                sourceCode.AppendLine("Catch ex as System.Exception");
                sourceCode.AppendLine("    If Not calculationContext.Dictionary.ContainsKey(\"{calculation.Id}\")");
                sourceCode.AppendLine($"       calculationContext.Dictionary.Add(\"{calculation.Id}\", {{\"\", ex.GetType().Name, ex.Message}})");
                sourceCode.AppendLine("    End If");
                sourceCode.AppendLine("    Throw");
                sourceCode.AppendLine("End Try");
                sourceCode.AppendLine();

                sourceCode.AppendLine("End Function");
            }

            sourceCode.AppendLine();
            sourceCode.AppendLine("End Sub");

            return ParseSourceCodeToStatementSyntax(sourceCode);
        }

        private string GetConditionalExistingCalculationResult(Calculation calculation)
        {
            return calculation.Current.DataType == CalculationDataType.Enum ?
              $"    Return If(String.IsNullOrWhiteSpace(existingCalculationResult{GetVariableName(calculation.Current.DataType)}), Nothing, CType([Enum].Parse(GetType({GetEnumVariableName(calculation.Name)}), existingCalculationResult{GetVariableName(calculation.Current.DataType)}), {GetEnumVariableName(calculation.Name)}?))"
            : $"    Return existingCalculationResult{GetVariableName(calculation.Current.DataType)}";

        }

        private static string GetConditionalAddValuesToDictionarySourceCode(Calculation calculation)
        {
            if (calculation.Current.DataType == CalculationDataType.Enum)
            {
                return $"calculationContext.Dictionary{GetVariableName(calculation.Current.DataType)}Values.Add(\"{calculation.Id}\", executedUserCodeCalculationResult?.ToString())";
            }
            else
            {
                return $"calculationContext.Dictionary{GetVariableName(calculation.Current.DataType)}Values.Add(\"{calculation.Id}\", executedUserCodeCalculationResult)";
            }
        }

        private static string GetSourceCodeOrDefault(Calculation calculation)
        {
            switch (calculation.Current.DataType)
            {
                case CalculationDataType.Enum:
                case CalculationDataType.Boolean:
                    return calculation.Current.SourceCode ?? "Return Nothing";
                default:
                    return calculation.Current?.SourceCode ?? CodeGenerationConstants.VisualBasicDefaultSourceCode;
            }
        }

        private static string GetDataType(CalculationDataType calculationDataType, string calculationName) => calculationDataType switch
        {
            CalculationDataType.Decimal => "Decimal?",
            CalculationDataType.String => "String",
            CalculationDataType.Boolean => "Boolean?",
            CalculationDataType.Enum => $"{GetEnumVariableName(calculationName)}?",
            _ => throw new ArgumentOutOfRangeException(),
        };

        private static string GetExistingCalculationResultDataType(CalculationDataType calculationDataType) => calculationDataType switch
        {
            CalculationDataType.Decimal => "Decimal?",
            CalculationDataType.String => "String",
            CalculationDataType.Boolean => "Boolean?",
            CalculationDataType.Enum => "String",
            _ => throw new ArgumentOutOfRangeException(),
        };

        private static string GetVariableName(CalculationDataType calculationDataType) => calculationDataType switch
        {
            CalculationDataType.Decimal => "Decimal",
            CalculationDataType.String => "String",
            CalculationDataType.Boolean => "Boolean",
            CalculationDataType.Enum => "String",
            _ => throw new ArgumentOutOfRangeException(),
        };

        private static string GetConditionalDictionaryAddSourceCode(CalculationDataType calculationDataType) => calculationDataType switch
        {
            CalculationDataType.Decimal => $"{{If(executedUserCodeCalculationResult.HasValue, executedUserCodeCalculationResult.ToString(), \"\")}}",
            CalculationDataType.String => $"{{executedUserCodeCalculationResult}}",
            CalculationDataType.Boolean => $"{{If(executedUserCodeCalculationResult.HasValue, executedUserCodeCalculationResult.ToString(), \"\")}}",
            CalculationDataType.Enum => $"{{If(executedUserCodeCalculationResult.HasValue, executedUserCodeCalculationResult.ToString(), \"\")}}",
            _ => throw new ArgumentOutOfRangeException(),
        };

        private static string GetEnumVariableName(string calculationName) => $"{GenerateIdentifier(calculationName)}Options";
    }
}