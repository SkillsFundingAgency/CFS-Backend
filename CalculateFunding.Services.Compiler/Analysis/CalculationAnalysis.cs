using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Compiler.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Calculation = CalculateFunding.Models.Calcs.Calculation;
using FundingLine = CalculateFunding.Models.Calcs.FundingLine;
using GraphFundingLine = CalculateFunding.Models.Graph.FundingLine;

namespace CalculateFunding.Services.Compiler.Analysis
{
    public class CalculationAnalysis : ICalculationAnalysis
    {
        public IEnumerable<CalculationRelationship> DetermineRelationshipsBetweenCalculations(Func<string, string> GetSourceCodeName, IEnumerable<Calculation> calculations)
        {
            Guard.IsNotEmpty(calculations, nameof(calculations));
            
            return calculations.SelectMany(_ =>
            {
                IEnumerable<Calculation> relatedCalculationNames = GetReferencedCalculations(GetSourceCodeName(_.Namespace), _.Current.SourceCode, calculations);

                return relatedCalculationNames.Select(rel => new CalculationRelationship
                {
                    CalculationOneId = _.Id,
                    CalculationTwoId = rel.Id
                });
            });
        }

        public IEnumerable<CalculationRelationship> DetermineRelationshipsBetweenReleasedDataCalculations(
            Func<string, string> GetSourceCodeName,
            IEnumerable<Calculation> sourceCalculations,
            IEnumerable<DatasetRelationshipSummary> datasetRelationshipSummaries)
        {
            Guard.IsNotEmpty(sourceCalculations, nameof(sourceCalculations));

            string calculationPrefix = CodeGenerationDatasetTypeConstants.CalculationPrefix;

            Dictionary<string, (string CalculationId, string SourceCodeName)> calculationIdsBySourceCodeName = new Dictionary<string, (string CalculationId, string SourceCodeName)>();

            foreach (DatasetRelationshipSummary datasetRelationshipSummary in datasetRelationshipSummaries)
            {
                if(datasetRelationshipSummary.PublishedSpecificationConfiguration == null)
                {
                    continue;
                }

                calculationIdsBySourceCodeName = datasetRelationshipSummary.PublishedSpecificationConfiguration.Calculations
                    .ToDictionary(_ => $"Datasets.{GetSourceCodeName(datasetRelationshipSummary.Name)}.{calculationPrefix}_{_.TemplateId}_{_.SourceCodeName}", _ => ($"{datasetRelationshipSummary.PublishedSpecificationConfiguration.SpecificationId}-{calculationPrefix}_{_.TemplateId}", _.SourceCodeName));
            }

            return sourceCalculations.SelectMany(_ =>
            {
                IEnumerable<string> relatedCalculationNames = SourceCodeHelpers.GetReferencedReleasedDataCalculations(calculationIdsBySourceCodeName.Keys, _.Current.SourceCode);

                return relatedCalculationNames.Select(rel =>
                {
                    if (!calculationIdsBySourceCodeName.TryGetValue(rel, out (string CalculationId, string SourceCodeName) twoCalculation))
                    {
                        throw new InvalidOperationException($"Could not locate a calculation id for sourceCodeName {rel}");
                    }

                    return new CalculationRelationship
                    {
                        CalculationOneId = _.Id,
                        TargetCalculationName = twoCalculation.SourceCodeName,
                        CalculationTwoId = twoCalculation.CalculationId
                    };
                });
            }).ToList();
        }

