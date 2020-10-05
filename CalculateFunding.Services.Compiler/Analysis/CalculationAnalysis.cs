using System;
using System.Collections.Generic;
using System.Linq;
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

        public IEnumerable<FundingLineCalculationRelationship> DetermineRelationshipsBetweenFundingLinesAndCalculations(IEnumerable<Calculation> calculations, IDictionary<string, Funding> fundingLines)
        {
            Guard.IsNotEmpty(fundingLines, nameof(fundingLines));

            IEnumerable<FundingLineCalculationRelationship> relationships = new FundingLineCalculationRelationship[0];

            foreach (Funding funding in fundingLines.Values)
            {
                Dictionary<string, (FundingLine, IEnumerable<string>)> fundingLineIdsBySourceCodeName = funding.FundingLines
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

            return relationships;
        }
    }
}