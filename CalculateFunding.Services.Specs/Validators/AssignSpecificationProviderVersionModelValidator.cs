using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Specs.Interfaces;
using FluentValidation;
using Polly;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Specs.Validators
{
    public class AssignSpecificationProviderVersionModelValidator : AbstractValidator<AssignSpecificationProviderVersionModel>
    {
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly IProvidersApiClient _providersApiClient;
        private readonly AsyncPolicy _providersApiClientPolicy;

        public AssignSpecificationProviderVersionModelValidator(
            ISpecificationsRepository specificationsRepository,
            IProvidersApiClient providersApiClient,
            ISpecificationsResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));
            Guard.ArgumentNotNull(providersApiClient, nameof(providersApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.ProvidersApiClient, nameof(resiliencePolicies.ProvidersApiClient));

            _specificationsRepository = specificationsRepository;
            _providersApiClient = providersApiClient;
            _providersApiClientPolicy = resiliencePolicies.ProvidersApiClient;

            RuleFor(model => model.SpecificationId)
                .NotEmpty()
                .WithMessage("Null or Empty SpecificationId provided")
                .CustomAsync(async (name, context, cancellationToken) =>
                {
                    AssignSpecificationProviderVersionModel model = context.ParentContext.InstanceToValidate as AssignSpecificationProviderVersionModel;
                    if (!string.IsNullOrWhiteSpace(model.SpecificationId))
                    {
                        Specification specification = await _specificationsRepository.GetSpecificationById(model.SpecificationId);
                        if(specification == null)
                        {
                            context.AddFailure(nameof(model.SpecificationId), $"Specification not found for SpecificationId - {model.SpecificationId}");
                        }
                        else if(specification.Current.ProviderSource != ProviderSource.FDZ)
                        {
                            context.AddFailure($"Specification ProviderSource is not set to FDZ");
                        }
                    }
                });

            RuleFor(model => model.ProviderVersionId)
                .NotEmpty()
                .WithMessage("Null or Empty ProviderVersionId provided")
                .CustomAsync(async (name, context, cancellationToken) =>
                {
                    AssignSpecificationProviderVersionModel model = context.ParentContext.InstanceToValidate as AssignSpecificationProviderVersionModel;
                    if (!string.IsNullOrWhiteSpace(model.ProviderVersionId))
                    {
                        HttpStatusCode providerVersionStatusCode = await _providersApiClientPolicy.ExecuteAsync(() => _providersApiClient.DoesProviderVersionExist(model.ProviderVersionId));
                        if (providerVersionStatusCode == HttpStatusCode.NotFound)
                        {
                            context.AddFailure(nameof(model.ProviderVersionId), $"Provider version id specified does not exist");
                        }
                    }
                });
        }
    }
}