        public IEnumerable<CalculationEnumRelationship> DetermineRelationshipsBetweenCalculationsAndEnums(IEnumerable<Calculation> calculations)
        {
            Guard.IsNotEmpty(calculations, nameof(calculations));

            IEnumerable<string> enums = calculations.Where(_ => _.Current.DataType == CalculationDataType.Enum).Select(_ => $"{_.Current.SourceCodeName}Options").Distinct();

            IEnumerable<CalculationEnumRelationship> calculationEnumRelationships = new CalculationEnumRelationship[0];

            foreach (Calculation calculation in calculations)
            {
                calculationEnumRelationships = calculationEnumRelationships.Concat(enums.SelectMany(_ => {
                    MatchCollection enumMatches = Regex.Matches(calculation.Current.SourceCode, $"({_})\\.(.*?(?=\\s\\w|$))", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    return enumMatches.Where(_ => _.Groups.Count > 0).Select(match => new CalculationEnumRelationship
                        {
                            Enum = new Models.Graph.Enum
                            {
                                SpecificationId = calculation.SpecificationId,
                                EnumName = match.Groups[1].Value,
                                EnumValue = match.Groups[2].Value,
                                FundingStreamId = calculation.FundingStreamId
                            },
                            Calculation = new Models.Graph.Calculation
                            {
                                SpecificationId = calculation.SpecificationId,
                                CalculationId = calculation.Id,
                                FundingStream = calculation.FundingStreamId,
                                CalculationName = calculation.Name,
                                CalculationType = calculation.Current.CalculationType == Models.Calcs.CalculationType.Additional ?
                                                                                        Models.Graph.CalculationType.Additional :
                                                                                        Models.Graph.CalculationType.Template
                            }

                        });
                }));
            }

            return calculationEnumRelationships;
        }

        public IEnumerable<FundingLineCalculationRelationship> DetermineRelationshipsBetweenFundingLinesAndCalculations(
            Func<string, string> GetSourceCodeName, 
            IEnumerable<Calculation> calculations, 
            IDictionary<string, Funding> fundingLines)
        {
            Guard.IsNotEmpty(fundingLines, nameof(fundingLines));

            IEnumerable<FundingLineCalculationRelationship> relationships = Array.Empty<FundingLineCalculationRelationship>();

            foreach (Funding funding in fundingLines.Values)
            {
                Dictionary<string, (FundingLine, IEnumerable<string>)> fundingLineIdsBySourceCodeName = Enumerable.DistinctBy(funding.FundingLines,
                    _ => $"{_.Namespace}.FundingLines.{_.SourceCodeName}")
                    .ToDictionary(_ => $"{GetSourceCodeName(_.Namespace)}.FundingLines.{_.SourceCodeName}", _ => (_ , _.Calculations.Select(calc => funding.Mappings[calc.Id])));
                string[] fundingLineNames = fundingLines.SelectMany(_ => _.Value.FundingLines).Select(_ => $"{GetSourceCodeName(_.Namespace)}.FundingLines.{_.SourceCodeName}")
                    .ToArray();

                relationships = relationships.Concat(calculations.SelectMany(_ =>
                {
                    IEnumerable<string> relatedCalculationNames = SourceCodeHelpers.GetReferencedCalculations(fundingLineNames, _.Current.SourceCode);

                    return relatedCalculationNames.SelectMany(rel => fundingLineIdsBySourceCodeName.TryGetValue(rel, out (FundingLine FundingLine, IEnumerable<string> Calculations) funding) ?
                        funding.Calculations.Select(calc => new FundingLineCalculationRelationship
                        {
                            CalculationOneId = _.Id,
                            FundingLine = new GraphFundingLine { SpecificationId = _.SpecificationId, FundingLineId = $"{funding.FundingLine.Namespace}_{funding.FundingLine.Id}", FundingLineName = funding.FundingLine.Name},
                            CalculationTwoId = calc
                        }) : throw new InvalidOperationException($"Could not locate a funding line id for sourceCodeName {rel}"));
                }));
            }

            return relationships.ToList();
        }

        public IEnumerable<FundingLineCalculationRelationship> DetermineRelationshipsBetweenReleasedDataFundingLinesAndCalculations(
            Func<string, string> GetSourceCodeName, 
            IEnumerable<Calculation> sourceCalculations,
            IEnumerable<DatasetRelationshipSummary> datasetRelationshipSummaries)
        {
            string fundingLinePrefix = CodeGenerationDatasetTypeConstants.FundingLinePrefix;

            IEnumerable<FundingLineCalculationRelationship> relationships = Array.Empty<FundingLineCalculationRelationship>();
            List<FundingLineReleasedDataRelationshipSummary> fundingLineRelationshipSummaries = new List<FundingLineReleasedDataRelationshipSummary>();

            foreach (DatasetRelationshipSummary datasetRelationshipSummary in datasetRelationshipSummaries)
            {
                if (datasetRelationshipSummary.PublishedSpecificationConfiguration == null)
                {
                    continue;
                }

                datasetRelationshipSummary.PublishedSpecificationConfiguration.FundingLines
                    .ForEach(_ => fundingLineRelationshipSummaries.Add(new FundingLineReleasedDataRelationshipSummary
                    {
                        FundingLineReferenceSourceCode = $"Datasets.{GetSourceCodeName(datasetRelationshipSummary.Name)}.{fundingLinePrefix}_{_.TemplateId}_{_.SourceCodeName}",
                        FundingLineId = $"{datasetRelationshipSummary.PublishedSpecificationConfiguration.SpecificationId}-{fundingLinePrefix}_{_.TemplateId}",
                        FundingLineName = _.Name
                    }));
            }

            return sourceCalculations.SelectMany(_ =>
            {
                IEnumerable<string> relatedCalculationNames = SourceCodeHelpers.GetReferencedReleasedDataCalculations(
                    fundingLineRelationshipSummaries.Select(s => s.FundingLineReferenceSourceCode),
                    _.Current.SourceCode);

                return relatedCalculationNames.Select(rel => new FundingLineCalculationRelationship
                {
                    CalculationOneId = _.Id,
                    FundingLine = new GraphFundingLine { 
                        SpecificationId = _.SpecificationId,
                        FundingLineId = fundingLineRelationshipSummaries.SingleOrDefault(s => s.FundingLineReferenceSourceCode == rel).FundingLineId,
                        FundingLineName = fundingLineRelationshipSummaries.SingleOrDefault(s => s.FundingLineReferenceSourceCode == rel).FundingLineName
                    },
                    CalculationTwoId = null
                });
            }).ToList();
        }

        public IEnumerable<Calculation> GetReferencedCalculations(string @namespace, string sourceCode, IEnumerable<Calculation> calculations)
        {
            Guard.IsNullOrWhiteSpace(@namespace, nameof(@namespace));

            if (string.IsNullOrWhiteSpace(sourceCode) || calculations == null || !calculations.Any())
            {
                return Enumerable.Empty<Calculation>();
            }

            SyntaxTree calculationSyntaxTree = VisualBasicSyntaxTree.ParseText(sourceCode);
            SyntaxNode root = calculationSyntaxTree.GetRoot();

            SyntaxNode[] invocationsToCheck = root
                .DescendantNodes()
                .Where(_ => _.Parent is InvocationExpressionSyntax && !(_ is ArgumentListSyntax))
                .ToArray();

            List<Calculation> referencedCalculations = new List<Calculation>();
            foreach(Calculation calculation in calculations)
            {
                if(invocationsToCheck.Any(_ => HasReference(_, @namespace, calculation.Namespace, calculation.Current.SourceCodeName)))
                {
                    referencedCalculations.Add(calculation);
                }
            }

            return referencedCalculations;
        }

        private const string FundingLinesNamespace = "FundingLines";

        private bool HasReference(SyntaxNode statementSyntax,
            string defaultNamespace,
            string calculationNamespace,
            string calculationName)
        {
            IEnumerable<string> texts = (statementSyntax is MemberAccessExpressionSyntax) ? statementSyntax.DescendantNodes().Select(_ => _.GetText().ToString().Trim()) :
                                        (statementSyntax is IdentifierNameSyntax) ? new[] { defaultNamespace, statementSyntax.GetText().ToString().Trim() } :
                                        new[] { string.Empty };

            // if this is a funding line and we haven't passed the funding line namespace in then we need to filter it out
            if (calculationNamespace != FundingLinesNamespace && texts.Any(_ => _.Equals(FundingLinesNamespace, StringComparison.InvariantCultureIgnoreCase)))
            {
                return false;
            }

            return texts.Any(_ => _.Equals(calculationNamespace, StringComparison.InvariantCultureIgnoreCase)) &&
                   texts.Any(_ => _.Equals(calculationName, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}