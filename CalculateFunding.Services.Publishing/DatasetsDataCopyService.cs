using CalculateFunding.Common.ApiClient.DataSets;
using CalculateFunding.Common.ApiClient.DataSets.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Processing;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FundingLine = CalculateFunding.Models.Publishing.FundingLine;

namespace CalculateFunding.Services.Publishing
{
    public class DatasetsDataCopyService : JobProcessingService, IDatasetsDataCopyService
    {
        private readonly IDatasetsApiClient _datasetsApiClient;
        private readonly ISpecificationService _specificationService;
        private readonly IAsyncPolicy _datasetsApiClientPolicy;
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly ILogger _logger;

        public DatasetsDataCopyService(
            IJobManagement jobManagement,
            ILogger logger,
            IDatasetsApiClient datasetsApiClient,
            ISpecificationService specificationService,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            IPublishedFundingRepository publishedFundingRepository) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(datasetsApiClient, nameof(datasetsApiClient));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(publishingResiliencePolicies?.DatasetsApiClient, nameof(publishingResiliencePolicies.DatasetsApiClient));
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            
            _logger = logger;
            _datasetsApiClient = datasetsApiClient;
            _specificationService = specificationService;
            _datasetsApiClientPolicy = publishingResiliencePolicies.DatasetsApiClient;
            _publishedFundingRepository = publishedFundingRepository;
        }

        public override async Task Process(Message message)
        {
            string specificationId = message.GetUserProperty<string>("specification-id");
            string relationshipId = message.GetUserProperty<string>("relationship-id");

            Guard.ArgumentNotNull(specificationId, nameof(specificationId));

            SpecificationSummary specificationSummary = await _specificationService.GetSpecificationSummaryById(specificationId);

            if (specificationSummary == null)
            {
                LogAndThrowException($"Specification not found for specification id- {specificationId}", true);
            }

            IEnumerable<DatasetSpecificationRelationshipViewModel> relationships = await GetDatasetSpecificationRelationship(specificationId);

            foreach (DatasetSpecificationRelationshipViewModel relationship in string.IsNullOrWhiteSpace(relationshipId) ? relationships : relationships.Where(_ => _.Id == relationshipId))
            {
                List<RelationshipDataSetExcelData> excelDataItems = await GetRelationshipDatasetExcelData(specificationId, relationship);

                // there are no published providers so exit early
                if (excelDataItems.IsNullOrEmpty())
                {
                    return;
                }

                NewDatasetVersionResponseModel datasetVersion = null;

                if (string.IsNullOrEmpty(relationship.DatasetId))
                {
                    CreateNewDatasetModel createNewDatasetModel = new CreateNewDatasetModel
                    {
                        DefinitionId = relationship.Definition?.Id,
                        Filename = $"{relationship.Name}.xlsx",
                        Description = relationship.RelationshipDescription,
                        Name = $"{relationship.Name}-{relationship.TargetSpecificationId ?? specificationId}",
                        FundingStreamId = specificationSummary.FundingStreams?.First().Id,
                        RowCount = excelDataItems.Count
                    };

                    ValidatedApiResponse<NewDatasetVersionResponseModel> datasetCreateResponse =
                        await _datasetsApiClientPolicy.ExecuteAsync(() => _datasetsApiClient.CreateAndPersistNewDataset(createNewDatasetModel));

                    if (!datasetCreateResponse.StatusCode.IsSuccess() || datasetCreateResponse.Content == null)
                    {
                        LogAndThrowException($"Failed to create a dataset, relationship id - {relationship.Id}. Status Code - {datasetCreateResponse.StatusCode}");
                    }

                    datasetVersion = datasetCreateResponse.Content;
                }
                else
                {
                    DatasetVersionUpdateModel datasetVersionUpdateModel = new DatasetVersionUpdateModel()
                    {
                        DatasetId = relationship.DatasetId,
                        Filename = $"{relationship.Name}.xlsx",
                        FundingStreamId = specificationSummary.FundingStreams.First().Id
                    };
                    ValidatedApiResponse<NewDatasetVersionResponseModel> datasetUpdateResponse =
                        await _datasetsApiClientPolicy.ExecuteAsync(() => _datasetsApiClient.DatasetVersionUpdateAndPersist(datasetVersionUpdateModel));

                    if (!datasetUpdateResponse.StatusCode.IsSuccess() || datasetUpdateResponse.Content == null)
                    {
                        LogAndThrowException($"Failed to update the dataset version, dataset id - {relationship.DatasetId}. Status Code - {datasetUpdateResponse.StatusCode}");
                    }

                    datasetVersion = datasetUpdateResponse.Content;
                }

                DatasetMetadataViewModel datasetMetadataViewModel = new DatasetMetadataViewModel()
                {
                    AuthorId = datasetVersion.Author?.Id,
                    AuthorName = datasetVersion.Author?.Name,
                    DatasetId = datasetVersion.DatasetId,
                    Description = datasetVersion.Description,
                    FundingStreamId = datasetVersion.FundingStreamId,
                    Filename = datasetVersion.Filename,
                    Name = datasetVersion.Name,
                    Version = datasetVersion.Version,
                    ExcelData = excelDataItems
                };

                HttpStatusCode fileUploadStatus = await _datasetsApiClientPolicy.ExecuteAsync(() => _datasetsApiClient.UploadDatasetFile(datasetVersion.Filename, datasetMetadataViewModel));

                if (!fileUploadStatus.IsSuccess())
                {
                    LogAndThrowException($"Failed to upload the dataset file, dataset id - {datasetVersion.DatasetId}, file name - {datasetVersion.Filename}. Status Code - {fileUploadStatus}");
                }

                AssignDatasourceModel assignDatasourceModel = new AssignDatasourceModel()
                {
                    DatasetId = datasetVersion.DatasetId,
                    RelationshipId = relationship.Id,
                    Version = datasetVersion.Version
                };

                ApiResponse<Common.ApiClient.Datasets.Models.JobCreationResponse> assignDatasourceVersionResponse = await _datasetsApiClientPolicy.ExecuteAsync(() => _datasetsApiClient.AssignDatasourceVersionToRelationship(assignDatasourceModel));

                if (!assignDatasourceVersionResponse.StatusCode.IsSuccess() || assignDatasourceVersionResponse.Content == null)
                {
                    LogAndThrowException($"Failed to assign datasource version to relationship, dataset id - {datasetVersion.DatasetId}, relationship id - {relationship.Id}, version - {datasetVersion.Version}. Status Code - {assignDatasourceVersionResponse.StatusCode}");
                }
            }
        }

