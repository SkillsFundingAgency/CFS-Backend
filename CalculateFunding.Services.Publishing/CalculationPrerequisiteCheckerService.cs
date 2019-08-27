using System;
using System.Collections.Generic;
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
        private readonly Policy _policy;

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
            
            ApiResponse<IEnumerable<CalculationMetadata>> calculationsResponse = await _policy.ExecuteAsync(() =>  _calcsApiClient.GetCalculations(specificationId));

            if (calculationsResponse?.Content == null)
            {
                LogErrorAndThrow($"Did locate any calculation metadata for specification {specificationId}. Unable to complete prerequisite checks");   
            }
            
            validationErrors.AddRange(calculationsResponse?.Content.Where(_ => _.PublishStatus != PublishStatus.Approved)
                .Select(_ => $"Calculation {_.Name} must be approved but is {_.PublishStatus}"));

            // TOOO: Check all template calculations are mapped to calculations

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

        private void LogErrorAndThrow(string message)
        {
            _logger.Error(message);
            
            throw new Exception(message);
        }
    }
}
