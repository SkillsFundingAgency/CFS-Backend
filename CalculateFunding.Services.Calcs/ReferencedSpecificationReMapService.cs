using CalculateFunding.Common.ApiClient.DataSets;
using CalculateFunding.Common.ApiClient.DataSets.Models;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Helpers;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type.Interfaces;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Processing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;
using UpdateDefinitionSpecificationRelationshipModel = CalculateFunding.Common.ApiClient.DataSets.Models.UpdateDefinitionSpecificationRelationshipModel;

namespace CalculateFunding.Services.Calcs
{
    public class ReferencedSpecificationReMapService : JobProcessingService, IReferencedSpecificationReMapService, IHealthChecker
    {
        private const string SpecificationId = "specification-id";
        private const string DatasetSpecificationRelationshipId = "dataset-specification-relationship-id";
        private readonly IJobManagement _jobManagement;
        private readonly ILogger _logger;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly IDatasetsApiClient _datasetsApiClient;
        private readonly ICalculationCodeReferenceUpdate _calculationCodeReferenceUpdate;
        private readonly AsyncPolicy _specificationsApiClientPolicy;
        private readonly AsyncPolicy _calculationsRepositoryPolicy;
        private readonly AsyncPolicy _policiesApiClientPolicy;
        private readonly AsyncPolicy _datasetsApiClientPolicy;
        private readonly ITypeIdentifierGenerator _typeIdentifierGenerator;

        public ReferencedSpecificationReMapService(ICalcsResiliencePolicies resiliencePolicies,
            ISpecificationsApiClient specificationsApiClient,
            ICalculationsRepository calculationsRepository,
            IPoliciesApiClient policiesApiClient,
            IDatasetsApiClient datasetsApiClient,
            ICalculationCodeReferenceUpdate calculationCodeReferenceUpdate,
            IJobManagement jobManagement,
            ILogger logger) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(calculationCodeReferenceUpdate, nameof(calculationCodeReferenceUpdate));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(datasetsApiClient, nameof(datasetsApiClient));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(calculationsRepository, nameof(calculationsRepository));
            Guard.ArgumentNotNull(resiliencePolicies.SpecificationsApiClient, nameof(resiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies.PoliciesApiClient, nameof(resiliencePolicies.PoliciesApiClient));
            Guard.ArgumentNotNull(resiliencePolicies.CalculationsRepository, nameof(resiliencePolicies.CalculationsRepository));
            Guard.ArgumentNotNull(resiliencePolicies.DatasetsApiClient, nameof(resiliencePolicies.DatasetsApiClient));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _specificationsApiClientPolicy = resiliencePolicies.SpecificationsApiClient;
            _policiesApiClientPolicy = resiliencePolicies.PoliciesApiClient;
            _calculationsRepositoryPolicy = resiliencePolicies.CalculationsRepository;
            _datasetsApiClientPolicy = resiliencePolicies.DatasetsApiClient;
            _specificationsApiClient = specificationsApiClient;
            _policiesApiClient = policiesApiClient;
            _datasetsApiClient = datasetsApiClient;
            _calculationsRepository = calculationsRepository;
            _typeIdentifierGenerator = new VisualBasicTypeIdentifierGenerator();
            _calculationCodeReferenceUpdate = calculationCodeReferenceUpdate;
            _jobManagement = jobManagement;
            _logger = logger;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth calcsRepoHealth = await ((IHealthChecker)_calculationsRepository).IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ReferencedSpecificationReMapService)
            };
            health.Dependencies.AddRange(calcsRepoHealth.Dependencies);

