using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentValidation;
using FluentValidation.Validators;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Validators
{
    public class UpdateDefinitionSpecificationRelationshipModelValidator : AbstractValidator<UpdateDefinitionSpecificationRelationshipModel>
    {
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly Polly.AsyncPolicy _specificationsApiClientPolicy;
        private readonly Polly.AsyncPolicy _policiesApiClientPolicy;

        public UpdateDefinitionSpecificationRelationshipModelValidator(
            IPoliciesApiClient policiesApiClient,
            ISpecificationsApiClient specificationsApiClient,
            IDatasetsResiliencePolicies datasetsResiliencePolicies)
        {
            _policiesApiClient = policiesApiClient;
            _specificationsApiClient = specificationsApiClient;
            _specificationsApiClientPolicy = datasetsResiliencePolicies.SpecificationsApiClient;
            _policiesApiClientPolicy = datasetsResiliencePolicies.PoliciesApiClient;

            RuleFor(model => model.Description)
              .NotEmpty()
              .WithMessage("Missing description");

            RuleFor(model => model.SpecificationId)
              .NotEmpty()
              .WithMessage("Missing specification id");

            RuleFor(model => model.FundingLineIds)
              .CustomAsync(async (name, context, ct) =>
              {
                  UpdateDefinitionSpecificationRelationshipModel relationshipModel = context.ParentContext.InstanceToValidate as UpdateDefinitionSpecificationRelationshipModel;
                  if (relationshipModel.FundingLineIds.IsNullOrEmpty() && relationshipModel.CalculationIds.IsNullOrEmpty())
                  {
                      context.AddFailure($"At least one funding line or calculation must be provided for the ReleasedData relationship type");
                  }
                  else
                  {
                      await ValidateFundingLinesAndCalculationsForReleasedDataRelationship(relationshipModel, context);
                  }
              });
        }

        private async Task ValidateFundingLinesAndCalculationsForReleasedDataRelationship(UpdateDefinitionSpecificationRelationshipModel relationshipModel, CustomContext context)
        {
            if (relationshipModel.FundingLineIds.IsNullOrEmpty() && relationshipModel.CalculationIds.IsNullOrEmpty()) return;

            ApiResponse<SpecificationSummary> specificationSummaryApiResponse =
               await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(relationshipModel.SpecificationId));

            if (!specificationSummaryApiResponse.StatusCode.IsSuccess() && specificationSummaryApiResponse.StatusCode != HttpStatusCode.NotFound)
            {
                string errorMessage = $"Failed to fetch specification summary for specification ID: {relationshipModel.SpecificationId} with StatusCode={specificationSummaryApiResponse.StatusCode}";
                throw new RetriableException(errorMessage);
            }

            if(specificationSummaryApiResponse.StatusCode == HttpStatusCode.NotFound || specificationSummaryApiResponse.Content == null)
            {
                context.AddFailure($"Specification - {relationshipModel.SpecificationId} not found");
                return;
            }

            SpecificationSummary specificationSummary = specificationSummaryApiResponse.Content;
            string fundingStreamId = specificationSummary.FundingStreams.First().Id;
            string fundingPeriodId = specificationSummary.FundingPeriod.Id;
            string templateId = specificationSummary.TemplateIds.First(x => x.Key == fundingStreamId).Value;

            ApiResponse<TemplateMetadataDistinctContents> metadataResponse =
                    await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetDistinctTemplateMetadataContents(fundingStreamId, fundingPeriodId, templateId));

            if (!metadataResponse.StatusCode.IsSuccess() && metadataResponse.StatusCode != HttpStatusCode.NotFound)
            {
                string errorMessage = $"Failed to fetch template metadata for FundingStreamId={fundingStreamId}, FundingPeriodId={fundingPeriodId} and TemplateId={templateId} with StatusCode={metadataResponse.StatusCode}";
                throw new RetriableException(errorMessage);
            }

            if (metadataResponse.StatusCode == HttpStatusCode.NotFound || metadataResponse.Content == null)
            {
                context.AddFailure($"Template metadata for fundingstream - {fundingStreamId}, fundingPeriodId - {fundingPeriodId} and templateId - {templateId} not found.");
                return;
            }

            TemplateMetadataDistinctContents metadata = metadataResponse.Content;

            if (!relationshipModel.FundingLineIds.IsNullOrEmpty())
            {
                var metadataFundingLineIds = (metadata.FundingLines ?? Enumerable.Empty<TemplateMetadataFundingLine>()).Select(x => x.TemplateLineId).Distinct().ToList();
                var missingFundingLineIds = relationshipModel.FundingLineIds.Distinct().Where(x => !metadataFundingLineIds.Contains(x)).ToList();

                if (missingFundingLineIds.Any())
                {
                    context.AddFailure($"The following funding lines not found in the metadata for fundingStream - {fundingStreamId}, fundingperiod - {fundingPeriodId} and template id - {templateId}: {string.Join(",", missingFundingLineIds)}");
                }
            }

            if (!relationshipModel.CalculationIds.IsNullOrEmpty())
            {
                var metadataCalculationIds = (metadata.Calculations ?? Enumerable.Empty<TemplateMetadataCalculation>()).Select(x => x.TemplateCalculationId).Distinct().ToList();
                var missingCalculationIds = relationshipModel.CalculationIds.Distinct().Where(x => !metadataCalculationIds.Contains(x)).ToList();

                if (missingCalculationIds.Any())
                {
                    context.AddFailure($"The following calculations not found in the metadata for fundingStream - {fundingStreamId}, fundingperiod - {fundingPeriodId} and template id - {templateId}: {string.Join(",", missingCalculationIds)}");
                }
            }
        }
    }
}
