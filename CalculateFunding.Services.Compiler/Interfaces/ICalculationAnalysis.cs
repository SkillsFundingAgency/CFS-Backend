using System;
using System.Collections.Generic;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Graph;
using Calculation = CalculateFunding.Models.Calcs.Calculation;

namespace CalculateFunding.Services.Compiler.Interfaces
{
    public interface ICalculationAnalysis
    {
        IEnumerable<CalculationRelationship> DetermineRelationshipsBetweenCalculations(
            Func<string, string> GetSourceCodeName, 
            IEnumerable<Calculation> calculations);
        
        IEnumerable<CalculationRelationship> DetermineRelationshipsBetweenReleasedDataCalculations(
            Func<string, string> GetSourceCodeName,
            IEnumerable<Calculation> sourceCalculations,
            IEnumerable<DatasetRelationshipSummary> datasetRelationshipSummaries,
            IEnumerable<TemplateMapping> templateMappings);
        
        IEnumerable<FundingLineCalculationRelationship> DetermineRelationshipsBetweenFundingLinesAndCalculations(
            Func<string, string> GetSourceCodeName, 
            IEnumerable<Calculation> calculations, 
            IDictionary<string, Funding> fundingLines);

        IEnumerable<FundingLineCalculationRelationship> DetermineRelationshipsBetweenReleasedDataFundingLinesAndCalculations(
            Func<string, string> GetSourceCodeName,
            IEnumerable<Calculation> sourceCalculations,
            IEnumerable<DatasetRelationshipSummary> datasetRelationshipSummaries);

        IEnumerable<CalculationEnumRelationship> DetermineRelationshipsBetweenCalculationsAndEnums(IEnumerable<Calculation> calculations);
    }
}