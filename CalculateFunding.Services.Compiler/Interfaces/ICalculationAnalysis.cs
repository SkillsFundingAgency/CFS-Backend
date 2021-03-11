using System.Collections.Generic;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Graph;
using Calculation = CalculateFunding.Models.Calcs.Calculation;

namespace CalculateFunding.Services.Compiler.Interfaces
{
    public interface ICalculationAnalysis
    {
        IEnumerable<CalculationRelationship> DetermineRelationshipsBetweenCalculations(IEnumerable<Calculation> calculations);
        IEnumerable<FundingLineCalculationRelationship> DetermineRelationshipsBetweenFundingLinesAndCalculations(IEnumerable<Calculation> calculations, IDictionary<string, Funding> fundingLines);

        IEnumerable<CalculationEnumRelationship> DetermineRelationshipsBetweenCalculationsAndEnums(IEnumerable<Calculation> calculations);
    }
}