using System;
using System.Collections.Generic;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing
{
    public class FundingLineTotalAggregator : IFundingLineTotalAggregator
    {
        public IEnumerable<Models.Publishing.FundingLine> GenerateTotals(TemplateMetadataContents templateMetadataContents, TemplateMapping templateMapping, IEnumerable<CalculationResult> calculationResults)
        {
            throw new NotImplementedException();
        }
    }
}
