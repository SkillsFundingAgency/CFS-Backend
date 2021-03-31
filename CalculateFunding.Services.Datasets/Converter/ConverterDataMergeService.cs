using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Processing;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;
using static CalculateFunding.Services.Core.NonRetriableException;

namespace CalculateFunding.Services.Datasets.Converter
{
    public class ConverterDataMergeService : JobProcessingService, IConverterDataMergeService
    {
        private readonly IValidator<ConverterMergeRequest> _requestValidator;
        private readonly IDatasetRepository _datasets;
        private readonly IConverterEligibleProviderService _eligibleProviderConverter;
        private readonly IPoliciesApiClient _policies;
        private readonly ISpecificationsApiClient _specifications;
        private readonly IConverterDataMergeLogger _converterDataMergeLogger;
        private readonly IDatasetCloneBuilderFactory _datasetCloneBuilderFactory;
        private readonly AsyncPolicy _policiesResilience;
        private readonly AsyncPolicy _datasetsResilience;
        private readonly AsyncPolicy _specificationsResilience;

        public ConverterDataMergeService(IDatasetRepository datasets,
            IConverterEligibleProviderService eligibleProviderConverter,
            IPoliciesApiClient policies,
            IConverterDataMergeLogger converterDataMergeLogger,
            ISpecificationsApiClient specifications,
            IDatasetCloneBuilderFactory datasetCloneBuilderFactory,
            IValidator<ConverterMergeRequest> requestValidator,
            IDatasetsResiliencePolicies resiliencePolicies,
            IJobManagement jobs,
            ILogger logger) : base(jobs, logger)
        {
            Guard.ArgumentNotNull(datasets, nameof(datasets));
            Guard.ArgumentNotNull(eligibleProviderConverter, nameof(eligibleProviderConverter));
            Guard.ArgumentNotNull(policies, nameof(policies));
            Guard.ArgumentNotNull(converterDataMergeLogger, nameof(converterDataMergeLogger));
            Guard.ArgumentNotNull(specifications, nameof(specifications));
            Guard.ArgumentNotNull(resiliencePolicies?.PoliciesApiClient, nameof(_policiesResilience));
            Guard.ArgumentNotNull(resiliencePolicies.DatasetRepository, nameof(_datasetsResilience));
            Guard.ArgumentNotNull(resiliencePolicies.SpecificationsApiClient, nameof(_specificationsResilience));

            _datasets = datasets;
            _eligibleProviderConverter = eligibleProviderConverter;
            _policies = policies;
            _converterDataMergeLogger = converterDataMergeLogger;
            _specifications = specifications;
            _datasetCloneBuilderFactory = datasetCloneBuilderFactory;
            _requestValidator = requestValidator;

            _policiesResilience = resiliencePolicies.PoliciesApiClient;
            _datasetsResilience = resiliencePolicies.DatasetRepository;
            _specificationsResilience = resiliencePolicies.SpecificationsApiClient;
        }

        public async Task<IActionResult> QueueJob(ConverterMergeRequest request)
        {
            Guard.ArgumentNotNull(request, nameof(request));

            ValidationResult validation = await _requestValidator.ValidateAsync(request);

            if (!validation.IsValid) return validation.AsBadRequest();

            Job job = (await QueueJobs(new JobCreateModel
            {
                JobDefinitionId = JobConstants.DefinitionNames.RunConverterDatasetMergeJob,
                Trigger = new Trigger
                {
                    Message = "Converter Merge Dataset Requested",
                    EntityId = request.DatasetRelationshipId,
                    EntityType = "DatasetRelationship"
                },
                MessageBody = request.AsJson(),
                Properties = new Dictionary<string, string>
                {
                    {"dataset-relationship-id", request.DatasetRelationshipId}
                }
            }))?.SingleOrDefault();

            return new OkObjectResult(job);
        }

        public override async Task Process(Message message)
        {
            EnsureIsNotNull(message, nameof(message));

            string jobId = message.GetUserProperty<string>("jobId");

            ConverterMergeRequest request = message.GetPayloadAsInstanceOf<ConverterMergeRequest>();

            await MergeDatasetForConverters(request, jobId);
        }

