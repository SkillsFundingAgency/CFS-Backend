using System.Linq;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Providers.ViewModels;
using FluentValidation;

namespace CalculateFunding.Services.Providers.Validators
{
    public class UploadProviderVersionValidator : AbstractValidator<ProviderVersionViewModel>
    {
        private const string messageSuffix = " provided to UploadProviderVersion";

        public UploadProviderVersionValidator()
        {
            RuleFor(model => model.ProviderVersionId)
               .NotEmpty()
               .WithMessage($"No provider version Id was{messageSuffix}");

            RuleFor(model => model.Description)
                .NotEmpty()
                .WithMessage($"No provider description was{messageSuffix}");

            RuleFor(model => model.Providers)
               .NotEmpty()
               .WithMessage($"No providers were{messageSuffix}");

            RuleFor(model => model.Name)
               .NotEmpty()
               .WithMessage($"No provider name was{messageSuffix}");

            RuleFor(model => model.VersionType)
                .NotEqual(ProviderVersionType.Missing)
                .WithMessage($"No provider version type{messageSuffix}");

            RuleFor(model => model.TargetDate)
                .NotEmpty()
                .WithMessage($"No target date{messageSuffix}");

            RuleFor(model => model.FundingStream)
                .NotEmpty()
                .When(model => model.ProviderVersionTypeString == ProviderVersionType.Custom.ToString())
                .WithMessage($"No funding stream{messageSuffix} with a custom provider version");

            RuleFor(model => model.Version)
                .GreaterThan(0)
                .WithMessage("Version number must be greater than zero");

            RuleFor(model => model.Providers)
               .Custom((name, context) =>
               {
                   ProviderVersionViewModel providerVersionModel = context.ParentContext.InstanceToValidate as ProviderVersionViewModel;

                   Provider providerWithEmptyUKPRN = providerVersionModel.Providers.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.UKPRN));
                   if (providerWithEmptyUKPRN != null)
                   {
                       context.AddFailure($"No UKPRN specified for '{providerWithEmptyUKPRN.Name}' was{messageSuffix}");
                   }

                   Provider providerWithEmptyLACode = providerVersionModel.Providers.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.LACode));

                   if (providerWithEmptyLACode != null)
                   {
                       context.AddFailure($"No LACode specified for '{providerWithEmptyLACode.Name}' was{messageSuffix}");
                   }

                   Provider providerWithEmptyEstablishmentName = providerVersionModel.Providers.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.Name));

                   if (providerWithEmptyEstablishmentName != null)
                   {
                       context.AddFailure($"No establishment name specified for '{providerWithEmptyEstablishmentName.Name}' was{messageSuffix}");
                   }

                   IGrouping<string, Provider> groupedUKPRNDuplicates = providerVersionModel.Providers.GroupBy(x => x.UKPRN).FirstOrDefault(g => g.Count() > 1);

                   if (groupedUKPRNDuplicates != null)
                   {
                       context.AddFailure($"Duplicate UKPRN specified for {groupedUKPRNDuplicates.Key} was{messageSuffix}");
                   }

                   Provider providerWithEmptyStatus = providerVersionModel.Providers.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.Status));

                   if (providerWithEmptyStatus != null)
                   {
                       context.AddFailure($"No status specified for '{providerWithEmptyStatus.Name}' was{messageSuffix}");
                   }

                   Provider providerWithEmptyTrustStatus = providerVersionModel.Providers.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.TrustStatusViewModelString));

                   if (providerWithEmptyTrustStatus != null)
                   {
                       context.AddFailure($"No trust status specified for '{providerWithEmptyTrustStatus.Name}' was{messageSuffix}");
                   }
               });
        }
    }
}
