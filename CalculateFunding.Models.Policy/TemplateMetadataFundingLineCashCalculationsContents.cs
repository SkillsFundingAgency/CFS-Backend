using System.Collections.Generic;

namespace CalculateFunding.Models.Policy
{
    public class TemplateMetadataFundingLineCashCalculationsContents
    {
        public string FundingStreamId { get; set; }
        public string FundingPeriodId { get; set; }
        public string TemplateVersion { get; set; }
        public IEnumerable<TemplateMetadataFundingLine> FundingLines { get; set; }
        public IDictionary<string, IEnumerable<TemplateMetadataCalculation>> CashCalculations { get; set; }
        public string SchemaVersion { get; set; }
    }
}
