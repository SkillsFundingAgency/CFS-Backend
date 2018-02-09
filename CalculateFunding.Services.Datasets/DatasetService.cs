using AutoMapper;
using CalculateFunding.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets
{
    public class DatasetService : IDatasetService
    {
        private readonly IBlobClient _blobClient;
        private readonly ILogger _logger;
        private readonly IDatasetRepository _datasetRepository;
        private readonly IValidator<CreateNewDatasetModel> _createNewDatasetModelValidator;
        private readonly IMapper _mapper;
        private readonly IValidator<DatasetMetadataModel> _datasetMetadataModelValidator;
        private readonly ISearchRepository<DatasetIndex> _searchRepository;
        private readonly IValidator<GetDatasetBlobModel> _getDatasetBlobModelValidator;

        public DatasetService(IBlobClient blobClient, ILogger logger,
            IDatasetRepository datasetRepository, IValidator<CreateNewDatasetModel> createNewDatasetModelValidator,
            IMapper mapper, IValidator<DatasetMetadataModel> datasetMetadataModelValidator,
            ISearchRepository<DatasetIndex> searchRepository, IValidator<GetDatasetBlobModel> getDatasetBlobModelValidator)
        {
            _blobClient = blobClient;
            _logger = logger;
            _datasetRepository = datasetRepository;
            _createNewDatasetModelValidator = createNewDatasetModelValidator;
            _mapper = mapper;
            _datasetMetadataModelValidator = datasetMetadataModelValidator;
            _searchRepository = searchRepository;
            _getDatasetBlobModelValidator = getDatasetBlobModelValidator;
        }

        async public Task<IActionResult> CreateNewDataset(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            CreateNewDatasetModel model = JsonConvert.DeserializeObject<CreateNewDatasetModel>(json);

            if (model == null)
            {
                _logger.Error("Null model name was provided to CreateNewDataset");
                return new BadRequestObjectResult("Null model name was provided");
            }
            var validationResult = (await _createNewDatasetModelValidator.ValidateAsync(model)).PopulateModelState();

            if (validationResult != null)
                return validationResult;

            string version = "v1";

            string datasetId = Guid.NewGuid().ToString();

            string fileName = $"{datasetId}/{version}/{model.Filename}";

            string blobUrl = _blobClient.GetBlobSasUrl(fileName,
                DateTimeOffset.UtcNow.AddDays(1), SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write);

            CreateNewDatasetResponseModel responseModel = _mapper.Map<CreateNewDatasetResponseModel>(model);

            responseModel.DatasetId = datasetId;
            responseModel.BlobUrl = blobUrl;
            responseModel.Author = request.GetUser();

            return new OkObjectResult(responseModel);
        }

        async public Task<IActionResult> GetDatasetByName(HttpRequest request)
        {
            request.Query.TryGetValue("datasetName", out var dsName);

            var datasetName = dsName.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(datasetName))
            {
                _logger.Error("No dataset name was provided to GetDatasetByName");

                return new BadRequestObjectResult("Null or empty dataset name provided");
            }

            IEnumerable<Dataset> datasets = await _datasetRepository.GetDatasetsByQuery(m => m.Name.ToLower() == datasetName.ToLower());

            if (!datasets.Any())
            {
                _logger.Information($"Dataset was not found for name: {datasetName}");

                return new NotFoundResult();
            }

            _logger.Information($"Dataset found for name: {datasetName}");

            return new OkObjectResult(datasets.FirstOrDefault());
        }

        async public Task<IActionResult> ValidateDataset(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            GetDatasetBlobModel model = JsonConvert.DeserializeObject<GetDatasetBlobModel>(json);

            if (model == null)
            {
                _logger.Error("Null model name was provided to ValidateDataset");
                return new BadRequestObjectResult("Null model name was provided");
            }

            var validationResult = (await _getDatasetBlobModelValidator.ValidateAsync(model)).PopulateModelState();

            if (validationResult != null)
                return validationResult;

            string fullBlobName = model.ToString();

            ICloudBlob blob = await _blobClient.GetBlobReferenceFromServerAsync(fullBlobName);

            if (blob == null)
            {
                _logger.Error($"Failed to find blob with path: {fullBlobName}");
                return new StatusCodeResult(412);
            }

            //TODO: Validate the data set here

            if (model.Version == 1)
            {
                try
                {
                    await SaveNewDataset(blob);
                }
                catch (Exception exception)
                {
                    _logger.Error(exception, "Failed to save the new dataset");
                    return new StatusCodeResult(500);
                }
            }

            return new OkResult();
        }

        async public Task SaveNewDataset(ICloudBlob blob)
        {
            Guard.ArgumentNotNull(blob, nameof(blob));

            IDictionary<string, string> metadata = blob.Metadata;

            Guard.ArgumentNotNull(metadata, nameof(metadata));

            DatasetMetadataModel metadataModel = new DatasetMetadataModel(metadata);

            var validationResult = await _datasetMetadataModelValidator.ValidateAsync(metadataModel);

            if (!validationResult.IsValid)
            {
                _logger.Error($"Invalid metadata on blob: {blob.Name}");

                throw new Exception($"Invalid metadata on blob: {blob.Name}");
            }

            DatasetDefinition datasetDefinition =
                (await _datasetRepository.GetDatasetDefinitionsByQuery(m => m.Id == metadataModel.DataDefinitionId)).FirstOrDefault();

            if (datasetDefinition == null)
            {
                _logger.Error($"Unable to find a data definition for id: {metadataModel.DataDefinitionId}, for blob: {blob.Name}");

                throw new Exception($"Unable to find a data definition for id: {metadataModel.DataDefinitionId}, for blob: {blob.Name}");
            }

            DatasetVersion newVersion = new DatasetVersion
            {
                Author = new Reference(metadataModel.AuthorId, metadataModel.AuthorId),
                Version = 1,
                Date = DateTime.UtcNow,
                PublishStatus = PublishStatus.Draft
            };

            Dataset dataset = new Dataset
            {
                Id = metadataModel.DatasetId,
                Name = metadataModel.Name,
                Description = metadataModel.Description,
                Definition = new Reference(datasetDefinition.Id, datasetDefinition.Name),
                Current = newVersion,
                History = new List<DatasetVersion>
                {
                    newVersion
                }
            };

            HttpStatusCode statusCode = await _datasetRepository.SaveDataset(dataset);

            if (!statusCode.IsSuccess())
            {
                _logger.Error($"Failed to save dataset for id: {metadataModel.DatasetId} with status code {statusCode.ToString()}");

                throw new Exception($"Failed to save dataset for id: {metadataModel.DatasetId} with status code {statusCode.ToString()}");
            }

            IEnumerable<IndexError> indexErrors = await AddNewDatasetToSearch(dataset);

            if (indexErrors.Any())
            {
                string errors = string.Join(";", indexErrors.Select(m => m.ErrorMessage).ToArraySafe());

                _logger.Error($"Failed to save dataset for id: {metadataModel.DatasetId} in search with errors {errors}");

                throw new Exception($"Failed to save dataset for id: {metadataModel.DatasetId} in search with errors {errors}");
            }
        }

        async Task<IEnumerable<IndexError>> AddNewDatasetToSearch(Dataset dataset)
        {
            return await _searchRepository.Index(new List<DatasetIndex>
            {
                new DatasetIndex
                {
                    Id = dataset.Id,
                    Name = dataset.Name,
                    DefinitionId = dataset.Definition.Id,
                    DefinitionName = dataset.Definition.Name,
                    Status = dataset.Current.PublishStatus.ToString(),
                    LastUpdatedDate = DateTimeOffset.Now
                }
            });
        }
    }
}
