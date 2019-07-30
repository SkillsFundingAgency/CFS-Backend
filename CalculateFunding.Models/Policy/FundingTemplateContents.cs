namespace CalculateFunding.Models.Policy
{
    public class FundingTemplateContents : FundingTemplate
    {
        /// <summary>
        /// The raw configuration of the template, eg JSON file which is uploaded as the template
        /// </summary>
        public string TemplateFileContents { get; set; }

        public string SchemaVersion { get; set; }
    }
}
