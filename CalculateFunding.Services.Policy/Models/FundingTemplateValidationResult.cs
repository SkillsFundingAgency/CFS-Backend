using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace CalculateFunding.Services.Policy.Models
{
    public class FundingTemplateValidationResult
    {
        public FundingTemplateValidationResult()
        {
            ValidationState = new ModelState();
        }

        public string Version { get; set; }

        public string FundingStreamId { get; set; }

        public ModelState ValidationState { get; set; }

        public bool IsValid => ValidationState.Errors.IsNullOrEmpty();
    }
}
