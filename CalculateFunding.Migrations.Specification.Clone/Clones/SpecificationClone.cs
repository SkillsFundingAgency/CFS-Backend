﻿using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.DataSets.Models;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Migrations.Specification.Clone.Helpers;
using CalculateFunding.Services.Core.Constants;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Migrations.Specification.Clone.Clones
{
    internal class SpecificationClone : ISpecificationClone
    {
        private readonly ILogger _logger;

        private readonly ISourceApiClient _sourceDataOperations;
        private readonly ITargetApiClient _targetDataOperations;

        public SpecificationClone(
            ILogger logger,
            ISourceApiClient sourceDataOperations,
            ITargetApiClient targetDataOperations)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(sourceDataOperations, nameof(sourceDataOperations));
            Guard.ArgumentNotNull(targetDataOperations, nameof(targetDataOperations));

            _logger = logger;

            _sourceDataOperations = sourceDataOperations;
            _targetDataOperations = targetDataOperations;
        }

        public async Task Run(CloneOptions cloneOptions)
        {
            string specificationId = cloneOptions.SourceSpecificationId;

            _logger.Information($"Starting to clone SpecificationId={specificationId}");

            _logger.Information($"Retrieving Specification Summary.");

            SpecificationSummary specificationSummary = await _sourceDataOperations.GetSpecificationSummaryById(specificationId);

            _logger.Information($"Retrieve Specification Summary completed.");

            string fundingStreamId = specificationSummary.FundingStreams.FirstOrDefault().Id;
            string existingFundingPeriodId = specificationSummary.FundingPeriod.Id;
            string fundingTemplateVersion = cloneOptions.TargetFundingTemplateVersion;

            // Get Target Funding Period
            _logger.Information($"Retrieving funding period with FundingPeriodId={cloneOptions.TargetPeriodId}");
            FundingPeriod targetFundingPeriod = await _sourceDataOperations.GetFundingPeriodById(cloneOptions.TargetPeriodId);
            _logger.Information($"FundingPeriodId={cloneOptions.TargetPeriodId} exists");
            
            // Clone spec and assign to Funding Template Version with Target Funding Period ID
            // All template items - funding lines and template calculations - Already cretead as part of above Create Spec API & related jobs
            CreateSpecificationModel createSpecificationModel = new CreateSpecificationModel
            {
                FundingPeriodId = targetFundingPeriod.Id,
                FundingStreamIds = new[] { fundingStreamId },
                Description = specificationSummary.Description,
                Name = $"Clone of {specificationSummary.Name} for FundingPeriod {targetFundingPeriod.Id} {Guid.NewGuid()}",
                ProviderSnapshotId = specificationSummary.ProviderSnapshotId,
                CoreProviderVersionUpdates = specificationSummary.CoreProviderVersionUpdates,
                ProviderVersionId = specificationSummary.ProviderVersionId,
                AssignedTemplateIds = new Dictionary<string, string>
                {
                    { fundingStreamId, fundingTemplateVersion }
                }
            };

            SpecificationSummary cloneSpecificationSummary = await _targetDataOperations.CreateSpecification(createSpecificationModel);

            _logger.Information($"Created clone SpecificationId={cloneSpecificationSummary.Id} and SpecificationName={cloneSpecificationSummary.Name}");

            // Wait for Template Calculations Creation Jobs Succeed

            IDictionary<string, JobSummary> latestJobsForSpecifications = 
                await _targetDataOperations.GetLatestJobsForSpecification(cloneSpecificationSummary.Id, new[] { JobConstants.DefinitionNames.AssignTemplateCalculationsJob });
            JobSummary assignTemplateCalcJobSummary = latestJobsForSpecifications[JobConstants.DefinitionNames.AssignTemplateCalculationsJob];

            _logger.Information($"{nameof(JobConstants.DefinitionNames.AssignTemplateCalculationsJob)} awaiting JobId={assignTemplateCalcJobSummary.JobId} to finish. JobStartTime={DateTime.UtcNow}");
            await ThenTheJobSucceeds(assignTemplateCalcJobSummary.JobId, $"Expected {nameof(JobConstants.DefinitionNames.AssignTemplateCalculationsJob)} to complete and succeed.");
            _logger.Information($"{nameof(JobConstants.DefinitionNames.AssignTemplateCalculationsJob)} job await finished at {DateTime.UtcNow}");

            _logger.Information($"Retrieving original specification calculations.");

            IEnumerable<Calculation> sourceCalculations = await _sourceDataOperations.GetCalculationsForSpecification(specificationId);

            _logger.Information($"Retrieved {sourceCalculations.Count()} calculations.");

            // Ensure changes on template calculations reflected to cloned specification calculations
            IEnumerable<Calculation> templateCalculations = sourceCalculations.Where(_ => _.CalculationType == CalculationType.Template);
            IEnumerable<Calculation> templateCalculationsWithChange = templateCalculations.Where(_ => _.Version > 1);

            if(templateCalculationsWithChange.Count() > 0)
            {
                _logger.Information($"{templateCalculationsWithChange.Count()} Template Calculations with changes exists. Replicating changes on cloned spec.");

                IEnumerable<Calculation> targetCalculations = await _targetDataOperations.GetCalculationsForSpecification(cloneSpecificationSummary.Id);

                foreach (Calculation templateCalculationWithChange in templateCalculationsWithChange)
                {
                    Calculation targetCalculation = targetCalculations.SingleOrDefault(_ => _.Name == templateCalculationWithChange.Name);

                    CalculationEditModel calculationEditModel = new CalculationEditModel
                    {
                        CalculationId = targetCalculation.Id,
                        DataType = templateCalculationWithChange.DataType,
                        Description = templateCalculationWithChange.Description,
                        Name = templateCalculationWithChange.Name,
                        SourceCode = templateCalculationWithChange.SourceCode,
                        SpecificationId = cloneSpecificationSummary.Id,
                        ValueType = templateCalculationWithChange.ValueType
                    };

                    await _targetDataOperations.EditCalculationWithSkipInstruct(cloneSpecificationSummary.Id, targetCalculation.Id, calculationEditModel);
                }

                _logger.Information($"Completed editing Template Calculations.");
            }

            // Clone additional calculations
            IEnumerable<Calculation> sourceAdditionalCalculations = sourceCalculations.Where(_ => _.CalculationType == CalculationType.Additional);
            _logger.Information($"Starting to create clone {sourceAdditionalCalculations.Count()} additional calculations");

            foreach (Calculation additionalCalculation in sourceAdditionalCalculations)
            {
                CalculationCreateModel calculationCreateModel = new CalculationCreateModel
                {
                    SpecificationId = cloneSpecificationSummary.Id,
                    Description = additionalCalculation.Description,
                    Name = additionalCalculation.Name,
                    SourceCode = additionalCalculation.SourceCode,
                    ValueType = additionalCalculation.ValueType,
                    Author = additionalCalculation.Author
                };

                await _targetDataOperations.CreateCalculation(
                    cloneSpecificationSummary.Id, 
                    calculationCreateModel,
                    skipCalcRun: true,
                    skipQueueCodeContextCacheUpdate: true,
                    overrideCreateModelAuthor: true);
            }
            _logger.Information($"Create clone {sourceAdditionalCalculations.Count()} additional calculations completed.");

            Common.ApiClient.Calcs.Models.Job queueCodeContextUpdateJob = await _targetDataOperations.QueueCodeContextUpdate(cloneSpecificationSummary.Id);
            _logger.Information($"Queued Code Context Update Job. JobId={queueCodeContextUpdateJob.Id}");

            QueueCalculationRunModel queueCalculationRunModel = new QueueCalculationRunModel
            {
                Author = new Reference("default", "defaultName"),
                CorrelationId = cloneSpecificationSummary.Id,
                Trigger = new TriggerModel
                {
                    EntityId = cloneSpecificationSummary.Id,
                    EntityType = nameof(SpecificationClone),
                    Message = "Assigning Additional Calculations for Specification"
                }
            };

            Common.ApiClient.Calcs.Models.Job queueCalculationRunJob = await _targetDataOperations.QueueCalculationRun(cloneSpecificationSummary.Id, queueCalculationRunModel);
            _logger.Information($"Queued Calculation Run Job. JobId={queueCalculationRunJob.Id}");

            // Clone uploaded data datasets (including converter wizard setting)
            // DefinitionSpecificationRelationship - where c.content.current.relationshipType == 'Uploaded'

            IEnumerable<DatasetSpecificationRelationshipViewModel> datasetSpecificationRelationshipViewModel = await _sourceDataOperations.GetRelationshipsBySpecificationId(specificationId);
            IEnumerable<DatasetSpecificationRelationshipViewModel> uploadedDatasetSpecificationRelationshipViewModels =
                datasetSpecificationRelationshipViewModel.Where(_ => _.RelationshipType == DatasetRelationshipType.Uploaded);

            _logger.Information($"{uploadedDatasetSpecificationRelationshipViewModels.Count()} uploaded dataset relationship exists. Starting to clone.");

            foreach (DatasetSpecificationRelationshipViewModel uploadedDatasetSpecificationRelationshipViewModel in uploadedDatasetSpecificationRelationshipViewModels)
            {
                CreateDefinitionSpecificationRelationshipModel createDefinitionSpecificationRelationshipModel = new CreateDefinitionSpecificationRelationshipModel
                {
                    DatasetDefinitionId = uploadedDatasetSpecificationRelationshipViewModel.Definition.Id,
                    SpecificationId = cloneSpecificationSummary.Id,
                    Name = uploadedDatasetSpecificationRelationshipViewModel.Name,
                    Description = uploadedDatasetSpecificationRelationshipViewModel.RelationshipDescription,
                    IsSetAsProviderData = uploadedDatasetSpecificationRelationshipViewModel.IsProviderData,
                    ConverterEnabled = uploadedDatasetSpecificationRelationshipViewModel.ConverterEnabled,
                    RelationshipType = uploadedDatasetSpecificationRelationshipViewModel.RelationshipType,
                };

                await _targetDataOperations.CreateRelationship(createDefinitionSpecificationRelationshipModel);
            }

            _logger.Information($"Completed clone uploaded dataset relationships.");

            _logger.Information($"Completed Spec Copy operation for SpecificationId={specificationId} and created SpecificationId={cloneSpecificationSummary.Id}");
        }

        private async Task ThenTheJobSucceeds(string jobId,
            string failureMessage,
            int timeoutSeconds = 600,
            int retryDelayMilliseconds = 5000)
                => await Wait.Until(() => TheJobSucceeds(jobId,
                failureMessage),
            failureMessage,
            timeoutSeconds,
            retryDelayMilliseconds);

        private async Task<bool> TheJobSucceeds(string jobId,
            string message)
        {
            JobViewModel job = await _targetDataOperations.GetJobById(jobId);

            if (job?.CompletionStatus == CompletionStatus.Failed)
            {
                throw new Exception(message);
            }

            return job?.RunningStatus == RunningStatus.Completed &&
                   job.CompletionStatus == CompletionStatus.Succeeded;
        }
    }
}
