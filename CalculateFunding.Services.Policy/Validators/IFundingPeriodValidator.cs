using CalculateFunding.Models.Policy;
using FluentValidation.Results;

namespace CalculateFunding.Services.Providers.Validators
{
    public interface IFundingPeriodValidator
    {
        ValidationResult Validate(FundingPeriod instance);
    }
}