        private async Task MergeDatasetForConverters(ConverterMergeRequest request, string jobId)
        {
            EnsureRequestIsValid(request);

            Dataset dataset = await LookupDataset(request);
            DatasetDefinition datasetDefinition = await LookupDatasetDefinition(dataset);

            await EnsureConverterCanBeRunAgainstDataset(datasetDefinition, dataset.Current, request.ProviderVersionId);
            EnsureFieldDefinitionIsValid(datasetDefinition);

            DefinitionSpecificationRelationship relationship = await GetDatasetRelationship(request);
            FundingConfiguration fundingConfiguration = await GetFundingConfiguration(relationship, datasetDefinition.FundingStreamId);

            EnsureConvertersAreEnabledForFundingConfiguration(fundingConfiguration);

            EligibleConverter[] eligibleProviders = (await GetEligibleProviders(request, fundingConfiguration)).ToArray();

            if (eligibleProviders.Length == 0) return;

            IDatasetCloneBuilder datasetCloneBuilder = _datasetCloneBuilderFactory.CreateCloneBuilder();

            await datasetCloneBuilder.LoadOriginalDataset(dataset, datasetDefinition);

           (string identifierFieldName, EligibleConverter[] convertersToProcess) =
                 DetectProvidersToProcess(eligibleProviders, datasetDefinition, datasetCloneBuilder);

            if (!convertersToProcess.Any()) return;

            List<RowCopyResult> results = new List<RowCopyResult>(convertersToProcess.Count());

            foreach (EligibleConverter converter in convertersToProcess)
            { 
                CopyRowInDataset(results, converter, datasetCloneBuilder, identifierFieldName);
            }

            DatasetVersion createdDatasetVersion = await SaveDatasetVersionWhenChangesExist(results, 
                request.Author,
                dataset, 
                datasetDefinition, 
                datasetCloneBuilder);

            if (createdDatasetVersion == null) return;

            await SaveOutcomesAsLog(results, request, createdDatasetVersion, jobId);
        }

        private static void EnsureConvertersAreEnabledForFundingConfiguration(FundingConfiguration fundingConfiguration)
        {
            Ensure(fundingConfiguration.EnableConverterDataMerge,
                $"Converter data merge not enabled for funding stream {fundingConfiguration.FundingStreamId} and funding period {fundingConfiguration.FundingPeriodId}");
        }

        private async Task<FundingConfiguration> GetFundingConfiguration(DefinitionSpecificationRelationship relationship,
            string fundingStreamId)
        {
            string specificationId = relationship.Specification?.Id;

            EnsureIsNotNullOrWhitespace(specificationId, $"DefinitionSpecificationRelationship {relationship.Id} has no specification reference.");

            SpecificationSummary specificationSummary =
                (await _specificationsResilience.ExecuteAsync(() => _specifications.GetSpecificationSummaryById(specificationId)))?.Content;

            EnsureIsNotNull(specificationSummary, $"Did not locate s specification summary for id {specificationId}");

            string fundingPeriodId = specificationSummary.FundingPeriod?.Id;

            FundingConfiguration fundingConfiguration = (await _policiesResilience.ExecuteAsync(() =>
                _policies.GetFundingConfiguration(fundingStreamId,
                    fundingPeriodId)))?.Content;

            EnsureIsNotNull(fundingConfiguration, $"Did not locate funding configuration for {fundingStreamId} {fundingPeriodId}");

            return fundingConfiguration;
        }

        private async Task SaveOutcomesAsLog(IEnumerable<RowCopyResult> results,
            ConverterMergeRequest request,
            DatasetVersion createdDatasetVersion,
            string jobId)
        {
            await _converterDataMergeLogger.SaveLogs(results, request, jobId, createdDatasetVersion.Version);
        }

        private async Task EnsureConverterCanBeRunAgainstDataset(DatasetDefinition datasetDefinition,
            DatasetVersion dataset,
            string providerVersionId)
        {
            // bool alreadyUsingCoreProviderVersion = dataset

            // Check if the dataset is enabled for the converter wizard. Fail job if not enabled

            // Check the last core provider version which has run against the dataset, if it's the same as the one being request, complete the job as successful, but log that it skipped processing

            //from Dan - apparently I just need to check which provider version id is stored against the datasetversion to check this (should be coming with a prereq)
        }

        private async Task<DefinitionSpecificationRelationship> GetDatasetRelationship(ConverterMergeRequest request)
        {
            DefinitionSpecificationRelationship relationship =
                await _datasets.GetDefinitionSpecificationRelationshipById(request.DatasetRelationshipId);

            EnsureIsNotNull(relationship, $"Dataset relationship not found. Id = '{request.DatasetRelationshipId}'");

            return relationship;
        }

