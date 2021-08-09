using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs.ObsoleteItems;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using FluentValidation;
using Polly;
using System.Linq;

namespace CalculateFunding.Services.Calcs.Validators
{
    public class ObsoleteItemValidator : AbstractValidator<ObsoleteItem>
    {
        private readonly ICalculationsRepository _calculationRepository;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly AsyncPolicy _specificationsApiClientPolicy;

        public ObsoleteItemValidator(
            ICalculationsRepository calculationRepository,
            ISpecificationsApiClient specificationsApiClient,
            ICalcsResiliencePolicies calcsResiliencePolicies)
        {
            Guard.ArgumentNotNull(calculationRepository, nameof(calculationRepository));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(calcsResiliencePolicies, nameof(calcsResiliencePolicies));
            Guard.ArgumentNotNull(calcsResiliencePolicies?.SpecificationsApiClient, nameof(calcsResiliencePolicies.SpecificationsApiClient));

            _calculationRepository = calculationRepository;
            _specificationsApiClient = specificationsApiClient;
            _specificationsApiClientPolicy = calcsResiliencePolicies.SpecificationsApiClient;

            CascadeMode = CascadeMode.StopOnFirstFailure;

            RuleFor(model => model.Id)
             .NotEmpty()
             .WithMessage("Null or empty obsolete item id provided.");

            RuleFor(model => model.SpecificationId)
              .NotEmpty()
              .WithMessage("Null or empty specification id provided.");

            RuleFor(model => model.CalculationIds)
             .Custom((calculationIds, context) => {
                 ObsoleteItem obsoleteItem = context.ParentContext.InstanceToValidate as ObsoleteItem;

                 if (!obsoleteItem.IsReleasedData)
                 {
                     if (calculationIds.IsNullOrEmpty())
                     {
                         context.AddFailure("Atleast one calculation id must be provided.");
                     }
                 }
             });

            RuleFor(model => model.DatasetRelationshipId)
             .Custom((datasetRelationshipId, context) => {
                 ObsoleteItem obsoleteItem = context.ParentContext.InstanceToValidate as ObsoleteItem;

                 if (obsoleteItem.IsReleasedData)
                 {
                     if (string.IsNullOrWhiteSpace(datasetRelationshipId))
                     {
                         context.AddFailure("Dataset relationship is required.");
                     }
                 }
             });

            RuleFor(model => model.SpecificationId)
                .CustomAsync(async (specificationId, context, ct) =>
                {
                    ApiResponse<SpecificationSummary> specificationApiResponse = await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(specificationId));

                    if (specificationApiResponse == null || !specificationApiResponse.StatusCode.IsSuccess() || specificationApiResponse.Content == null)
                    {
                        context.AddFailure("Failed to find specification for provided specification id.");
                    }
                });

            RuleFor(model => model.CalculationIds)
                .CustomAsync(async (calculationIds, context, ct) =>
                {
                    if (calculationIds == null || !calculationIds.Any())
                    {
                        return;
                    }

                    foreach (var calculationId in calculationIds)
                    {
                        Models.Calcs.Calculation calculation = await _calculationRepository.GetCalculationById(calculationId);

                        if (calculation == null)
                        {
                            context.AddFailure($"Failed to find calculation for provided calculation id - {calculationId}.");
                        }
                    }
                });
        }
    }
}
