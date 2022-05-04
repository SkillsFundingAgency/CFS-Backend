using System;
using CalculateFunding.Models.Publishing;
using System.Collections.Generic;
using TemplateFundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IInformationLinesAggregationService
    {
        void AggregateFundingLines(string specificationId, string providerId, IEnumerable<FundingLine> fundingLines, IEnumerable<TemplateFundingLine> templateFundingLines);
    }
}
