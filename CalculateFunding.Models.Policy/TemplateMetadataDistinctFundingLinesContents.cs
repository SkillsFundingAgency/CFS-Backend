using System.Collections.Generic;

namespace CalculateFunding.Models.Policy
{
    public class TemplateMetadataDistinctFundingLinesContents
    {
        public string FundingStreamId { get; set; }
        public string FundingPeriodId { get; set; }
        public string TemplateVersion { get; set; }
        public IEnumerable<TemplateMetadataFundingLine> FundingLines { get; set; }
        public string SchemaVersion { get; set; }
    }
}
