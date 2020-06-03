using FluentValidation.Results;

namespace CalculateFunding.Services.Policy.Models
{
    public class FundingTemplateValidationResult : ValidationResult
    {
        public FundingTemplateValidationResult()
        {
        }

        public string TemplateVersion { get; set; }

        public string FundingStreamId { get; set; }

        public string FundingPeriodId { get; set; }

        public string SchemaVersion { get; set; }
    }
}
