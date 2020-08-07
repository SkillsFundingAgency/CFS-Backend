using FluentValidation.Results;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderIdsServiceRequestValidator
    {
        ValidationResult Validate(string[] providerIds);
    }
}
