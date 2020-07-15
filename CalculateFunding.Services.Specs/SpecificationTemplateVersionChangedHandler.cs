using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Specs.Interfaces;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Specs
{
    public class SpecificationTemplateVersionChangedHandler : ISpecificationTemplateVersionChangedHandler
    {
        private const string AssignTemplateCalculationsJob = JobConstants.DefinitionNames.AssignTemplateCalculationsJob;
        
        private readonly IJobManagement _jobs;
        private readonly ICalculationsApiClient _calculations;
        private readonly ILogger _logger;
        private readonly AsyncPolicy _calculationsPolicy;

        public SpecificationTemplateVersionChangedHandler(IJobManagement jobs,
            ICalculationsApiClient calculations,
            ISpecificationsResiliencePolicies resiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(jobs, nameof(jobs));
            Guard.ArgumentNotNull(calculations, nameof(calculations));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(resiliencePolicies?.CalcsApiClient, nameof(resiliencePolicies.CalcsApiClient));

            _jobs = jobs;
            _calculations = calculations;
            _logger = logger;
            _calculationsPolicy = resiliencePolicies.CalcsApiClient;
        }

        public async Task HandleTemplateVersionChanged(SpecificationVersion specificationVersion,
            string fundingStreamId,
            string templateVersionId)
        {
            await HandleTemplateVersionChanged(specificationVersion,
                new Dictionary<string, string>
                {
                    {
                        fundingStreamId, templateVersionId
                    }
                },
                null,
                null);
        }

        public async Task HandleTemplateVersionChanged(SpecificationVersion specificationVersion,
            IDictionary<string, string> assignedTemplateIds,
            Reference user,
            string correlationId)
        {
            Guard.ArgumentNotNull(specificationVersion, nameof(specificationVersion));

            if (assignedTemplateIds.IsNullOrEmpty())
            {
                //this is a temporary branch to keep the existing edit call working before the UI catches up
                return;
            }

            string specificationId = specificationVersion.SpecificationId;
            string fundingPeriodId = specificationVersion.FundingPeriod.Id;

            foreach (KeyValuePair<string, string> assignedTemplateId in assignedTemplateIds)
            {
                string fundingStreamId = assignedTemplateId.Key;
                string templateVersionId = assignedTemplateId.Value;

                if (!specificationVersion.TemplateVersionHasChanged(fundingStreamId, templateVersionId))
                {
                    LogInformation($"FundingStream {fundingStreamId} template version id {templateVersionId} not changed in specification {specificationId}.");

                    continue;
                }

                LogInformation($"FundingStream {fundingStreamId} template version id {templateVersionId} changed for specification {specificationId}.");

                await AssignTemplateWithSpecification(specificationId, templateVersionId, fundingStreamId, fundingPeriodId);
                await QueueAssignTemplateCalculationsJob(user, correlationId, specificationId, fundingStreamId, fundingPeriodId, templateVersionId);
            }
        }

        private Task<Job> QueueAssignTemplateCalculationsJob(Reference user,
            string correlationId,
            string specificationId,
            string fundingStreamId,
            string fundingPeriodId,
            string templateVersionId) =>
            _jobs.QueueJob(new JobCreateModel
            {
                JobDefinitionId = AssignTemplateCalculationsJob,
                InvokerUserId = user?.Id,
                InvokerUserDisplayName = user?.Name,
                CorrelationId = correlationId,
                Trigger = new Trigger
                {
                    Message = "Changed template version for specification",
                    EntityId = specificationId,
                    EntityType = nameof(Specification)
                },
                Properties = new Dictionary<string, string>
                {
                    {
                        "specification-id", specificationId
                    },
                    {
                        "fundingstream-id", fundingStreamId
                    },
                    {
                        "fundingperiod-id", fundingPeriodId
                    },
                    {
                        "template-version", templateVersionId
                    }
                }
            });

        private async Task AssignTemplateWithSpecification(string specificationId,
            string templateVersionId,
            string fundingStreamId,
            string fundingPeriodId)
        {
            ApiResponse<TemplateMapping> mappingResponse = await _calculationsPolicy.ExecuteAsync(() => _calculations.AssociateTemplateIdWithSpecification(specificationId,
                templateVersionId,
                fundingStreamId));

            if (mappingResponse?.StatusCode.IsSuccess() != true)
            {
                string message = $"Unable to associate template version {templateVersionId} for funding stream {fundingStreamId} and period {fundingPeriodId} on specification {specificationId}";

                LogError(message);

                throw new InvalidOperationException(message);
            }
        }

        private void LogInformation(string message) => _logger.Information(FormatMessage(message));

        private void LogError(string message) => _logger.Error(FormatMessage(message));

        private static string FormatMessage(string message) => $"SpecificationTemplateVersionChangedHandler: {message}";
    }
}