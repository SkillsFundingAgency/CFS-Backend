using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentValidation;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PoliciesApiModels = CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Services.Datasets.Validators
{
    public class DatasetVersionUpdateModelValidator : AbstractValidator<DatasetVersionUpdateModel>
    {
        private readonly IEnumerable<string> validExtensions = new[] { ".csv", ".xls", ".xlsx" };
        private readonly IPolicyRepository _policyRepository;
        private readonly IJobManagement _jobManagement;
        private readonly AsyncPolicy _datasetRepositoryPolicies;
        private readonly IDatasetRepository _datasetRepository;

        public DatasetVersionUpdateModelValidator(IPolicyRepository policyRepository,
            IJobManagement jobManagement,
            IDatasetsResiliencePolicies datasetsResiliencePolicies,
            IDatasetRepository datasetRepository)
        {
            _policyRepository = policyRepository;
            _jobManagement = jobManagement;
            _datasetRepository = datasetRepository;
            _datasetRepositoryPolicies = datasetsResiliencePolicies.DatasetRepository;

            RuleFor(model => model.Filename)
             .Custom((name, context) =>
             {
                 DatasetVersionUpdateModel model = context.ParentContext.InstanceToValidate as DatasetVersionUpdateModel;
                 if(string.IsNullOrWhiteSpace(model.Filename))
                     context.AddFailure("You must provide a filename");
                 else if (!validExtensions.Contains(Path.GetExtension(model.Filename.ToLower())))
                     context.AddFailure("Check you have the right file format");
             });

            RuleFor(model => model.DatasetId)
                .CustomAsync(async(name, context, ct) =>
                {
                    DatasetVersionUpdateModel model = context.ParentContext.InstanceToValidate as DatasetVersionUpdateModel;

                    if (string.IsNullOrWhiteSpace(model.DatasetId))
                    {
                        context.AddFailure("You must give a datasetId");
                    }
                    else
                    {
                        IEnumerable<JobSummary> jobTypesRunning = await GetNonCompletedConverterJobs(model.DatasetId);

                        if (!jobTypesRunning.IsNullOrEmpty())
                        {
                            context.AddFailure($"Unable to upload a new dataset as there is a converter job running id:{jobTypesRunning.First().JobId}.");
                        }
                    }
                });

            RuleFor(model => model.FundingStreamId)
            .Custom((name, context) =>
            {
                DatasetVersionUpdateModel model = context.ParentContext.InstanceToValidate as DatasetVersionUpdateModel;
                if (string.IsNullOrWhiteSpace(model.FundingStreamId))
                {
                    context.AddFailure("You must give a Funding Stream Id for the dataset");
                }
                else
                {
                    IEnumerable<PoliciesApiModels.FundingStream> fundingStreams = _policyRepository.GetFundingStreams().Result;

                    if (fundingStreams != null && !fundingStreams.Any(_ => _.Id == model.FundingStreamId))
                    {
                        context.AddFailure($"Unable to find given funding stream ID: {model.FundingStreamId}");
                    }
                }
            });
        }

        public async Task<IEnumerable<JobSummary>> GetNonCompletedConverterJobs(string datasetId)
        {
            Guard.ArgumentNotNull(datasetId, nameof(datasetId));

            // only need relationships which have convert enabled as they will be the only ones which will have converter jobs running
            IEnumerable<DefinitionSpecificationRelationship> definitionSpecificationRelationships = await _datasetRepositoryPolicies.ExecuteAsync(() => _datasetRepository.GetDefinitionSpecificationRelationshipsByQuery(_ => _.Content.DatasetId == datasetId && _.Content.Current.ConverterEnabled));

            if (definitionSpecificationRelationships.IsNullOrEmpty())
            {
                return null;
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset windowOfTime = now.AddHours(-2);

            IDictionary<string, DefinitionSpecificationRelationship> definitionSpecificationRelationshipsDictionary = definitionSpecificationRelationships.ToDictionary(_ => _.Id);

            IEnumerable<JobSummary> jobSummaries = await _jobManagement.GetNonCompletedJobsWithinTimeFrame(windowOfTime, now);

            return jobSummaries?.Where(_ => _.JobType == JobConstants.DefinitionNames.RunConverterDatasetMergeJob && definitionSpecificationRelationshipsDictionary.ContainsKey(_.Properties["dataset-relationship-id"]));
        }
    }
}