        private async Task<List<RelationshipDataSetExcelData>> GetRelationshipDatasetExcelData(string specificationId, DatasetSpecificationRelationshipViewModel relationship)
        {
            List<RelationshipDataSetExcelData> excelDataItems = new List<RelationshipDataSetExcelData>();

            await _publishedFundingRepository.PublishedProviderBatchProcessing("IS_NULL(c.content.released) = false",
                specificationId,
                publishedProviders =>
                {
                    foreach (PublishedProvider publishedProvider in publishedProviders)
                    {
                        PublishedProviderVersion publishedProviderVersion = publishedProvider.Released;
                        RelationshipDataSetExcelData excelDataItem = new RelationshipDataSetExcelData(publishedProviderVersion.Provider.UKPRN);

                        if (relationship.PublishedSpecificationConfiguration != null &&
                            relationship.PublishedSpecificationConfiguration.FundingLines.AnyWithNullCheck())
                        {
                            foreach (PublishedSpecificationItem item in relationship.PublishedSpecificationConfiguration.FundingLines)
                            {
                                FundingLine fundingLine = publishedProviderVersion.FundingLines.FirstOrDefault(f => f.TemplateLineId == item.TemplateId);
                                excelDataItem.FundingLines.Add($"{CodeGenerationDatasetTypeConstants.FundingLinePrefix}_{item.TemplateId}_{item.Name}", fundingLine?.Value);
                            }
                        }

                        if (relationship.PublishedSpecificationConfiguration != null &&
                            relationship.PublishedSpecificationConfiguration.Calculations.AnyWithNullCheck())
                        {
                            foreach (PublishedSpecificationItem item in relationship.PublishedSpecificationConfiguration.Calculations)
                            {
                                FundingCalculation calculation = publishedProviderVersion.Calculations.FirstOrDefault(f => f.TemplateCalculationId == item.TemplateId);
                                excelDataItem.Calculations.Add($"{CodeGenerationDatasetTypeConstants.CalculationPrefix}_{item.TemplateId}_{item.Name}", calculation?.Value);
                            }
                        }

                        excelDataItems.Add(excelDataItem);
                    }

                    return Task.CompletedTask;
                },
                100);

            return excelDataItems;
        }

        private async Task<IEnumerable<DatasetSpecificationRelationshipViewModel>> GetDatasetSpecificationRelationship(string specificationId)
        {
            ApiResponse<IEnumerable<DatasetSpecificationRelationshipViewModel>> relationshipsApiResponse = await _datasetsApiClientPolicy.ExecuteAsync(
                            () => _datasetsApiClient.GetReferenceRelationshipsBySpecificationId(specificationId));

            if (!relationshipsApiResponse.StatusCode.IsSuccess() || relationshipsApiResponse.Content == null)
            {
                LogAndThrowException($"Failed to retrieve referenced relationships for the specificaiton - {specificationId}. Status Code - {relationshipsApiResponse.StatusCode}");
            }

            return relationshipsApiResponse.Content;
        }

        private void LogAndThrowException(string errorMessage, bool isNonRetriable = false)
        {
            _logger.Error(errorMessage);
            throw isNonRetriable ? new NonRetriableException(errorMessage) : new Exception(errorMessage);
        }
    }
}
