namespace CalculateFunding.Models.Policy.TemplateBuilder
{
    public class TemplateCreateAsCloneCommand
    {
        public string CloneFromTemplateId { get; set; }
        
        public string FundingStreamId { get; set; }

        public string FundingPeriodId { get; set; }

        public string Description { get; set; }
        
        public string Version { get; set; }
    }
}