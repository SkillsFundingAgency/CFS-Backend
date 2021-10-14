using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Compiler.Interfaces;
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
            
            Dictionary<string, string> calculationIdsBySourceCodeName = calculations
                .ToDictionary(_ => $"{GetSourceCodeName(_.Namespace)}.{_.Current.SourceCodeName}", _ => _.Id);
            string[] calculationNames = calculations.Select(_ => $"{GetSourceCodeName(_.Namespace)}.{_.Current.SourceCodeName}")
                .ToArray();

            return calculations.SelectMany(_ =>
            {
                IEnumerable<string> relatedCalculationNames = SourceCodeHelpers.GetReferencedCalculations(calculationNames, _.Current.SourceCode);

                return relatedCalculationNames.Select(rel => new CalculationRelationship
                {
                    CalculationOneId = _.Id,
                    CalculationTwoId = calculationIdsBySourceCodeName.TryGetValue(rel, out string twoId) ? 
                        twoId : 
                        throw new InvalidOperationException($"Could not locate a calculation id for sourceCodeName {rel}")
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

            HashSet<string> calculationIdsBySourceCodeName = new HashSet<string>();

            foreach (DatasetRelationshipSummary datasetRelationshipSummary in datasetRelationshipSummaries)
            {
                if(datasetRelationshipSummary.PublishedSpecificationConfiguration == null)
                {
                    continue;
                }

                calculationIdsBySourceCodeName = datasetRelationshipSummary.PublishedSpecificationConfiguration.Calculations
                    .Select(_ => $"Datasets.{GetSourceCodeName(datasetRelationshipSummary.Name)}.{calculationPrefix}_{_.TemplateId}_{_.SourceCodeName}").ToHashSet();
            }

            return sourceCalculations.SelectMany(_ =>
            {
                IEnumerable<string> relatedCalculationNames = SourceCodeHelpers.GetReferencedReleasedDataCalculations(calculationIdsBySourceCodeName, _.Current.SourceCode);

                return relatedCalculationNames.Select(rel => new CalculationRelationship
                {
                    CalculationOneId = _.Id,
                    CalculationTwoId = calculationIdsBySourceCodeName.TryGetValue(rel, out string twoId) ?
                        twoId :
                        throw new InvalidOperationException($"Could not locate a calculation id for sourceCodeName {rel}")
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
                Dictionary<string, (FundingLine, IEnumerable<string>)> fundingLineIdsBySourceCodeName = funding.FundingLines
                    .DistinctBy(_ => $"{_.Namespace}.FundingLines.{_.SourceCodeName}")
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
                        FundingLineTargetSpecificationId = datasetRelationshipSummary.PublishedSpecificationConfiguration.SpecificationId,
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
                        SpecificationId = fundingLineRelationshipSummaries.SingleOrDefault(s => s.FundingLineReferenceSourceCode == rel).FundingLineTargetSpecificationId,
                        FundingLineId = fundingLineRelationshipSummaries.SingleOrDefault(s => s.FundingLineReferenceSourceCode == rel).FundingLineReferenceSourceCode,
                        FundingLineName = fundingLineRelationshipSummaries.SingleOrDefault(s => s.FundingLineReferenceSourceCode == rel).FundingLineName
                    },
                    CalculationTwoId = null
                });
            }).ToList();
        }
    }
}