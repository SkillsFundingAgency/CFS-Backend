using CalculateFunding.Services.Policy.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface IFundingTemplateValidationService
    {
        Task<FundingTemplateValidationResult> ValidateFundingTemplate(string fundingTemplate);
    }
}
