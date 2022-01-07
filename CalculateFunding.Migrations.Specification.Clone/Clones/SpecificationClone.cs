using CalculateFunding.Common.ApiClient.Calcs.Models;
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
using DatasetRelationshipType = CalculateFunding.Common.ApiClient.DataSets.Models.DatasetRelationshipType;

namespace CalculateFunding.Migrations.Specification.Clone.Clones
{
    internal class SpecificationClone : ISpecificationClone
    {
        private readonly ILogger _logger;

        private readonly ISourceApiClient _sourceDataOperations;
        private readonly ITargetApiClient _targetDataOperations;
        private readonly IList<SpecificationMappingOption> _specificationMappingOptions;

        public SpecificationClone(
            ILogger logger,
            ISourceApiClient sourceDataOperations,
            ITargetApiClient targetDataOperations,
            IList<SpecificationMappingOption> specificationMappingOptions)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(sourceDataOperations, nameof(sourceDataOperations));
            Guard.ArgumentNotNull(targetDataOperations, nameof(targetDataOperations));
            Guard.ArgumentNotNull(specificationMappingOptions, nameof(specificationMappingOptions));

            _logger = logger;

            _sourceDataOperations = sourceDataOperations;
            _targetDataOperations = targetDataOperations;
            _specificationMappingOptions = specificationMappingOptions;
        }

