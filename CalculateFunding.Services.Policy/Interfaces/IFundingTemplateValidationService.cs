using CalculateFunding.Services.Policy.Models;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Policy.Interfaces
{
    public interface IFundingTemplateValidationService
    {
        Task<FundingTemplateValidationResult> ValidateFundingTemplate(string fundingTemplate);
    }
}
