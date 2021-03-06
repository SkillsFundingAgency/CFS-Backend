﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class CalculationPrerequisiteCheckerService : ICalculationPrerequisiteCheckerService
    {
        private readonly ICalculationsApiClient _calcsApiClient;
        private readonly ILogger _logger;
        private readonly AsyncPolicy _policy;

        public CalculationPrerequisiteCheckerService(ICalculationsApiClient calculationsApiClient,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(calculationsApiClient, nameof(calculationsApiClient));
            Guard.ArgumentNotNull(publishingResiliencePolicies?.CalculationsApiClient, nameof(publishingResiliencePolicies.CalculationsApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _calcsApiClient = calculationsApiClient;
            _policy = publishingResiliencePolicies.CalculationsApiClient;
            _logger = logger;
        }

        public async Task<IEnumerable<string>> VerifyCalculationPrerequisites(SpecificationSummary specification)
        {
            List<string> validationErrors = new List<string>();

            string specificationId = specification.Id;

            ApiResponse<IEnumerable<CalculationMetadata>> calculationsResponse = await _policy.ExecuteAsync(() => _calcsApiClient.GetCalculationMetadataForSpecification(specificationId));

            if (calculationsResponse?.Content == null)
            {
                string errorMessage = $"Did locate any calculation metadata for specification {specificationId}. Unable to complete prerequisite checks";

                _logger.Error(errorMessage);
                validationErrors.Add(errorMessage);

                return validationErrors;
            }

            validationErrors.AddRange(calculationsResponse?.Content.Where(_ => _.PublishStatus != PublishStatus.Approved && _.CalculationType == CalculationType.Template)
                .Select(_ => $"Calculation {_.Name} must be approved but is {_.PublishStatus}"));

            foreach (var fundingStream in specification.FundingStreams)
            {
                ApiResponse<TemplateMapping> templateMappingResponse = await _calcsApiClient.GetTemplateMapping(specificationId, fundingStream.Id);

                foreach (TemplateMappingItem calcInError in templateMappingResponse.Content.TemplateMappingItems.Where(c => string.IsNullOrWhiteSpace(c.CalculationId)))
                {
                    validationErrors.Add($"{calcInError.EntityType} {calcInError.Name} is not mapped to a calculation in CFS");
                }
            }

            return validationErrors;
        }
    }
}