        public async Task Run(CloneOptions cloneOptions)
        {
            string specificationId = cloneOptions.SourceSpecificationId;

            _logger.Information("Validating configuration.");
            if (! await ValidateConfiguration(cloneOptions))
            {
                _logger.Error("Configuration invalid - clone cancelled.");
                return;
            }
            _logger.Information("Configuration valid.");

            _logger.Information($"Starting to clone SpecificationId={specificationId}");

            _logger.Information($"Retrieving Specification Summary.");

            SpecificationSummary specificationSummary = await _sourceDataOperations.GetSpecificationSummaryById(specificationId);

            _logger.Information($"Retrieve Specification Summary completed.");

            string fundingStreamId = specificationSummary.FundingStreams.FirstOrDefault().Id;
            string existingFundingPeriodId = specificationSummary.FundingPeriod.Id;
            string fundingTemplateVersion = cloneOptions.TargetFundingTemplateVersion;

            // Get Target Funding Period
            _logger.Information($"Retrieving funding period with FundingPeriodId={cloneOptions.TargetPeriodId}");
            FundingPeriod targetFundingPeriod = await _targetDataOperations.GetFundingPeriodById(cloneOptions.TargetPeriodId);
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

            IEnumerable<DatasetSpecificationRelationshipViewModel> datasetSpecificationRelationshipViewModel = await _sourceDataOperations.GetRelationshipsBySpecificationId(specificationId);
            IEnumerable<DatasetSpecificationRelationshipViewModel> uploadedDatasetSpecificationRelationshipViewModels =
                datasetSpecificationRelationshipViewModel.Where(_ => _.RelationshipType == DatasetRelationshipType.Uploaded);

            _logger.Information($"{uploadedDatasetSpecificationRelationshipViewModels.Count()} uploaded data dataset relationship exists. Starting to clone.");

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

            _logger.Information($"Uploaded Data Dataset relationship Create operation completed.");

            if (cloneOptions.IncludeReleasedDataDateset.GetValueOrDefault())
            {
                IEnumerable<DatasetSpecificationRelationshipViewModel> releasedDatasetSpecificationRelationshipViewModels =
                    datasetSpecificationRelationshipViewModel.Where(_ => _.RelationshipType == DatasetRelationshipType.ReleasedData);
                _logger.Information($"{releasedDatasetSpecificationRelationshipViewModels.Count()} released data dataset relationship exists. Starting to clone.");

                foreach (DatasetSpecificationRelationshipViewModel releasedDatasetSpecificationRelationshipViewModel in releasedDatasetSpecificationRelationshipViewModels)
                {
                    SpecificationMappingOption specificationMappingOption
                        = _specificationMappingOptions.SingleOrDefault(_ => _.SourceSpecificationId == releasedDatasetSpecificationRelationshipViewModel.PublishedSpecificationConfiguration.SpecificationId);

                    CreateDefinitionSpecificationRelationshipModel createDefinitionSpecificationRelationshipModel = new CreateDefinitionSpecificationRelationshipModel
                    {
                        SpecificationId = cloneSpecificationSummary.Id,
                        Name = releasedDatasetSpecificationRelationshipViewModel.Name,
                        Description = releasedDatasetSpecificationRelationshipViewModel.RelationshipDescription,
                        IsSetAsProviderData = releasedDatasetSpecificationRelationshipViewModel.IsProviderData,
                        ConverterEnabled = releasedDatasetSpecificationRelationshipViewModel.ConverterEnabled,
                        RelationshipType = releasedDatasetSpecificationRelationshipViewModel.RelationshipType,
                        FundingLineIds = releasedDatasetSpecificationRelationshipViewModel.PublishedSpecificationConfiguration.FundingLines.Select(_ => _.TemplateId),
                        CalculationIds = releasedDatasetSpecificationRelationshipViewModel.PublishedSpecificationConfiguration.Calculations.Select(_ => _.TemplateId),
                        TargetSpecificationId = specificationMappingOption.targetSpecificationId
                    };

                    await _targetDataOperations.CreateRelationship(createDefinitionSpecificationRelationshipModel);
                }

                _logger.Information($"Released Data Dataset relationship upload operation completed.");
            }
            else
            {
                _logger.Information($"Skipping Released Data Dataset relationship creation operation.");
            }

            _logger.Information($"Retrieving original specification calculations.");

            IEnumerable<Calculation> sourceCalculations = await _sourceDataOperations.GetCalculationsForSpecification(specificationId);

            _logger.Information($"Retrieved {sourceCalculations.Count()} calculations.");

            // Clone additional calculations
            IEnumerable<Calculation> sourceAdditionalCalculations = sourceCalculations.Where(_ => _.CalculationType == CalculationType.Additional);
            
            IEnumerable<Calculation> targetCalculations = await _sourceDataOperations.GetCalculationsForSpecification(cloneSpecificationSummary.Id);

            _logger.Information($"Starting to create clone {sourceAdditionalCalculations.Count()} additional calculations");

            int additionalCalculationIndex = 0;

            foreach (Calculation additionalCalculation in sourceAdditionalCalculations)
            {
                // Edge-case scenario
                // Target Specification has an additional calcuation with the same name as template calculation one on the funding template.
                // This case can not be replicated. Behaviour is to
                // 1. Do not create that additional calculation
                // 2. Update template calculation details with the source additional calculation
                if (targetCalculations.Select(_ => _.Name).Contains(additionalCalculation.Name))
                {
                    Calculation templateCalculation = targetCalculations.SingleOrDefault(_ => _.Name == additionalCalculation.Name);

                    CalculationEditModel calculationEditModel = new CalculationEditModel
                    {
                        SpecificationId = cloneSpecificationSummary.Id,
                        CalculationId = templateCalculation.Id,
                        Name = additionalCalculation.Name,
                        DataType = additionalCalculation.DataType,
                        Description = additionalCalculation.Description,
                        SourceCode = additionalCalculation.SourceCode,
                        ValueType = additionalCalculation.ValueType
                    };

                    await _targetDataOperations.EditCalculationWithSkipInstruct(cloneSpecificationSummary.Id, templateCalculation.Id, calculationEditModel);
                }
                else
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

                    try
                    {
                        await _targetDataOperations.CreateCalculation(
                                                cloneSpecificationSummary.Id,
                                                calculationCreateModel,
                                                skipCalcRun: true,
                                                skipQueueCodeContextCacheUpdate: true,
                                                overrideCreateModelAuthor: true);
                    }
                    catch(Exception ex)
                    {
                        _logger.Error(ex, $"Error creating additional calculation {calculationCreateModel.Name} so calculation skipped. " +
                            $"This is probably due to calculation referencing template calculation which is not in template. " +
                            $"Additional calculation will need to be manually created with correct details if required.");
                    }
                }
                additionalCalculationIndex++;

                if (additionalCalculationIndex % 10 == 0)
                {
                    _logger.Information($"Create clone {additionalCalculationIndex}/{sourceAdditionalCalculations.Count()} additional calculations completed.");
                }
            }
            _logger.Information($"Create clone {sourceAdditionalCalculations.Count()} additional calculations completed.");