        private async Task<IEnumerable<EligibleConverter>> GetEligibleProviders(ConverterMergeRequest request,
            FundingConfiguration fundingConfiguration)
        {
            IEnumerable<EligibleConverter> eligibleProviders =
                await _eligibleProviderConverter.GetProviderIdsForConverters(request.ProviderVersionId, fundingConfiguration);

            EnsureIsNotNull(eligibleProviders, "Eligible providers returned null");

            return eligibleProviders;
        }

        private async Task<DatasetVersion> SaveDatasetVersionWhenChangesExist(List<RowCopyResult> results,
            Reference author,
            Dataset dataset,
            DatasetDefinition datasetDefinition,
            IDatasetCloneBuilder datasetCloneBuilder) =>
            results.Any(_ => _.Outcome == RowCopyOutcome.Copied) ? 
                await datasetCloneBuilder.SaveContents(author, datasetDefinition, dataset) : 
                null;

        private void CopyRowInDataset(ICollection<RowCopyResult> results,
            EligibleConverter converter,
            IDatasetCloneBuilder datasetCloneBuilder,
            string identifierFieldName)
        {
            RowCopyResult result = datasetCloneBuilder.CopyRow(identifierFieldName,
                converter.PreviousProviderIdentifier, 
                converter.ProviderId);

            EnsureIsNotNull(result, $"Copy row result was null when processing {converter.PreviousProviderIdentifier} to {converter.ProviderId}");

            results.Add(result);
        }

        private static void EnsureFieldDefinitionIsValid(DatasetDefinition datasetDefinition)
        {
            FieldDefinition identifierField = datasetDefinition.TableDefinitions?.FirstOrDefault()?.FieldDefinitions
                .SingleOrDefault(_ => _.IdentifierFieldType.HasValue);

            EnsureIsNotNull(identifierField, "No identifier field was specified on this dataset definition.");
            Ensure(identifierField.IdentifierFieldType == IdentifierFieldType.UKPRN,
                "Converter data merge only supports schemas with UKPRN set as the identifier.");
        }

        private async Task<DatasetDefinition> LookupDatasetDefinition(Dataset dataset)
        {
            string definitionId = dataset.Definition.Id;

            DatasetDefinition datasetDefinition = await _datasets.GetDatasetDefinition(definitionId);

            EnsureIsNotNull(datasetDefinition, $"Did not locate dataset definition {definitionId}");
            Ensure(datasetDefinition.ConverterEnabled, "Dataset is not enabled for converters. Enable it in the dataset definition.");

            return datasetDefinition;
        }

        private async Task<Dataset> LookupDataset(ConverterMergeRequest request)
        {
            Dataset dataset = await _datasets.GetDatasetByDatasetId(request.DatasetId);

            EnsureIsNotNull(dataset, "Dataset not found.");
            EnsureIsNotNull(dataset.Definition, "Dataset has no definition.");

            return dataset;
        }

        private static void EnsureRequestIsValid(ConverterMergeRequest request)
        {
            EnsureIsNotNull(request, "No ConverterMergeRequest supplied to process");
            EnsureIsNotNullOrWhitespace(request.ProviderVersionId, "Empty or null providerVersionId");
            EnsureIsNotNullOrWhitespace(request.DatasetId, "Empty or null datasetId");
            EnsureIsNotNullOrWhitespace(request.Version, "Empty or null version");
            EnsureIsNotNullOrWhitespace(request.DatasetRelationshipId, "Empty or null datasetRelationshipId");
        }

        private (string fieldNameIdentifier, EligibleConverter[]) DetectProvidersToProcess(IEnumerable<EligibleConverter> eligibleProviders,
            DatasetDefinition datasetDefinition,
            IDatasetCloneBuilder datasetCloneBuilder)
        {
            FieldDefinition fieldOfIdentifier = datasetDefinition
                .TableDefinitions
                .First()
                .FieldDefinitions
                .Single(_ => _.IdentifierFieldType == IdentifierFieldType.UKPRN);

            string identifierFieldName = fieldOfIdentifier.Name;
            
            IEnumerable<string> existingProviderIdentifiers = datasetCloneBuilder.GetExistingIdentifierValues(identifierFieldName);

            IEnumerable<EligibleConverter> providersWithPreviousDatasetSources = eligibleProviders
                .Where(_ => existingProviderIdentifiers.Contains(_.PreviousProviderIdentifier));

            return (identifierFieldName, providersWithPreviousDatasetSources
                .Where(_ => !existingProviderIdentifiers
                    .Contains(_.ProviderId)).ToArray());
        }
    }
}