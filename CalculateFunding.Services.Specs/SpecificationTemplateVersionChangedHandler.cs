using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Graph;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Specs.Interfaces;
using Polly;
using Serilog;
using Job = CalculateFunding.Common.ApiClient.Jobs.Models.Job;
using GraphApiEntitySpecification = CalculateFunding.Common.ApiClient.Graph.Models.Specification;
using GraphApiEntityCalculation = CalculateFunding.Common.ApiClient.Graph.Models.Calculation;
using GraphApiEntityFundingLine = CalculateFunding.Common.ApiClient.Graph.Models.FundingLine;
using CalculateFunding.Common.ApiClient.Graph.Models;
using CalculateFunding.Models.Graph;

namespace CalculateFunding.Services.Specs
{
    public class SpecificationTemplateVersionChangedHandler : ISpecificationTemplateVersionChangedHandler
    {
        private const string AssignTemplateCalculationsJob = JobConstants.DefinitionNames.AssignTemplateCalculationsJob;

        private readonly IJobManagement _jobs;
        private readonly ICalculationsApiClient _calculations;
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly IGraphApiClient _graphApiClient;
        private readonly ILogger _logger;
        private readonly AsyncPolicy _calculationsPolicy;
        private readonly AsyncPolicy _policiesPolicy;

        public SpecificationTemplateVersionChangedHandler(IJobManagement jobs,
            ICalculationsApiClient calculations,
            ISpecificationsResiliencePolicies resiliencePolicies,
            ILogger logger,
            IPoliciesApiClient policiesApiClient,
            IGraphApiClient graphApiClient)
        {
            Guard.ArgumentNotNull(jobs, nameof(jobs));
            Guard.ArgumentNotNull(calculations, nameof(calculations));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(resiliencePolicies?.CalcsApiClient, nameof(resiliencePolicies.CalcsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.PoliciesApiClient, nameof(resiliencePolicies.PoliciesApiClient));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(graphApiClient, nameof(graphApiClient));

            _jobs = jobs;
            _calculations = calculations;
            _logger = logger;
            _policiesApiClient = policiesApiClient;
            _graphApiClient = graphApiClient;
            _calculationsPolicy = resiliencePolicies.CalcsApiClient;
            _policiesPolicy = resiliencePolicies.PoliciesApiClient;
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
                    await DetectAndCreateObsoleteItemsForFundingLines(specificationId, fundingStreamId, fundingPeriodId, previousTemplateVersionId, templateVersionId);
                }

                await AssignTemplateWithSpecification(specificationVersion, templateVersionId, fundingStreamId, fundingPeriodId);
                await QueueAssignTemplateCalculationsJob(user, correlationId, specificationId, fundingStreamId, fundingPeriodId, templateVersionId);
            }
        }

