namespace CalculateFunding.Models.Policy.TemplateBuilder
{
    public class TemplateCreateCommand
    {
        public string FundingStreamId { get; set; }

        public string FundingPeriodId { get; set; }

        public string SchemaVersion { get; set; }
        
        public string Name { get; set; }

        public string Description { get; set; }
    }
}