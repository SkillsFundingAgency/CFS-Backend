using FluentValidation.Results;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IProviderIdsServiceRequestValidator
    {
        ValidationResult Validate(string[] providerIds);
    }
}