            // Ensure changes on template calculations reflected to cloned specification calculations
            IEnumerable<Calculation> templateCalculations = sourceCalculations.Where(_ => _.CalculationType == CalculationType.Template);
            IEnumerable<Calculation> templateCalculationsWithChange = templateCalculations.Where(_ => _.Version > 1);

            if(templateCalculationsWithChange.Count() > 0)
            {
                _logger.Information($"{templateCalculationsWithChange.Count()} Template Calculations with changes exists. Replicating changes on cloned spec.");

                targetCalculations = await _targetDataOperations.GetCalculationsForSpecification(cloneSpecificationSummary.Id);

                int templateCalculationWithChangeIndex = 0;
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

                    try
                    {
                        await _targetDataOperations.EditCalculationWithSkipInstruct(cloneSpecificationSummary.Id, targetCalculation.Id, calculationEditModel);

                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"Edit SpecificationID={cloneSpecificationSummary.Id} TemplateCalculationID={targetCalculation.Id} failed with given exception message. " +
                            $"SpecificationClone operation is not interrupted for this error. Please check detailed error message for exception and possible fix." +
                            $"Possible scenarios are:" +
                            $"1. Source calculation has reference to a 'Released Data' and Clone operation does not include cloning 'Released Data' datasets to target specification. Fix: Please update calculation source code manually");
                    }

                    templateCalculationWithChangeIndex++;
                    if (templateCalculationWithChangeIndex % 10 == 0)
                    {
                        _logger.Information($"Edit clone {templateCalculationWithChangeIndex}/{templateCalculationsWithChange.Count()} template calculations completed.");
                    }
                }

                _logger.Information($"Completed editing Template Calculations.");
            }

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

            _logger.Information($"Completed clone uploaded dataset relationships.");

            _logger.Information($"Completed Spec Copy operation for SpecificationId={specificationId} and created SpecificationId={cloneSpecificationSummary.Id}");
        }

        public async Task<bool> ValidateConfiguration(CloneOptions cloneOptions)
        {
            string specificationId = cloneOptions.SourceSpecificationId;
            try
            {
                SpecificationSummary specificationSummary = await _sourceDataOperations.GetSpecificationSummaryById(specificationId);
                FundingPeriod targetFundingPeriod = await _targetDataOperations.GetFundingPeriodById(cloneOptions.TargetPeriodId);

                string fundingStreamId = specificationSummary.FundingStreams.FirstOrDefault().Id;

                FundingTemplateContents template = await _targetDataOperations.GetFundingTemplate(fundingStreamId, targetFundingPeriod.Id, cloneOptions.TargetFundingTemplateVersion);
            }
            catch
            {
                return false;
            }
            
            if (cloneOptions.IncludeReleasedDataDateset.GetValueOrDefault())
            {
                if(_specificationMappingOptions == null || !_specificationMappingOptions.Any())
                {
                    _logger.Error("include-released-data-dataset argument used, however no SpecificationMappingOption configuration items have been added.");
                    return false;
                }
                
                IEnumerable<DatasetSpecificationRelationshipViewModel> datasetSpecificationRelationshipViewModel = await _sourceDataOperations.GetRelationshipsBySpecificationId(specificationId);

                IEnumerable<DatasetSpecificationRelationshipViewModel> releasedDatasetSpecificationRelationshipViewModels =
                    datasetSpecificationRelationshipViewModel.Where(_ => _.RelationshipType == DatasetRelationshipType.ReleasedData);

                if (!releasedDatasetSpecificationRelationshipViewModels.Any())
                {
                    _logger.Error("include-released-data-dataset argument used, however specification does not contain any released data items.");
                    return false;
                }

                List<string> unreferencedReleasedSpecifications = releasedDatasetSpecificationRelationshipViewModels
                                                            .Where(_ => !_specificationMappingOptions.Select(m => m.SourceSpecificationId).Contains(_.PublishedSpecificationConfiguration.SpecificationId))
                                                            .Select(_ => _.PublishedSpecificationConfiguration.SpecificationId).ToList();
                if (unreferencedReleasedSpecifications.Any())
                {
                    _logger.Error($"Source specification contains released datasets which have no associated SpecificationMappingOption configuration item. {unreferencedReleasedSpecifications.Join(",")}");
                    return false;
                }
            }

            return true;
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
