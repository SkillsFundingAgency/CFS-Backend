using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Compiler.Interfaces;
using Calculation = CalculateFunding.Models.Calcs.Calculation;

namespace CalculateFunding.Services.Compiler.Analysis
{
    public class CalculationAnalysis : ICalculationAnalysis
    {
        public IEnumerable<CalculationRelationship> DetermineRelationshipsBetweenCalculations(IEnumerable<Calculation> calculations)
        {
            Guard.IsNotEmpty(calculations, nameof(calculations));
            
            Dictionary<string, string> calculationIdsBySourceCodeName = calculations
                .ToDictionary(_ => _.Current.SourceCodeName, _ => _.Id);
            string[] calculationNames = calculations.Select(_ => _.Current.SourceCodeName)
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
    }
}