            return health;
        }

        public override async Task Process(Message message)
        {
            string specificationId = message.GetUserProperty<string>(SpecificationId);
            string datasetSpecificationRelationshipId = message.GetUserProperty<string>(DatasetSpecificationRelationshipId);

            Guard.ArgumentNotNull(specificationId, SpecificationId);
            Guard.ArgumentNotNull(datasetSpecificationRelationshipId, DatasetSpecificationRelationshipId);

            bool relationshipChanges = false;

            ApiResponse<SpecificationSummary> specificationSummaryResponse = await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(specificationId));

            if (specificationSummaryResponse != null && specificationSummaryResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new EntityNotFoundException($"Specification with ID '{specificationId}' was not found");
            }
            else if (specificationSummaryResponse?.Content == null)
            {
                _logger.Error($"Failed to get specification for specification id '{specificationId}'");

                throw new ArgumentNullException(nameof(SpecificationSummary));
            }

            SpecificationSummary specificationSummary = specificationSummaryResponse.Content;

            ApiResponse<IEnumerable<DatasetSpecificationRelationshipViewModel>> datasetRelationshipResponse = await _datasetsApiClientPolicy.ExecuteAsync(
                () => _datasetsApiClient.GetReferenceRelationshipsBySpecificationId(specificationId));

            if (datasetRelationshipResponse?.Content == null || 
                !datasetRelationshipResponse.Content.Any(_ => _.Id == datasetSpecificationRelationshipId))
            {
                string errorMessage = $"No dataset relationships returned for {specificationId}";
                _logger.Error(errorMessage);

                throw new EntityNotFoundException($"Dataset specification relationship with id '{datasetSpecificationRelationshipId}' was not found");
            }

            DatasetSpecificationRelationshipViewModel definitionSpecificationRelationship = datasetRelationshipResponse.Content.FirstOrDefault(_ => _.Id == datasetSpecificationRelationshipId);

            
            Dictionary<uint, PublishedSpecificationItem> calculations = definitionSpecificationRelationship.PublishedSpecificationConfiguration?.Calculations?.ToDictionary(_ => _.TemplateId);
            Dictionary<uint, PublishedSpecificationItem> fundingLines = definitionSpecificationRelationship.PublishedSpecificationConfiguration?.FundingLines?.ToDictionary(_ => _.TemplateId);
            IEnumerable<Models.Calcs.Calculation> sourceCalculations = await _calculationsRepositoryPolicy.ExecuteAsync(
                             () => _calculationsRepository.GetCalculationsBySpecificationId(specificationId));

            foreach (Reference fundingStream in specificationSummary.FundingStreams)
            {
                 TemplateMapping templateMapping = await _calculationsRepositoryPolicy.ExecuteAsync(
                             () => _calculationsRepository.GetTemplateMapping(specificationId, fundingStream.Id));

                if (templateMapping == null)
                {
                    string errorMessage = $"Did not locate Template Mapping for funding stream id {fundingStream.Id} and specification id {specificationId}";
                    _logger.Error(errorMessage);
                    throw new Exception(errorMessage);
                }

                ApiResponse<TemplateMetadataContents> templateMetadataContentsResponse = await _policiesApiClientPolicy.ExecuteAsync(() => 
                    _policiesApiClient.GetFundingTemplateContents(fundingStream.Id,
                        specificationSummary.FundingPeriod.Id,
                        specificationSummary.TemplateIds[fundingStream.Id]
                    )
                );

                if (templateMetadataContentsResponse?.Content == null)
                {
                    string errorMessage = $"Did not locate Template Metadata Contents for funding stream id {fundingStream.Id}, funding period id {specificationSummary.FundingPeriod.Id} and template version {specificationSummary.TemplateIds[fundingStream.Id]}";
                    _logger.Error(errorMessage);
                    throw new Exception(errorMessage);
                }

                TemplateMetadataContents templateMetadataContents = templateMetadataContentsResponse.Content;

                Dictionary<uint, TemplateMappingItem> templateCalculations = templateMapping.TemplateMappingItems.ToDictionary(_ => _.TemplateId);

                templateCalculations.ForEach(_ =>
                {
                    // if the dataset relationship has a calculation which has changed its name then we need to update the source code
                    if (calculations != null && calculations.ContainsKey(_.Key) && calculations[_.Key].Name != _.Value.Name)
                    {
                        relationshipChanges = true;
                        string newSouceCodeName = _typeIdentifierGenerator.GenerateIdentifier(_.Value.Name);
                        sourceCalculations.ForEach(calc =>
                        {
                            calc.Current.SourceCode = _calculationCodeReferenceUpdate.ReplaceSourceCodeReferences(calc.Current.SourceCode, calculations[_.Key].SourceCodeName, newSouceCodeName);
                        });
                    }
                });

                Dictionary<uint, FundingLine> templateFundingLines = templateMetadataContents.RootFundingLines.Flatten(_ => _.FundingLines).DistinctBy(_ => _.TemplateLineId).ToDictionary(_ => _.TemplateLineId);

                templateFundingLines.ForEach(_ =>
                {
                    // if the dataset relationship has a calculation which has changed its name then we need to update the source code
                    if (fundingLines != null && fundingLines.ContainsKey(_.Key) && fundingLines[_.Key].Name != _.Value.Name)
                    {
                        relationshipChanges = true;
                        string newSouceCodeName = _typeIdentifierGenerator.GenerateIdentifier(_.Value.Name);
                        sourceCalculations.ForEach(calc =>
                        {
                            calc.Current.SourceCode = _calculationCodeReferenceUpdate.ReplaceSourceCodeReferences(calc.Current.SourceCode, fundingLines[_.Key].SourceCodeName, newSouceCodeName);
                        });
                    }
                });
            }

            // only update the dataset relationship if a calculation name has changed or a funding line name has changed
            if (relationshipChanges)
            {
                // persist updated relationship
                ValidatedApiResponse<DefinitionSpecificationRelationshipVersion> validatedApiResponse = await _datasetsApiClientPolicy.ExecuteAsync(
                                 () => _datasetsApiClient.UpdateDefinitionSpecificationRelationship(
                                         new UpdateDefinitionSpecificationRelationshipModel
                                         {
                                             CalculationIds = definitionSpecificationRelationship.PublishedSpecificationConfiguration.Calculations.Select(_ => _.TemplateId),
                                             Description = definitionSpecificationRelationship.RelationshipDescription,
                                             FundingLineIds = definitionSpecificationRelationship.PublishedSpecificationConfiguration.FundingLines.Select(_ => _.TemplateId)
                                         },
                                         specificationId,
                                         definitionSpecificationRelationship.Id
                                 ));

                if (validatedApiResponse?.Content == null || !validatedApiResponse.StatusCode.IsSuccess())
                {
                    throw new NonRetriableException(
                        $"Unable to update definition specification relationship id {definitionSpecificationRelationship.Id}");
                }

                // persist updated calculations
                await _calculationsRepositoryPolicy.ExecuteAsync(
                                 () => _calculationsRepository.UpdateCalculations(sourceCalculations));
            }
        }

        public async Task<IActionResult> QueueReferencedSpecificationReMapJobs(string specificationId, Reference user, string correlationId)
        {
            List<Job> jobs = new List<Job>();

            ApiResponse<IEnumerable<DatasetSpecificationRelationshipViewModel>> datasetRelationshipsResponse = await _datasetsApiClientPolicy.ExecuteAsync(
                () => _datasetsApiClient.GetReferenceRelationshipsBySpecificationId(specificationId));

            if (datasetRelationshipsResponse?.Content == null)
            {
                string message = $"No dataset relationships returned for {specificationId}";
                _logger.Information(message);

                return new NotFoundResult();
            }

            foreach (DatasetSpecificationRelationshipViewModel datasetSpecificationRelationshipViewModel in datasetRelationshipsResponse.Content)
            {
                jobs.Add(await _jobManagement.QueueJob(new JobCreateModel
                {
                    JobDefinitionId = JobConstants.DefinitionNames.ReferencedSpecificationReMapJob,
                    SpecificationId = specificationId,
                    InvokerUserId = user.Id,
                    InvokerUserDisplayName = user.Name,
                    CorrelationId = correlationId,
                    Properties = new Dictionary<string, string>
                    {
                        {"specification-id", specificationId},
                        {"dataset-specification-relationship-id", datasetSpecificationRelationshipViewModel.Id}
                    },
                    Trigger = new Trigger
                    {
                        EntityId = specificationId,
                        EntityType = "Specification",
                        Message = "Specification relationship changed"
                    }
                }));
            }

            return new OkObjectResult(jobs);
        }
    }
}
