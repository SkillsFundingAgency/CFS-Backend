using CalculateFunding.Models.Providers.ViewModels;
using CalculateFunding.Services.Providers.Interfaces;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Services.Providers.Validators
{
    public class UploadProviderVersionValidator : AbstractValidator<ProviderVersionViewModel>
    {
        public UploadProviderVersionValidator()
        {
            RuleFor(model => model.Id)
               .NotEmpty()
               .WithMessage("No provider version Id was provided to UploadProviderVersion");

            RuleFor(model => model.Description)
                .NotEmpty()
                .WithMessage("No provider description was provided to UploadProviderVersion");

            RuleFor(model => model.Providers)
               .NotEmpty()
               .WithMessage("No providers were provided to UploadProviderVersion");

            RuleFor(model => model.Name)
               .NotEmpty()
               .WithMessage("No provider name was provided to UploadProviderVersion");

            RuleFor(model => model.VersionType)
                .NotEqual(ProviderVersionType.Missing)
                .WithMessage("No provider version type provided to UploadProviderVersion");

            RuleFor(model => model.Providers)
               .Custom((name, context) => {
                   ProviderVersionViewModel providerVersionModel = context.ParentContext.InstanceToValidate as ProviderVersionViewModel;

                   ProviderViewModel providerWithEmptyUKPRN = providerVersionModel.Providers.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.UKPRN));
                   if (providerWithEmptyUKPRN != null)
                   {
                       context.AddFailure($"No UKPRN specified for '{providerWithEmptyUKPRN.Name}' was provided to UploadProviderVersion");
                   }

                   ProviderViewModel providerWithEmptyLACode = providerVersionModel.Providers.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.LACode));

                   if (providerWithEmptyLACode != null)
                   {
                       context.AddFailure($"No LACode specified for '{providerWithEmptyLACode.Name}' was provided to UploadProviderVersion");
                   }

                   ProviderViewModel providerWithEmptyEstablishmentName = providerVersionModel.Providers.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.Name));

                   if (providerWithEmptyEstablishmentName != null)
                   {
                       context.AddFailure($"No establishment name specified for '{providerWithEmptyEstablishmentName.Name}' was provided to UploadProviderVersion");
                   }

                   IGrouping<string, ProviderViewModel> groupedUKPRNDuplicates = providerVersionModel.Providers.GroupBy(x => x.UKPRN).FirstOrDefault(g => g.Count() > 1);

                   if (groupedUKPRNDuplicates != null)
                   {
                       context.AddFailure($"Duplicate UKPRN specified for {groupedUKPRNDuplicates.Key} was provided to UploadProviderVersion");
                   }

                   ProviderViewModel providerWithEmptyStatus = providerVersionModel.Providers.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.Status));

                   if (providerWithEmptyStatus != null)
                   {
                       context.AddFailure($"No status specified for '{providerWithEmptyStatus.Name}' was provided to UploadProviderVersion");
                   }
               });
        }
    }
}