        private async Task DetectAndCreateObsoleteItemsForFundingLines(
            string specificationId,
            string fundingStreamId,
            string fundingPeriodId,
            string previousTemplateVersionId,
            string templateVersionId)
        {
            Task<TemplateMetadataDistinctContents> previousTemplateMetadataTask = GetDistinctTemplateMetadataContents(fundingStreamId, fundingPeriodId, previousTemplateVersionId);
            Task<TemplateMetadataDistinctContents> templateMetadataTask = GetDistinctTemplateMetadataContents(fundingStreamId, fundingPeriodId, templateVersionId);

            await TaskHelper.WhenAllAndThrow(previousTemplateMetadataTask, templateMetadataTask);
            TemplateMetadataDistinctContents previousTemplateMetadata = previousTemplateMetadataTask.Result;
            TemplateMetadataDistinctContents templateMetadata = templateMetadataTask.Result;

            IEnumerable<TemplateMetadataFundingLine> missingFundingLines = previousTemplateMetadata.FundingLines
                                                                            .Where(p => !templateMetadata.FundingLines.Any(c => c.FundingLineCode == p.FundingLineCode))
                                                                            .ToList();

            if(!missingFundingLines.Any())
            {
                return;
            }

            ApiResponse<IEnumerable<Entity<GraphApiEntitySpecification>>> specificationEntitiesApiResponse =
                await _graphApiClient.GetAllEntitiesRelatedToSpecification(specificationId);

            if (!specificationEntitiesApiResponse.StatusCode.IsSuccess() && specificationEntitiesApiResponse.Content == null)
            {
                string message = $"Unable to find the graph entities for specification - {specificationId}.";
                LogInformation(message);
                throw new Exception(message);
            }

            IEnumerable<Entity<GraphApiEntitySpecification>> specificationEntities = specificationEntitiesApiResponse.Content;

            IEnumerable<Models.Graph.Calculation> specificationCalculations = specificationEntities
                .Where(_ => _.Relationships != null)
                .SelectMany(_ => _.Relationships.Where(rel => rel.Type.Equals(SpecificationCalculationRelationships.FromIdField, StringComparison.InvariantCultureIgnoreCase)))
                .Select(rel => ((object)rel.One).AsJson().AsPoco<Models.Graph.Calculation>())
                .Distinct();

            foreach (var fundingLine in missingFundingLines)
            {
                ApiResponse<IEnumerable<Entity<GraphApiEntityFundingLine>>> fundingLineEntitiesApiResponse =
                    await _graphApiClient.GetAllEntitiesRelatedToFundingLine(fundingLine.FundingLineCode);

                if (!specificationEntitiesApiResponse.StatusCode.IsSuccess() && specificationEntitiesApiResponse.Content == null)
                {
                    string message = $"Unable to find the graph entities for funding line - {fundingLine}.";
                    LogInformation(message);
                    throw new Exception(message);
                }

                IEnumerable<Entity<GraphApiEntityFundingLine>> fundinglineEntities = fundingLineEntitiesApiResponse.Content;

                IEnumerable<string> fundingLineCalculationIds = fundinglineEntities.Where(_ => _.Relationships != null)
                    .SelectMany(_ => _.Relationships.Where(rel => rel.Type.Equals(FundingLineCalculationRelationship.FromIdField, StringComparison.InvariantCultureIgnoreCase)))
                    .Select(rel => ((object)rel.Two).AsJson().AsPoco<Models.Graph.Calculation>().CalculationId)
                    .Distinct();

                // Calculations in specification and in missing fundingline
                IDictionary<string, IEnumerable<string>> fundingLineCalculationIdsByTemplateCalculationId
                    = specificationCalculations
                    .Where(sc => fundingLineCalculationIds.Contains(sc.CalculationId))
                    .GroupBy(x => x.TemplateCalculationId)
                    .ToDictionary(x => x.Key, x => x.Select(x => x.CalculationId).Distinct());

                if (fundingLineCalculationIdsByTemplateCalculationId.Any())
                {
                    foreach (var calculationsByTemplateId in fundingLineCalculationIdsByTemplateCalculationId.Where(x => x.Value.Any()))
                    {
                        ObsoleteItem obsoleteItem = new ObsoleteItem
                        {
                            SpecificationId = specificationId,
                            FundingLineId = fundingLine.FundingLineCode,
                            TemplateCalculationId = uint.Parse(calculationsByTemplateId.Key),
                            CalculationIds = calculationsByTemplateId.Value.ToList(),
                            ItemType = ObsoleteItemType.FundingLine,
                            Id = Guid.NewGuid().ToString()
                        };

                        ApiResponse<ObsoleteItem> obsoleteItemResponse = await _calculationsPolicy.ExecuteAsync(() => _calculations.CreateObsoleteItem(obsoleteItem));

                        if (!specificationEntitiesApiResponse.StatusCode.IsSuccess() && specificationEntitiesApiResponse.Content == null)
                        {
                            string message = $"Unable to find the graph entities for funding line - {fundingLine}.";
                            LogInformation(message);
                            throw new Exception(message);
                        }
                    }
                }
            }
        }

        private async Task<TemplateMetadataDistinctContents> GetDistinctTemplateMetadataContents(string fundingStreamId, string fundingPeriodId, string templateVersion)
        {
            ApiResponse<TemplateMetadataDistinctContents> templateMetatdataResponse = await _policiesPolicy.ExecuteAsync(() => _policiesApiClient.GetDistinctTemplateMetadataContents(fundingStreamId, fundingPeriodId, templateVersion));

            if (!templateMetatdataResponse.StatusCode.IsSuccess() || templateMetatdataResponse.Content == null)
            {
                string message = $"Unable to find the template metadata for funding stream - {fundingStreamId}, funding period id - {fundingPeriodId}, template version - {templateVersion}.";
                LogInformation(message);
                throw new Exception(message);
            }

            return templateMetatdataResponse.Content;
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
                SpecificationId = specificationId,
                Trigger = new Trigger
                {
                    Message = "Changed template version for specification",
                    EntityId = specificationId,
                    EntityType = nameof(Models.Specs.Specification)
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