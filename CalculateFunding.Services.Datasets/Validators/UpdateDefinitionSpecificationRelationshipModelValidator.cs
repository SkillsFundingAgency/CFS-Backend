using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentValidation;
using FluentValidation.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Validators
{
    public class UpdateDefinitionSpecificationRelationshipModelValidator : AbstractValidator<UpdateDefinitionSpecificationRelationshipModel>
    {
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly ICalcsRepository _calcsRepository;
        private readonly IDatasetRepository _datasetRepository;
        private readonly Polly.AsyncPolicy _specificationsApiClientPolicy;
        private readonly Polly.AsyncPolicy _policiesApiClientPolicy;

        public UpdateDefinitionSpecificationRelationshipModelValidator(
            IPoliciesApiClient policiesApiClient,
            ISpecificationsApiClient specificationsApiClient,
            ICalcsRepository calcsRepository,
            IDatasetRepository datasetRepository,
            IDatasetsResiliencePolicies datasetsResiliencePolicies)
        {
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(calcsRepository, nameof(calcsRepository));
            Guard.ArgumentNotNull(datasetRepository, nameof(datasetRepository));
            Guard.ArgumentNotNull(datasetsResiliencePolicies?.CalculationsApiClient, nameof(datasetsResiliencePolicies.CalculationsApiClient));
            Guard.ArgumentNotNull(datasetsResiliencePolicies?.PoliciesApiClient, nameof(datasetsResiliencePolicies.PoliciesApiClient));

            _policiesApiClient = policiesApiClient;
            _specificationsApiClient = specificationsApiClient;
            _specificationsApiClientPolicy = datasetsResiliencePolicies.SpecificationsApiClient;
            _policiesApiClientPolicy = datasetsResiliencePolicies.PoliciesApiClient;
            _calcsRepository = calcsRepository;
            _datasetRepository = datasetRepository;

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
                      context.AddFailure("At least one funding line or calculation must be provided for the ReleasedData relationship type");
                  }
                  else
                  {
                      await ValidateFundingLinesAndCalculationsForReleasedDataRelationship(relationshipModel, context);
                      await ValidateFundingLinesAndCalculationsRemoval(relationshipModel, context);
                  }
              });
        }
        private async Task ValidateFundingLinesAndCalculationsRemoval(UpdateDefinitionSpecificationRelationshipModel model, CustomContext context)
        {
            DefinitionSpecificationRelationship definitionSpecificationRelationship = await _datasetRepository.GetDefinitionSpecificationRelationshipById(model.RelationshipId);

            if (definitionSpecificationRelationship == null || definitionSpecificationRelationship.Current?.RelationshipType != DatasetRelationshipType.ReleasedData)
            {
                return;
            }

            IEnumerable<PublishedSpecificationItem> itemsToRemove = new PublishedSpecificationItem[0];

            itemsToRemove = itemsToRemove.Concat(definitionSpecificationRelationship.Current?.PublishedSpecificationConfiguration?.FundingLines?.Where(_ => !model.FundingLineIds.Contains(_.TemplateId)) ?? new PublishedSpecificationItem[0]);

            itemsToRemove = itemsToRemove.Concat(definitionSpecificationRelationship.Current?.PublishedSpecificationConfiguration?.Calculations?.Where(_ => !model.CalculationIds.Contains(_.TemplateId)) ?? new PublishedSpecificationItem[0]);

            IEnumerable<CalculationResponseModel> calculationResponseModels = await _calcsRepository.GetCurrentCalculationsBySpecificationId(model.SpecificationId);

            IEnumerable<PublishedSpecificationItem> referencedItemsToRemove = itemsToRemove.Where(_ => calculationResponseModels.Any(calc => calc.SourceCode.IndexOf(_.SourceCodeName, StringComparison.InvariantCultureIgnoreCase) >= 0));

            if (referencedItemsToRemove.Any())
            {
                foreach (PublishedSpecificationItem publishedSpecificationItem in referencedItemsToRemove)
                {
                    context.AddFailure($"Unable to remove {publishedSpecificationItem.Name} as it is in use.");
                }
            }
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
