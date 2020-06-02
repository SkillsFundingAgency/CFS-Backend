namespace CalculateFunding.Models.Policy.TemplateBuilder
{
    public class TemplateFundingLinesUpdateCommand
    {
        public string TemplateId { get; set; }
        
        public string TemplateFundingLinesJson { get; set; }
    }
}