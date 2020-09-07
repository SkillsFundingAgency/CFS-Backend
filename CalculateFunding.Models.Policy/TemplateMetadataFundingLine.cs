using CalculateFunding.Common.TemplateMetadata.Enums;

namespace CalculateFunding.Models.Policy
{
    public class TemplateMetadataFundingLine
    {
        public string Name { get; set; }
        public string FundingLineCode { get; set; }
        public uint TemplateLineId { get; set; }
        public FundingLineType Type { get; set; }
    }
}
