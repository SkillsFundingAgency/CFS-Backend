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
        public IEnumerable<CalculationRelationship> DetermineRelationshipsBetweenCalculations(IEnumerable<Calculation> calculations)
        {
            Guard.IsNotEmpty(calculations, nameof(calculations));
            
            Dictionary<string, string> calculationIdsBySourceCodeName = calculations
                .ToDictionary(_ => $"{_.Namespace}.{_.Current.SourceCodeName}", _ => _.Id);
            string[] calculationNames = calculations.Select(_ => $"{_.Namespace}.{_.Current.SourceCodeName}")
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

        public IEnumerable<CalculationEnumRelationship> DetermineRelationshipsBetweenCalculationsAndEnums(IEnumerable<Calculation> calculations)
        {
            Guard.IsNotEmpty(calculations, nameof(calculations));

            IEnumerable<string> enums = calculations.Where(_ => _.Current.DataType == CalculationDataType.Enum).Select(_ => $"{_.Current.SourceCodeName}Options").Distinct();

            IEnumerable<CalculationEnumRelationship> calculationEnumRelationships = new CalculationEnumRelationship[0];

            foreach (Calculation calculation in calculations)
            {
                calculationEnumRelationships = calculationEnumRelationships.Concat(enums.SelectMany(_ => {
                    MatchCollection enumMatches = Regex.Matches(calculation.Current.SourceCode, $"({_}).(.*?(?=\\s\\w|$))", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    return enumMatches.Where(_ => _.Groups.Count > 0).Select(match => new CalculationEnumRelationship
                    {
                        Enum = new Models.Graph.Enum
                        {
                            EnumName = match.Groups[1].Value,
                            EnumValue = match.Groups[2].Value,
                            FundingStreamId = calculation.FundingStreamId,
                            SpecificationId = calculation.SpecificationId
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

        public IEnumerable<FundingLineCalculationRelationship> DetermineRelationshipsBetweenFundingLinesAndCalculations(IEnumerable<Calculation> calculations, IDictionary<string, Funding> fundingLines)
        {
            Guard.IsNotEmpty(fundingLines, nameof(fundingLines));

            IEnumerable<FundingLineCalculationRelationship> relationships = new FundingLineCalculationRelationship[0];

            foreach (Funding funding in fundingLines.Values)
            {
                Dictionary<string, (FundingLine, IEnumerable<string>)> fundingLineIdsBySourceCodeName = funding.FundingLines
                    .DistinctBy(_ => $"{_.Namespace}.FundingLines.{_.SourceCodeName}")
                    .ToDictionary(_ => $"{_.Namespace}.FundingLines.{_.SourceCodeName}", _ => (_ , _.Calculations.Select(calc => funding.Mappings[calc.Id])));
                string[] fundingLineNames = fundingLines.SelectMany(_ => _.Value.FundingLines).Select(_ => $"{_.Namespace}.FundingLines.{_.SourceCodeName}")
                    .ToArray();

                relationships = relationships.Concat(calculations.SelectMany(_ =>
                {
                    IEnumerable<string> relatedCalculationNames = SourceCodeHelpers.GetReferencedCalculations(fundingLineNames, _.Current.SourceCode);

                    return relatedCalculationNames.SelectMany(rel => fundingLineIdsBySourceCodeName.TryGetValue(rel, out (FundingLine FundingLine, IEnumerable<string> Calculations) funding) ?
                        funding.Calculations.Select(calc => new FundingLineCalculationRelationship
                        {
                            CalculationOneId = _.Id,
                            FundingLine = new GraphFundingLine { FundingLineId = $"{funding.FundingLine.Namespace}_{funding.FundingLine.Id}", FundingLineName = funding.FundingLine.Name},
                            CalculationTwoId = calc
                        }) : throw new InvalidOperationException($"Could not locate a funding line id for sourceCodeName {rel}"));
                }));
            }

            return relationships.ToList();
        }
    }
}