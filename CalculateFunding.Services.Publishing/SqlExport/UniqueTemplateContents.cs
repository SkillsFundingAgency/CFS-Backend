namespace CalculateFunding.Models.Scenarios
{
    public class UniqueTemplateContents
    {
        public System.Collections.Generic.IEnumerable<Common.TemplateMetadata.Models.FundingLine> FundingLines { get; set; }
        public System.Collections.Generic.IEnumerable<Common.TemplateMetadata.Models.Calculation> Calculations { get; set; }
    }
}
