using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing
{
    public class CalculationPrerequisiteCheckerService : ICalculationPrerequisiteCheckerService
    {
        private readonly ICalculationsApiClient _calcsApiClient;

        public CalculationPrerequisiteCheckerService(ICalculationsApiClient calculationsApiClient, IPublishingResiliencePolicies publishingResiliencePolicies)
        {
            _calcsApiClient = calculationsApiClient;
        }

        public async Task<IEnumerable<string>> VerifyCalculationPrerequisites(SpecificationSummary specification)
        {
            List<string> validationErrors = new List<string>();
            // TODO: Check all calculations are approved
            var calcsResult = await _calcsApiClient.GetCalculations(specification.Id);
            foreach (var calc in calcsResult.Content)
            {
                // TODO: Add approval status to calculation
            }

            // TOOO: Check all template calculations are mapped to calculations

            foreach (var fundingStream in specification.FundingStreams)
            {
                var templateMappingResponse = await _calcsApiClient.GetTemplateMapping(specification.Id, fundingStream.Id);

                foreach (var calcInError in templateMappingResponse.Content.TemplateMappingItems.Where(c => string.IsNullOrWhiteSpace(c.CalculationId)))
                {
                    validationErrors.Add($"{calcInError.EntityType} {calcInError.Name} is not mapped to a calculation in CFS");
                }
            }

            return validationErrors;
            throw new NotImplementedException();
        }
    }
}
