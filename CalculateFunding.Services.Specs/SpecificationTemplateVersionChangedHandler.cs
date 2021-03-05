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
using Job = CalculateFunding.Common.ApiClient.Jobs.Models.Job;

namespace CalculateFunding.Services.Specs
{
    public class SpecificationTemplateVersionChangedHandler : ISpecificationTemplateVersionChangedHandler
    {
        private const string AssignTemplateCalculationsJob = JobConstants.DefinitionNames.AssignTemplateCalculationsJob;
        private const string DetectObsoleteFundingLinesJob = JobConstants.DefinitionNames.DetectObsoleteFundingLinesJob;

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

        public async Task HandleTemplateVersionChanged(SpecificationVersion previousSpecificationVersion,
            SpecificationVersion specificationVersion,
            IDictionary<string, string> assignedTemplateIds,
            Reference user,
            string correlationId)
        {
            Guard.ArgumentNotNull(previousSpecificationVersion, nameof(previousSpecificationVersion));

            if (assignedTemplateIds.IsNullOrEmpty())
            {
                //this is a temporary branch to keep the existing edit call working before the UI catches up
                return;
            }

            string specificationId = previousSpecificationVersion.SpecificationId;
            string fundingPeriodId = previousSpecificationVersion.FundingPeriod.Id;

            foreach (KeyValuePair<string, string> assignedTemplateId in assignedTemplateIds)
            {
                string fundingStreamId = assignedTemplateId.Key;
                string templateVersionId = assignedTemplateId.Value;

                if (!previousSpecificationVersion.TemplateVersionHasChanged(fundingStreamId, templateVersionId))
                {
                    LogInformation($"FundingStream {fundingStreamId} template version id {templateVersionId} not changed in specification {specificationId}.");

                    continue;
                }

                LogInformation($"FundingStream {fundingStreamId} template version id {templateVersionId} changed for specification {specificationId}.");

                string previousTemplateVersionId = previousSpecificationVersion.GetTemplateVersionId(fundingStreamId);
                if (!string.IsNullOrWhiteSpace(previousTemplateVersionId))
                {
                    await DetectAndCreateObsoleteItemsForFundingLines(user, correlationId, specificationId, fundingStreamId, fundingPeriodId, previousTemplateVersionId, templateVersionId);
                }

                await AssignTemplateWithSpecification(specificationVersion, templateVersionId, fundingStreamId, fundingPeriodId);
                await QueueAssignTemplateCalculationsJob(user, correlationId, specificationId, fundingStreamId, fundingPeriodId, templateVersionId);
            }
        }

        private Task<Job> DetectAndCreateObsoleteItemsForFundingLines(Reference user,
            string correlationId,
            string specificationId,
            string fundingStreamId,
            string fundingPeriodId,
            string previousTemplateVersionId,
            string templateVersionId) => _jobs.QueueJob(new JobCreateModel
            {
                JobDefinitionId = DetectObsoleteFundingLinesJob,
                InvokerUserId = user?.Id,
                InvokerUserDisplayName = user?.Name,
                CorrelationId = correlationId,
                SpecificationId = specificationId,
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
                        "previous-template-version-id", previousTemplateVersionId
                    },
                    {
                        "template-version", templateVersionId
                    }
                }
            });

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
                SpecificationId = specificationId,
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

        private async Task AssignTemplateWithSpecification(SpecificationVersion specificationVersion,
            string templateVersionId,
            string fundingStreamId,
            string fundingPeriodId)
        {
            specificationVersion.AddOrUpdateTemplateId(fundingStreamId, templateVersionId);

            ApiResponse<TemplateMapping> mappingResponse = await _calculationsPolicy.ExecuteAsync(() => _calculations.ProcessTemplateMappings(specificationVersion.SpecificationId,
                templateVersionId,
                fundingStreamId));

            if (mappingResponse?.StatusCode.IsSuccess() != true)
            {
                string message = $"Unable to associate template version {templateVersionId} for funding stream {fundingStreamId} and period {fundingPeriodId} on specification {specificationVersion.SpecificationId}";

                LogError(message);

                throw new InvalidOperationException(message);
            }
        }

        private void LogInformation(string message) => _logger.Information(FormatMessage(message));

        private void LogError(string message) => _logger.Error(FormatMessage(message));

        private static string FormatMessage(string message) => $"SpecificationTemplateVersionChangedHandler: {message}";
    }
}