using CalculateFunding.Services.CalcEngine.Interfaces;
using FluentValidation;

namespace CalculateFunding.Services.CalcEngine.Validators
{
    public class CalculatorResiliencePoliciesValidator : AbstractValidator<ICalculatorResiliencePolicies>
    {
        private const string NotNullErrMessage = "must not be null";
        public CalculatorResiliencePoliciesValidator()
        {
            RuleFor(m => m.CacheProvider).NotNull().WithMessage(GenerateNotNullMessage("CacheProvider Policy"));
            RuleFor(m => m.Messenger).NotNull().WithMessage(GenerateNotNullMessage("Messenger policy"));
            RuleFor(m => m.ProviderSourceDatasetsRepository).NotNull().WithMessage(GenerateNotNullMessage("ProviderSourceDatasetsRepository"));
            RuleFor(m => m.ProviderResultsRepository).NotNull().WithMessage(GenerateNotNullMessage("ProviderResultsRepository"));
            RuleFor(m => m.CalculationsRepository).NotNull().WithMessage(GenerateNotNullMessage("CalculationRepository"));
        }

        private static string GenerateNotNullMessage(string componentName)
        {
            return $"{componentName} {NotNullErrMessage}";
        }
    }
}
