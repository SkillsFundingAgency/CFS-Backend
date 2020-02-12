using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.DataImporter.Validators.Models;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using OfficeOpenXml;
using Serilog;
using BadRequestObjectResult = Microsoft.AspNetCore.Mvc.BadRequestObjectResult;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace CalculateFunding.Services.Datasets.Services
{
    [TestClass]
    public class DatasetsServiceValidateDatasetTests : DatasetServiceTestsBase
    {
        [TestMethod]
        public async Task ValidateDataset_GivenNullModel_ReturnsBadRequest()
        {
            //Arrange
            ILogger logger = CreateLogger();

            DatasetService service = CreateDatasetService(logger: logger);

            // Act
            IActionResult result = await service.ValidateDataset(null, null, null);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("Null model name was provided to ValidateDataset"));
        }

        [TestMethod]
        public async Task ValidateDataset_GivenInvalidModel_ReturnsBadRequest()
        {
            //Arrange
            GetDatasetBlobModel model = new GetDatasetBlobModel();

            ILogger logger = CreateLogger();

            ValidationResult validationResult = new ValidationResult(new[]{
                    new ValidationFailure("prop1", "any error")
                });

            IValidator<GetDatasetBlobModel> validator = CreateGetDatasetBlobModelValidator(validationResult);


            DatasetService service = CreateDatasetService(logger: logger, getDatasetBlobModelValidator: validator);

            // Act
            IActionResult result = await service.ValidateDataset(model, null, null);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task ValidateDataset_GivenModelButBlobNotFound_ReturnsPreConditionFailed()
        {
            //Arrange
            GetDatasetBlobModel getDatasetBlobModel = NewGetDatasetBlobModel();

            string blobPath = $"{getDatasetBlobModel.DatasetId}/v{getDatasetBlobModel.Version}/{getDatasetBlobModel.Filename}";

            ILogger logger = CreateLogger();

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Is(blobPath))
                .Returns((ICloudBlob)null);

            DatasetService service = CreateDatasetService(logger: logger, blobClient: blobClient);

            // Act
            IActionResult result = await service.ValidateDataset(getDatasetBlobModel, null, null);

            // Assert
            result
                .Should()
                .BeOfType<PreconditionFailedResult>()
                .Which
                .Value
                .Should()
                .Be($"Failed to find blob with path: {blobPath}");

            logger
                .Received(1)
                .Error($"Failed to find blob with path: {blobPath}");
        }

        [TestMethod]
        public async Task ValidateDataset_GivenModelButAndBlobFoundButBlobHasNoData_ReturnsPreConditionFailed()
        {
            //Arrange
            GetDatasetBlobModel getDatasetBlobModel = NewGetDatasetBlobModel();

            string blobPath = $"{getDatasetBlobModel.DatasetId}/v{getDatasetBlobModel.Version}/{getDatasetBlobModel.Filename}";

            ILogger logger = CreateLogger();

            IDictionary<string, string> metaData = new Dictionary<string, string>
            {
                { "dataDefinitionId", DataDefintionId }
            };

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);
            blob
                .Name
                .Returns(blobPath);

            MemoryStream memoryStream = new MemoryStream(new byte[0]);

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Is(blobPath))
                .Returns(blob);
            blobClient
               .DownloadToStreamAsync(Arg.Is(blob))
               .Returns(memoryStream);

            DatasetDefinition datasetDefinition = new DatasetDefinition
            {
                Id = DataDefintionId
            };

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                datasetDefinition
            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DocumentEntity<DatasetDefinition>, bool>>>())
                .Returns(datasetDefinitions);

            DatasetService service = CreateDatasetService(logger: logger, blobClient: blobClient, datasetRepository: datasetRepository);

            // Act
            IActionResult result = await service.ValidateDataset(getDatasetBlobModel, null, null);

            // Assert
            result
                .Should()
                .BeOfType<PreconditionFailedResult>()
                .Which
                .Value
                .Should()
                .Be($"Blob {blobPath} contains no data");

            logger
                .Received(1)
                .Error(Arg.Is($"Blob {blobPath} contains no data"));
        }

        [TestMethod]
        public async Task ValidateDataset_GivenModelButAndBlobDataDefinitionNotFound_ReturnsPreConditionFailed()
        {
            // Arrange
            GetDatasetBlobModel getDatasetBlobModel = NewGetDatasetBlobModel();

            string blobPath = $"{getDatasetBlobModel.DatasetId}/v{getDatasetBlobModel.Version}/{getDatasetBlobModel.Filename}";

            ILogger logger = CreateLogger();

            IDictionary<string, string> metaData = new Dictionary<string, string>
            {
                { "dataDefinitionId", DataDefintionId }
            };

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);

            MemoryStream memoryStream = new MemoryStream(CreateTestExcelPackage());

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Is(blobPath))
                .Returns(blob);
            blobClient
               .DownloadToStreamAsync(Arg.Is(blob))
               .Returns(memoryStream);

            IEnumerable<DatasetDefinition> datasetDefinitions = Enumerable.Empty<DatasetDefinition>();

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DocumentEntity<DatasetDefinition>, bool>>>())
                .Returns(datasetDefinitions);

            DatasetService service = CreateDatasetService(logger: logger, blobClient: blobClient, datasetRepository: datasetRepository);

            // Act
            IActionResult result = await service.ValidateDataset(getDatasetBlobModel, null, null);

            // Assert
            result
                .Should()
                .BeOfType<PreconditionFailedResult>()
                .Which
                .Value
                .Should()
                .Be($"Unable to find a data definition for id: {DataDefintionId}, for blob: {blobPath}");

            logger
                .Received(1)
                .Error(Arg.Is($"Unable to find a data definition for id: {DataDefintionId}, for blob: {blobPath}"));
        }

        [TestMethod]
        public async Task OnValidateDataset_GivenTableResultsContainsOneError_EnsuresDatasetValidationModelIsWrittenToCache()
        {
            //Arrange
            GetDatasetBlobModel getDatasetBlobModel = NewGetDatasetBlobModel();

            Message message = GetValidateDatasetMessage(getDatasetBlobModel);

            string blobPath = $"{getDatasetBlobModel.DatasetId}/v{getDatasetBlobModel.Version}/{getDatasetBlobModel.Filename}";

            ILogger logger = CreateLogger();

            IDictionary<string, string> metaData = new Dictionary<string, string>
            {
                { "dataDefinitionId", DataDefintionId }
            };

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);

            MemoryStream memoryStream = new MemoryStream(CreateTestExcelPackage());

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Is(blobPath))
                .Returns(blob);
            blobClient
                .DownloadToStreamAsync(Arg.Is(blob))
                .Returns(memoryStream);

            DatasetDefinition datasetDefinition = new DatasetDefinition
            {
                Id = DataDefintionId
            };

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                datasetDefinition
            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DocumentEntity<DatasetDefinition>, bool>>>())
                .Returns(datasetDefinitions);

            List<DatasetValidationError> errors = new List<DatasetValidationError>
            {
                new DatasetValidationError { ErrorMessage = "error" }
            };

            ValidationResult validationResult = new ValidationResult(new[]{
                new ValidationFailure("prop1", "any error")
            });

            IValidator<DatasetUploadValidationModel> datasetUploadValidator = CreateDatasetUploadValidator(validationResult);

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult{ GlobalErrors = errors }
            };

            ApiResponse<ProviderVersion> providerVersionResponse = new ApiResponse<ProviderVersion>(HttpStatusCode.OK, new ProviderVersion
            {
                Providers = new[]
                {
                    new Common.ApiClient.Providers.Models.Provider()
                }
            });

            IProvidersApiClient providersApiClient = CreateProvidersApiClient();
            providersApiClient
                .GetAllMasterProviders()
                .Returns(providerVersionResponse);

            ICacheProvider cacheProvider = CreateCacheProvider();

            IMapper mapper = CreateMapper();

            DatasetService service = CreateDatasetService(
                logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                datasetUploadValidator: datasetUploadValidator,
                cacheProvider: cacheProvider,
                providersApiClient: providersApiClient,
                mapper: mapper);

            // Act
            await service.ValidateDataset(message);

            // Assert     
            mapper
                .Received(1)
                .Map<Models.ProviderLegacy.ProviderSummary>(Arg.Any<Common.ApiClient.Providers.Models.Provider>());

            await cacheProvider
                .Received(1)
                .SetAsync(Arg.Is($"{CacheKeys.DatasetValidationStatus}:{message.UserProperties["operation-id"].ToString()}"), Arg.Is<DatasetValidationStatusModel>(v =>
                v.OperationId == message.UserProperties["operation-id"].ToString() &&
                v.ValidationFailures.Count == 3));
        }

        [TestMethod]
        public void OnValidateDataset_GivenNoProvidersReturned_ThrowsRetriableException()
        {
            //Arrange
            const string errorMessage = "Failed to fetch master providers with status code: BadRequest";

            GetDatasetBlobModel model = NewGetDatasetBlobModel();

            string blobPath = $"{model.DatasetId}/v{model.Version}/{model.Filename}";

            Message message = GetValidateDatasetMessage(model);

            ILogger logger = CreateLogger();

            IDictionary<string, string> metaData = new Dictionary<string, string>
            {
                { "dataDefinitionId", DataDefintionId }
            };

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);

            MemoryStream memoryStream = new MemoryStream(CreateTestExcelPackage());

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Is(blobPath))
                .Returns(blob);
            blobClient
                .DownloadToStreamAsync(Arg.Is(blob))
                .Returns(memoryStream);

            DatasetDefinition datasetDefinition = new DatasetDefinition
            {
                Id = DataDefintionId
            };

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                datasetDefinition
            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DocumentEntity<DatasetDefinition>, bool>>>())
                .Returns(datasetDefinitions);

            List<DatasetValidationError> errors = new List<DatasetValidationError>
            {
                new DatasetValidationError { ErrorMessage = "error" }
            };

            ValidationResult validationResult = new ValidationResult(new[]{
                new ValidationFailure("prop1", "any error")
            });

            IValidator<DatasetUploadValidationModel> datasetUploadValidator = CreateDatasetUploadValidator(validationResult);

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult{ GlobalErrors = errors }
            };

            IProvidersApiClient providersApiClient = CreateProvidersApiClient();
            providersApiClient
                .GetAllMasterProviders()
                .Returns(new ApiResponse<ProviderVersion>(HttpStatusCode.BadRequest));

            IMapper mapper = CreateMapper();

            DatasetService service = CreateDatasetService(
                logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                datasetUploadValidator: datasetUploadValidator,
                providersApiClient: providersApiClient,
                mapper: mapper);

            // Act
            Func<Task> result = () => service.ValidateDataset(message);

            // Assert
            result
               .Should()
               .ThrowExactly<RetriableException>()
               .Which
               .Message
               .Should()
               .Be(errorMessage);
        }

        [TestMethod]
        public async Task ValidateDataset_GivenDatasetBlobModel_CallsJobServiceToQueueJob()
        {
            // Arrange
            GetDatasetBlobModel model = NewGetDatasetBlobModel(_ => _.WithDefinitionId(DataDefintionId));

            string blobPath = $"{model.DatasetId}/v{model.Version}/{model.Filename}";

            ILogger logger = CreateLogger();

            IDictionary<string, string> metaData = new Dictionary<string, string>
            {
                { "dataDefinitionId", DataDefintionId }
            };

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);

            MemoryStream memoryStream = new MemoryStream(CreateTestExcelPackage());

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Is(blobPath))
                .Returns(blob);
            blobClient
               .DownloadToStreamAsync(Arg.Is(blob))
               .Returns(memoryStream);

            DatasetDefinition datasetDefinition = new DatasetDefinition()
            {
                Id = DataDefintionId,
            };

            IEnumerable<DatasetDefinition> datasetDefinitions = new List<DatasetDefinition>() { datasetDefinition };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DocumentEntity<DatasetDefinition>, bool>>>())
                .Returns(datasetDefinitions);

            ICacheProvider cacheProvider = CreateCacheProvider();

            IJobsApiClient jobsApiClient = CreateJobsApiClient();

            DatasetService service = CreateDatasetService(
                logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                cacheProvider: cacheProvider,
                jobsApiClient: jobsApiClient);

            // Act
            IActionResult result = await service.ValidateDataset(model, null, null);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<DatasetValidationStatusModel>()
                .Should()
                .NotBeNull();

            await cacheProvider
                .Received(1)
                .SetAsync<DatasetValidationStatusModel>(Arg.Is<string>(c => c.StartsWith(CacheKeys.DatasetValidationStatus) && c.Length > 40),
                Arg.Is<DatasetValidationStatusModel>(s => !string.IsNullOrWhiteSpace(s.OperationId) && s.CurrentOperation == DatasetValidationStatus.Queued));

            await jobsApiClient
                .Received(1)
                .CreateJob(Arg.Is<JobCreateModel>(j => j.JobDefinitionId == JobConstants.DefinitionNames.ValidateDatasetJob));
        }

        [TestMethod]
        public async Task ValidateDataset_GivenDatasetBlobModelWithAuthorMetadata_CallsJobServiceToQueueJobWithCorrectInvokerDetails()
        {
            // Arrange
            const string authorId = "user1";
            const string authorName = "user 1";

            GetDatasetBlobModel model = NewGetDatasetBlobModel(_ => _.WithDefinitionId(DataDefintionId));

            string blobPath = $"{model.DatasetId}/v{model.Version}/{model.Filename}";

            ILogger logger = CreateLogger();

            IDictionary<string, string> metaData = new Dictionary<string, string>
            {
                { "dataDefinitionId", DataDefintionId },
                { "authorId", authorId },
                { "authorName", authorName }
            };

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);

            MemoryStream memoryStream = new MemoryStream(CreateTestExcelPackage());

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Is(blobPath))
                .Returns(blob);
            blobClient
               .DownloadToStreamAsync(Arg.Is(blob))
               .Returns(memoryStream);

            DatasetDefinition datasetDefinition = new DatasetDefinition()
            {
                Id = DataDefintionId,
            };

            IEnumerable<DatasetDefinition> datasetDefinitions = new List<DatasetDefinition>() { datasetDefinition };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DocumentEntity<DatasetDefinition>, bool>>>())
                .Returns(datasetDefinitions);

            ICacheProvider cacheProvider = CreateCacheProvider();

            IJobsApiClient jobsApiClient = CreateJobsApiClient();

            DatasetService service = CreateDatasetService(
                logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                cacheProvider: cacheProvider,
                jobsApiClient: jobsApiClient);

            // Act
            IActionResult result = await service.ValidateDataset(model, null, null);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<DatasetValidationStatusModel>()
                .Should()
                .NotBeNull();

            await cacheProvider
                .Received(1)
                .SetAsync<DatasetValidationStatusModel>(Arg.Is<string>(c => c.StartsWith(CacheKeys.DatasetValidationStatus) && c.Length > 40),
                Arg.Is<DatasetValidationStatusModel>(s => !string.IsNullOrWhiteSpace(s.OperationId) && s.CurrentOperation == DatasetValidationStatus.Queued));

            await jobsApiClient
                .Received(1)
                .CreateJob(Arg.Is<JobCreateModel>(
                    j =>
                    j.JobDefinitionId == JobConstants.DefinitionNames.ValidateDatasetJob &&
                    j.InvokerUserId == authorId &&
                    j.InvokerUserDisplayName == authorName));
        }

        [TestMethod]
        public async Task OnValidateDataset_GivenTableResultsContainsThreeErrors_EnsuresValidationResultModelIsWrittenToCache()
        {
            //Arrange
            GetDatasetBlobModel model = NewGetDatasetBlobModel(_ => _.WithDefinitionId(DataDefintionId));

            string blobPath = $"{model.DatasetId}/v{model.Version}/{model.Filename}";

            Message message = GetValidateDatasetMessage(model);

            ILogger logger = CreateLogger();

            IDictionary<string, string> metaData = new Dictionary<string, string>
            {
                { "dataDefinitionId", DataDefintionId }
            };

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);

            MemoryStream memoryStream = new MemoryStream(CreateTestExcelPackage());

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Is(blobPath))
                .Returns(blob);
            blobClient
                .DownloadToStreamAsync(Arg.Is(blob))
                .Returns(memoryStream);

            DatasetDefinition datasetDefinition = new DatasetDefinition
            {
                Id = DataDefintionId
            };

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                datasetDefinition
            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DocumentEntity<DatasetDefinition>, bool>>>())
                .Returns(datasetDefinitions);

            List<DatasetValidationError> errors = new List<DatasetValidationError>
            {
                new DatasetValidationError { ErrorMessage = "error" },
                new DatasetValidationError { ErrorMessage = "error" },
                new DatasetValidationError { ErrorMessage = "error" }
            };

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult{ GlobalErrors = errors }
            };

            IExcelDatasetReader datasetReader = CreateExcelDatasetReader();
            datasetReader
                .Read(Arg.Any<Stream>(), Arg.Is(datasetDefinition))
                .Returns(tableLoadResults.ToList());

            ValidationResult validationResult = new ValidationResult(new[]{
                new ValidationFailure("prop1", "any error")
            });

            IValidator<DatasetUploadValidationModel> datasetUploadValidator = CreateDatasetUploadValidator(validationResult);

            ICacheProvider cacheProvider = CreateCacheProvider();

            ApiResponse<ProviderVersion> providerVersionResponse = new ApiResponse<ProviderVersion>(HttpStatusCode.OK, new ProviderVersion
            {
                Providers = new[]
               {
                    new Common.ApiClient.Providers.Models.Provider()
                }
            });

            IProvidersApiClient providersApiClient = CreateProvidersApiClient();
            providersApiClient
                .GetAllMasterProviders()
                .Returns(providerVersionResponse);

            DatasetService service = CreateDatasetService(
                logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                datasetUploadValidator: datasetUploadValidator,
                cacheProvider: cacheProvider,
                providersApiClient: providersApiClient);

            // Act
            Func<Task> test = async () => { await service.ValidateDataset(message); };

            // Assert
            test
                .Should()
                .NotThrow();

            await cacheProvider
                .Received(1)
                .SetAsync(Arg.Is($"{CacheKeys.DatasetValidationStatus}:{message.UserProperties["operation-id"].ToString()}"), Arg.Is<DatasetValidationStatusModel>(v =>
                v.OperationId == message.UserProperties["operation-id"].ToString() &&
                v.ValidationFailures.Count == 3));
        }

        [TestMethod]
        public void OnValidateDataset_GivenTableResultsAndMetadatValidatesButFailsToSave_ThrowsInValidOperationException()
        {
            //Arrange
            const string authorId = "author-id";
            const string authorName = "author-name";
            const string name = "name";
            const string description = "test description";

            GetDatasetBlobModel model = NewGetDatasetBlobModel();

            Message message = GetValidateDatasetMessage(model);

            string blobPath = $"{model.DatasetId}/v{model.Version}/{model.Filename}";

            IDictionary<string, string> metaData = new Dictionary<string, string>
                {
                    { "dataDefinitionId", model.DefinitionId },
                    { "authorName", authorName },
                    { "authorId", authorId },
                    { "datasetId", model.DatasetId },
                    { "name", name },
                    { "description", description },
                };

            ILogger logger = CreateLogger();

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);

            MemoryStream memoryStream = new MemoryStream(CreateTestExcelPackage());

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Is(blobPath))
                .Returns(blob);
            blobClient
                .DownloadToStreamAsync(Arg.Is(blob))
                .Returns(memoryStream);

            DatasetDefinition datasetDefinition = new DatasetDefinition
            {
                Id = DataDefintionId
            };

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                datasetDefinition
            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DocumentEntity<DatasetDefinition>, bool>>>())
                .Returns(datasetDefinitions);
            datasetRepository
               .SaveDataset(Arg.Any<Dataset>())
               .Returns(HttpStatusCode.InternalServerError);

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult{ GlobalErrors = new List<DatasetValidationError>() }
            };

            ApiResponse<ProviderVersion> providerVersionResponse = new ApiResponse<ProviderVersion>(HttpStatusCode.OK, new ProviderVersion
            {
                Providers = new[]
             {
                    new Common.ApiClient.Providers.Models.Provider()
                }
            });

            IProvidersApiClient providersApiClient = CreateProvidersApiClient();
            providersApiClient
                .GetAllMasterProviders()
                .Returns(providerVersionResponse);

            DatasetService service = CreateDatasetService(
                logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                providersApiClient: providersApiClient
                );

            // Act
            Func<Task> result = async () => { await service.ValidateDataset(message); };

            // Assert
            result
                .Should().Throw<InvalidOperationException>()
                .WithMessage($"Failed to save dataset for id: {model.DatasetId} with status code InternalServerError");

            logger
                .Received(1)
                .Error(Arg.Is($"Failed to save dataset for id: {model.DatasetId} with status code InternalServerError"));

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is("Failed to save the dataset or dataset version during validation"));
        }

        [TestMethod]
        public void OnValidateDataset_GivenTableResultsAndMetadatValidatesButFailsToSaveToSearch_ReturnsInternalServerError()
        {
            //Arrange
            const string authorId = "author-id";
            const string authorName = "author-name";
            const string name = "name";

            GetDatasetBlobModel model = NewGetDatasetBlobModel();

            string blobPath = $"{model.DatasetId}/v{model.Version}/{model.Filename}";

            Message message = GetValidateDatasetMessage(model);

            IDictionary<string, string> metaData = new Dictionary<string, string>
                {
                    { "dataDefinitionId", model.DefinitionId },
                    { "authorName", authorName },
                    { "authorId", authorId },
                    { "datasetId", model.DatasetId },
                    { "name", name },
                    { "description", model.Description },
                };

            ILogger logger = CreateLogger();

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);

            MemoryStream memoryStream = new MemoryStream(CreateTestExcelPackage());

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Is(blobPath))
                .Returns(blob);
            blobClient
                .DownloadToStreamAsync(Arg.Is(blob))
                .Returns(memoryStream);

            DatasetDefinition datasetDefinition = new DatasetDefinition
            {
                Id = DataDefintionId
            };

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                datasetDefinition
            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DocumentEntity<DatasetDefinition>, bool>>>())
                .Returns(datasetDefinitions);

            datasetRepository
                .SaveDataset(Arg.Any<Dataset>())
                .Returns(HttpStatusCode.OK);

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult{ GlobalErrors = new List<DatasetValidationError>() }
            };

            IExcelDatasetReader datasetReader = CreateExcelDatasetReader();
            datasetReader
                .Read(Arg.Any<Stream>(), Arg.Is(datasetDefinition))
                .Returns(tableLoadResults.ToList());

            List<IndexError> indexErrors = new List<IndexError>
            {
                new IndexError()
            };

            ApiResponse<ProviderVersion> providerVersionResponse = new ApiResponse<ProviderVersion>(HttpStatusCode.OK, new ProviderVersion
            {
                Providers = new[]
                {
                    new Common.ApiClient.Providers.Models.Provider(),
                    new Common.ApiClient.Providers.Models.Provider(),
                    new Common.ApiClient.Providers.Models.Provider()
                }
            });

            IProvidersApiClient providersApiClient = CreateProvidersApiClient();
            providersApiClient
                .GetAllMasterProviders()
                .Returns(providerVersionResponse);

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Index(Arg.Any<List<DatasetIndex>>())
                .Returns(indexErrors);

            DatasetService service = CreateDatasetService(
                logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                searchRepository: searchRepository,
                providersApiClient: providersApiClient);

            // Act
            Func<Task> result = async () => { await service.ValidateDataset(message); };

            // Assert
            result
                .Should().Throw<InvalidOperationException>()
                .WithMessage($"Failed to save dataset for id: {model.DatasetId} in search with errors ");
        }

        [TestMethod]
        public async Task OnValidateDataset_GivenTableResultsAndMetadataValidatesAndSavesWhenFirstDatasetVersion_ThenValidationSuccessful()
        {
            //Arrange
            const string authorId = "author-id";
            const string authorName = "author-name";
            const string name = "name";

            GetDatasetBlobModel model = NewGetDatasetBlobModel();

            string blobPath = $"{model.DatasetId}/v{model.Version}/{model.Filename}";

            Message message = GetValidateDatasetMessage(model);

            IDictionary<string, string> metaData = new Dictionary<string, string>
                {
                    { "dataDefinitionId", model.DefinitionId },
                    { "authorName", authorName },
                    { "authorId", authorId },
                    { "datasetId", model.DatasetId },
                    { "name", name },
                    { "description", model.Description },
                };

            ILogger logger = CreateLogger();

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);

            MemoryStream memoryStream = new MemoryStream(CreateTestExcelPackage());

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Is(blobPath))
                .Returns(blob);
            blobClient
                .DownloadToStreamAsync(Arg.Is(blob))
                .Returns(memoryStream);

            DatasetDefinition datasetDefinition = new DatasetDefinition
            {
                Id = model.DefinitionId
            };

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                datasetDefinition
            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DocumentEntity<DatasetDefinition>, bool>>>())
                .Returns(datasetDefinitions);

            datasetRepository
                .SaveDataset(Arg.Any<Dataset>())
                .Returns(HttpStatusCode.OK);

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult{ GlobalErrors = new List<DatasetValidationError>() }
            };

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();

            ICacheProvider cacheProvider = CreateCacheProvider();

            ApiResponse<ProviderVersion> providerVersionResponse = new ApiResponse<ProviderVersion>(HttpStatusCode.OK, new ProviderVersion
            {
                Providers = new[]
                {
                    new Common.ApiClient.Providers.Models.Provider()
                }
            });

            IProvidersApiClient providersApiClient = CreateProvidersApiClient();
            providersApiClient
                .GetAllMasterProviders()
                .Returns(providerVersionResponse);

            DatasetService service = CreateDatasetService(logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                searchRepository: searchRepository,
                cacheProvider: cacheProvider,
                providersApiClient: providersApiClient);

            // Act
            Func<Task> result = async () => { await service.ValidateDataset(message); };

            // Assert
            result
                .Should().NotThrow();

            // Ensure initial version is set
            await datasetRepository
                .Received(1)
                .SaveDataset(Arg.Is<Dataset>(d =>
                d.Current.Version == 1
                ));

            // Ensure comment is null
            await datasetRepository
                .Received(1)
                .SaveDataset(Arg.Is<Dataset>(d =>
                string.IsNullOrWhiteSpace(d.Current.Comment)
                ));

            // Ensure the rest of the properties are set
            await datasetRepository
                .Received(1)
                .SaveDataset(Arg.Is<Dataset>(d =>
                d.Definition.Id == model.DefinitionId &&
                d.Current.Author.Name == authorName &&
                d.Current.Author.Id == authorId &&
                d.Name == name &&
                d.Description == model.Description
                ));

            await searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<DatasetIndex>>(
                    d => d.First().Id == model.DatasetId &&
                    d.First().DefinitionId == model.DefinitionId &&
                    d.First().DefinitionName == datasetDefinition.Name &&
                    d.First().Name == name &&
                    d.First().Status == "Draft" &&
                    d.First().Version == 1 &&
                    d.First().Description == model.Description &&
                    d.First().LastUpdatedById == authorId &&
                    d.First().LastUpdatedByName == authorName &&
                    d.First().ChangeNote == ""
              ));

            await cacheProvider
                 .Received(1)
                 .SetAsync<DatasetValidationStatusModel>(
                 Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                 Arg.Is<DatasetValidationStatusModel>(s =>
                     s.CurrentOperation == DatasetValidationStatus.Validated &&
                     s.ErrorMessage == null &&
                     s.OperationId == message.UserProperties["operation-id"].ToString()
                     ));
        }

        [TestMethod]
        public async Task OnValidateDataset_GivenTableResultsAndMetadataValidatesAndSavesWhenUpdatingExistingDataset_ThenValidationSuccessful()
        {
            //Arrange
            const string authorId = "author-id";
            const string authorName = "author-name";
            const string name = "name";
            const string initialDescription = "Initial description";
            const string updatedDescription = "Updated description";
            const string updateComment = "Update comment";

            GetDatasetBlobModel model = NewGetDatasetBlobModel(_ => _.WithDescription(updatedDescription).WithComment(updateComment).WithVersion(2).WithLastUpdatedBy(new ReferenceBuilder().WithId(authorId).WithName(authorName)));

            string blobPath = $"{model.DatasetId}/v{model.Version}/{model.Filename}";

            Message message = GetValidateDatasetMessage(model);

            IDictionary<string, string> metaData = new Dictionary<string, string>
                {
                    { "dataDefinitionId", model.DefinitionId },
                    { "authorName", authorName },
                    { "authorId", authorId },
                    { "datasetId", model.DatasetId },
                    { "name", name },
                    { "description", model.Description },
                    { "comment", model.Comment }
                };

            ILogger logger = CreateLogger();

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);

            blob
                .Name
                .Returns(model.Filename);

            MemoryStream memoryStream = new MemoryStream(CreateTestExcelPackage());

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Is(blobPath))
                .Returns(blob);
            blobClient
                .DownloadToStreamAsync(Arg.Is(blob))
                .Returns(memoryStream);

            DatasetDefinition datasetDefinition = new DatasetDefinition
            {
                Id = model.DefinitionId,
                Name = "Dataset Definition Name",
            };

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                datasetDefinition
            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DocumentEntity<DatasetDefinition>, bool>>>())
                .Returns(datasetDefinitions);

            datasetRepository
                .SaveDataset(Arg.Any<Dataset>())
                .Returns(HttpStatusCode.OK);

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult{ GlobalErrors = new List<DatasetValidationError>() }
            };

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();

            ICacheProvider cacheProvider = CreateCacheProvider();

            ApiResponse<ProviderVersion> providerVersionResponse = new ApiResponse<ProviderVersion>(HttpStatusCode.OK, new ProviderVersion
            {
                Providers = new[]
                {
                    new Common.ApiClient.Providers.Models.Provider()
                }
            });

            IProvidersApiClient providersApiClient = CreateProvidersApiClient();
            providersApiClient
                .GetAllMasterProviders()
                .Returns(providerVersionResponse);

            DatasetService service = CreateDatasetService(
                logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                searchRepository: searchRepository,
                cacheProvider: cacheProvider,
                providersApiClient: providersApiClient);

            DatasetVersion existingDatasetVersion = new DatasetVersion()
            {
                Version = 1,
            };

            Dataset existingDataset = new Dataset()
            {
                Id = model.DatasetId,
                Current = existingDatasetVersion,
                History = new List<DatasetVersion>() { existingDatasetVersion },
                Definition = new Reference(datasetDefinition.Id, datasetDefinition.Name),
                Description = initialDescription,
                Name = name,
                Published = null,
            };

            datasetRepository
                .GetDatasetByDatasetId(Arg.Is(model.DatasetId))
                .Returns(existingDataset);

            // Act
            Func<Task> result = async () => { await service.ValidateDataset(message); };

            // Assert
            result
                .Should().NotThrow();

            // Ensure next version is set
            await datasetRepository
                .Received(1)
                .SaveDataset(Arg.Is<Dataset>(d =>
                d.Current.Version == 2
                ));

            // Ensure comment is changed
            await datasetRepository
                .Received(1)
                .SaveDataset(Arg.Is<Dataset>(d =>
                d.Current.Comment == updateComment
                ));

            // Ensure description is updated
            await datasetRepository
                .Received(1)
                .SaveDataset(Arg.Is<Dataset>(d =>
                d.Description == updatedDescription
                ));

            // Ensure the rest of the properties are the same
            await datasetRepository
                .Received(1)
                .SaveDataset(Arg.Is<Dataset>(d =>
                    d.Definition.Id == model.DefinitionId &&
                    d.Current.Author.Name == authorName &&
                    d.Current.Author.Id == authorId &&
                    d.Name == name
                ));

            await searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<DatasetIndex>>(
                    d => d.First().Id == model.DatasetId &&
                    d.First().DefinitionId == model.DefinitionId &&
                    d.First().DefinitionName == datasetDefinition.Name &&
                    d.First().Name == name &&
                    d.First().Status == "Draft" &&
                    d.First().Version == 2 &&
                    d.First().Description == updatedDescription &&
                     d.First().LastUpdatedById == authorId &&
                    d.First().LastUpdatedByName == authorName &&
                    d.First().ChangeNote == updateComment
                    ));

            await cacheProvider
                 .Received(1)
                 .SetAsync<DatasetValidationStatusModel>(
                 Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                 Arg.Is<DatasetValidationStatusModel>(s =>
                     s.CurrentOperation == DatasetValidationStatus.Validated &&
                     s.ErrorMessage == null &&
                     s.OperationId == message.UserProperties["operation-id"].ToString()
                     ));
        }

        [TestMethod]
        public void OnValidateDataset_GivenTableResultsAndMetadataValidatesAndProvidedVersionIsNotDeterminedToBeNext_ThenExceptionIsThrown()
        {
            //Arrange
            const string authorId = "author-id";
            const string authorName = "author-name";
            const string name = "name";
            const string initialDescription = "Initial description";
            const string updatedDescription = "Updated description";
            const string updateComment = "Update comment";

            GetDatasetBlobModel model = NewGetDatasetBlobModel(_ => _.WithDescription(updatedDescription).WithComment(updateComment).WithVersion(2).WithLastUpdatedBy(new ReferenceBuilder().WithId(authorId).WithName(authorName)));

            string blobPath = $"{model.DatasetId}/v{model.Version}/{model.Filename}";

            Message message = GetValidateDatasetMessage(model);

            IDictionary<string, string> metaData = new Dictionary<string, string>
                {
                    { "dataDefinitionId", model.DefinitionId },
                    { "authorName", authorName },
                    { "authorId", authorId },
                    { "datasetId", model.DatasetId },
                    { "name", name },
                    { "description", model.Description },
                    { "comment", model.Comment }
                };

            ILogger logger = CreateLogger();

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);

            blob
                .Name
                .Returns(model.Filename);

            MemoryStream memoryStream = new MemoryStream(CreateTestExcelPackage());

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Is(blobPath))
                .Returns(blob);
            blobClient
                .DownloadToStreamAsync(Arg.Is(blob))
                .Returns(memoryStream);

            DatasetDefinition datasetDefinition = new DatasetDefinition
            {
                Id = model.DefinitionId,
                Name = "Dataset Definition Name",
            };

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                datasetDefinition
            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DocumentEntity<DatasetDefinition>, bool>>>())
                .Returns(datasetDefinitions);

            datasetRepository
                .SaveDataset(Arg.Any<Dataset>())
                .Returns(HttpStatusCode.OK);

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult{ GlobalErrors = new List<DatasetValidationError>() }
            };

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();

            ApiResponse<ProviderVersion> providerVersionResponse = new ApiResponse<ProviderVersion>(HttpStatusCode.OK, new ProviderVersion
            {
                Providers = new[]
               {
                    new Common.ApiClient.Providers.Models.Provider(),
                    new Common.ApiClient.Providers.Models.Provider(),
                    new Common.ApiClient.Providers.Models.Provider()
                }
            });

            IProvidersApiClient providersApiClient = CreateProvidersApiClient();
            providersApiClient
                .GetAllMasterProviders()
                .Returns(providerVersionResponse);

            DatasetService service = CreateDatasetService(logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                searchRepository: searchRepository,
                providersApiClient: providersApiClient);

            DatasetVersion existingDatasetVersion1 = new DatasetVersion()
            {
                Version = 2,
            };

            DatasetVersion existingDatasetVersion2 = new DatasetVersion()
            {
                Version = 2,
            };

            Dataset existingDataset = new Dataset()
            {
                Id = model.DatasetId,
                Current = existingDatasetVersion2,
                History = new List<DatasetVersion>() {
                    existingDatasetVersion1,
                    existingDatasetVersion2,
                },
                Definition = new Reference(datasetDefinition.Id, datasetDefinition.Name),
                Description = initialDescription,
                Name = name,
                Published = null,
            };

            datasetRepository
                .GetDatasetByDatasetId(Arg.Is(model.DatasetId))
                .Returns(existingDataset);

            // Act
            Func<Task> result = () => service.ValidateDataset(message);

            // Assert
            result
               .Should().Throw<InvalidOperationException>()
               .WithMessage($"Failed to save dataset or dataset version for id: {model.DatasetId} due to version mismatch. Expected next version to be 3 but request provided '2'");

            logger
                .Received(1)
                .Error(Arg.Is($"Failed to save dataset or dataset version for id: {model.DatasetId} due to version mismatch. Expected next version to be 3 but request provided '2'"));
        }

        [TestMethod]
        public void OnValidateDataset_GivenTableResultsAndMetadataValidatesAndExistingDatasetIsNull_ThenExceptionIsThrown()
        {
            //Arrange
            const string authorId = "author-id";
            const string authorName = "author-name";
            const string name = "name";
            const string updatedDescription = "Updated description";
            const string updateComment = "Update comment";

            GetDatasetBlobModel model = NewGetDatasetBlobModel(_ => _.WithDescription(updatedDescription).WithComment(updateComment).WithVersion(2).WithLastUpdatedBy(new ReferenceBuilder().WithId(authorId).WithName(authorName)));

            string blobPath = $"{model.DatasetId}/v{model.Version}/{model.Filename}";

            Message message = GetValidateDatasetMessage(model);

            IDictionary<string, string> metaData = new Dictionary<string, string>
                {
                    { "dataDefinitionId", model.DefinitionId },
                    { "authorName", authorName },
                    { "authorId", authorId },
                    { "datasetId", model.DatasetId },
                    { "name", name },
                    { "description", model.Description },
                    { "comment", model.Comment }
                };

            ILogger logger = CreateLogger();

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);

            blob
                .Name
                .Returns(model.Filename);

            MemoryStream memoryStream = new MemoryStream(CreateTestExcelPackage());

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Is(blobPath))
                .Returns(blob);
            blobClient
                .DownloadToStreamAsync(Arg.Is(blob))
                .Returns(memoryStream);

            DatasetDefinition datasetDefinition = new DatasetDefinition
            {
                Id = model.DefinitionId,
                Name = "Dataset Definition Name",
            };

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                datasetDefinition
            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DocumentEntity<DatasetDefinition>, bool>>>())
                .Returns(datasetDefinitions);

            datasetRepository
                .SaveDataset(Arg.Any<Dataset>())
                .Returns(HttpStatusCode.OK);

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult{ GlobalErrors = new List<DatasetValidationError>() }
            };

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();

            ApiResponse<ProviderVersion> providerVersionResponse = new ApiResponse<ProviderVersion>(HttpStatusCode.OK, new ProviderVersion
            {
                Providers = new[]
                {
                    new Common.ApiClient.Providers.Models.Provider(),
                }
            });

            IProvidersApiClient providersApiClient = CreateProvidersApiClient();
            providersApiClient
                .GetAllMasterProviders()
                .Returns(providerVersionResponse);

            DatasetService service = CreateDatasetService(logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                searchRepository: searchRepository,
                providersApiClient: providersApiClient);

            DatasetVersion existingDatasetVersion1 = new DatasetVersion()
            {
                Version = 2,
            };

            DatasetVersion existingDatasetVersion2 = new DatasetVersion()
            {
                Version = 2,
            };

            Dataset existingDataset = null;

            datasetRepository
                .GetDatasetByDatasetId(Arg.Is(model.DatasetId))
                .Returns(existingDataset);

            // Act
            Func<Task> action = () => service.ValidateDataset(message);

            // Assert
            action
                .Should().Throw<InvalidOperationException>()
                .WithMessage($"Failed to retrieve dataset for id: {model.DatasetId} response was null");

            logger
                .Received(1)
                .Warning($"Failed to retrieve dataset for id: {model.DatasetId} response was null");
        }

        [TestMethod]
        public void OnValidateDataset_GivenTableResultsAndMetadataValidatesAndSaveDatasetFails_ThenErrorIsReturned()
        {
            //Arrange
            const string authorId = "author-id";
            const string authorName = "author-name";
            const string name = "name";
            const string initialDescription = "Initial description";
            const string updatedDescription = "Updated description";
            const string updateComment = "Update comment";

            GetDatasetBlobModel model = NewGetDatasetBlobModel(_ => _.WithDescription(updatedDescription).WithComment(updateComment).WithVersion(2).WithLastUpdatedBy(new ReferenceBuilder().WithId(authorId).WithName(authorName)));

            string blobPath = $"{model.DatasetId}/v{model.Version}/{model.Filename}";

            Message message = GetValidateDatasetMessage(model);

            IDictionary<string, string> metaData = new Dictionary<string, string>
                {
                    { "dataDefinitionId", model.DefinitionId },
                    { "authorName", authorName },
                    { "authorId", authorId },
                    { "datasetId", model.DatasetId },
                    { "name", name },
                    { "description", model.Description },
                    { "comment", model.Comment }
                };

            ILogger logger = CreateLogger();

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);

            blob
                .Name
                .Returns(model.Filename);

            MemoryStream memoryStream = new MemoryStream(CreateTestExcelPackage());

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Is(blobPath))
                .Returns(blob);
            blobClient
                .DownloadToStreamAsync(Arg.Is(blob))
                .Returns(memoryStream);

            DatasetDefinition datasetDefinition = new DatasetDefinition
            {
                Id = model.DefinitionId,
                Name = "Dataset Definition Name",
            };

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                datasetDefinition
            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DocumentEntity<DatasetDefinition>, bool>>>())
                .Returns(datasetDefinitions);

            datasetRepository
                .SaveDataset(Arg.Any<Dataset>())
                .Returns(HttpStatusCode.InternalServerError);

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult{ GlobalErrors = new List<DatasetValidationError>() }
            };

            ApiResponse<ProviderVersion> providerVersionResponse = new ApiResponse<ProviderVersion>(HttpStatusCode.OK, new ProviderVersion
            {
                Providers = new[]
                {
                    new Common.ApiClient.Providers.Models.Provider()
                }
            });

            IProvidersApiClient providersApiClient = CreateProvidersApiClient();
            providersApiClient
                .GetAllMasterProviders()
                .Returns(providerVersionResponse);

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();

            DatasetService service = CreateDatasetService(logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                searchRepository: searchRepository,
                providersApiClient: providersApiClient);

            DatasetVersion existingDatasetVersion = new DatasetVersion()
            {
                Version = 1,
            };

            Dataset existingDataset = new Dataset()
            {
                Id = model.DatasetId,
                Current = existingDatasetVersion,
                History = new List<DatasetVersion>() { existingDatasetVersion },
                Definition = new Reference(datasetDefinition.Id, datasetDefinition.Name),
                Description = initialDescription,
                Name = name,
                Published = null,
            };

            datasetRepository
                .GetDatasetByDatasetId(Arg.Is(model.DatasetId))
                .Returns(existingDataset);

            // Act
            Func<Task> result = () => service.ValidateDataset(message);

            // Assert
            result
                .Should().Throw<InvalidOperationException>()
                .WithMessage($"Failed to save dataset for id: {model.DatasetId} with status code InternalServerError");

            logger
                .Received(1)
                .Warning(Arg.Is($"Failed to save dataset for id: {model.DatasetId} with status code InternalServerError"));
        }

        [TestMethod]
        public void OnValidateDataset_GivenTableResultsAndMetadataValidatesAndSavesWhenUpdatingSearchThenErrorIsThrown()
        {
            //Arrange
            const string authorId = "author-id";
            const string authorName = "author-name";
            const string name = "name";
            const string initialDescription = "Initial description";
            const string updatedDescription = "Updated description";
            const string updateComment = "Update comment";

            GetDatasetBlobModel model = NewGetDatasetBlobModel(_ => _.WithDescription(updatedDescription).WithComment(updateComment).WithVersion(2).WithLastUpdatedBy(new ReferenceBuilder().WithId(authorId).WithName(authorName)));

            string blobPath = $"{model.DatasetId}/v{model.Version}/{model.Filename}";

            Message message = GetValidateDatasetMessage(model);

            IDictionary<string, string> metaData = new Dictionary<string, string>
                {
                    { "dataDefinitionId", model.DefinitionId },
                    { "authorName", authorName },
                    { "authorId", authorId },
                    { "datasetId", model.DatasetId },
                    { "name", name },
                    { "description", model.Description },
                    { "comment", model.Comment }
                };

            ILogger logger = CreateLogger();

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);

            blob
                .Name
                .Returns(model.Filename);

            MemoryStream memoryStream = new MemoryStream(CreateTestExcelPackage());

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Is(blobPath))
                .Returns(blob);
            blobClient
                .DownloadToStreamAsync(Arg.Is(blob))
                .Returns(memoryStream);

            DatasetDefinition datasetDefinition = new DatasetDefinition
            {
                Id = model.DefinitionId,
                Name = "Dataset Definition Name",
            };

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                datasetDefinition
            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DocumentEntity<DatasetDefinition>, bool>>>())
                .Returns(datasetDefinitions);

            datasetRepository
                .SaveDataset(Arg.Any<Dataset>())
                .Returns(HttpStatusCode.OK);

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult{ GlobalErrors = new List<DatasetValidationError>() }
            };

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();

            ApiResponse<ProviderVersion> providerVersionResponse = new ApiResponse<ProviderVersion>(HttpStatusCode.OK, new ProviderVersion
            {
                Providers = new[]
                {
                    new Common.ApiClient.Providers.Models.Provider()
                }
            });

            IProvidersApiClient providersApiClient = CreateProvidersApiClient();
            providersApiClient
                .GetAllMasterProviders()
                .Returns(providerVersionResponse);

            DatasetService service = CreateDatasetService(logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                searchRepository: searchRepository,
                providersApiClient: providersApiClient);

            DatasetVersion existingDatasetVersion = new DatasetVersion()
            {
                Version = 1,
            };

            Dataset existingDataset = new Dataset()
            {
                Id = model.DatasetId,
                Current = existingDatasetVersion,
                History = new List<DatasetVersion>() { existingDatasetVersion },
                Definition = new Reference(datasetDefinition.Id, datasetDefinition.Name),
                Description = initialDescription,
                Name = name,
                Published = null,
            };

            datasetRepository
                .GetDatasetByDatasetId(Arg.Is(model.DatasetId))
                .Returns(existingDataset);

            IEnumerable<IndexError> indexErrors = new List<IndexError>()
            {
                new IndexError()
                {
                     Key = "datasetId",
                      ErrorMessage = "Error in dataset ID for search",
                }
            };

            searchRepository
                .Index(Arg.Any<List<DatasetIndex>>())
                .Returns(indexErrors);

            // Act
            Func<Task> result = () => service.ValidateDataset(message);

            // Assert
            result
                .Should().Throw<InvalidOperationException>()
                .WithMessage($"Failed to save dataset for id: {model.DatasetId} in search with errors Error in dataset ID for search");

            logger
                .Received(1)
                .Warning(Arg.Is($"Failed to save dataset for id: {model.DatasetId} in search with errors Error in dataset ID for search"));
        }

        [TestMethod]
        public async Task OnValidateDataset_WhenNoOperationIdProvided_ThenErrorLogged()
        {
            // Arrange
            Message message = GetValidateDatasetMessage(NewGetDatasetBlobModel());

            message.UserProperties["operation-id"] = "";

            ILogger logger = CreateLogger();

            DatasetService service = CreateDatasetService(logger: logger);

            // Act
            await service.ValidateDataset(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is("Operation ID was null or empty string on the message from ValidateDataset"));
        }

        [TestMethod]
        public async Task OnValidateDataset_WhenOperationIdIsNull_ThenErrorLogged()
        {
            // Arrange
            Message message = GetValidateDatasetMessage(NewGetDatasetBlobModel());

            message.UserProperties["operation-id"] = null;

            ILogger logger = CreateLogger();

            DatasetService service = CreateDatasetService(logger: logger);

            // Act
            await service.ValidateDataset(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is("Operation ID was null or empty string on the message from ValidateDataset"));
        }

        [TestMethod]
        public async Task OnValidateDataset_WhenOperationIdIsEmptyString_ThenErrorLogged()
        {
            // Arrange
            Message message = GetValidateDatasetMessage(NewGetDatasetBlobModel());

            message.UserProperties["operation-id"] = string.Empty;

            ILogger logger = CreateLogger();

            DatasetService service = CreateDatasetService(logger: logger);

            // Act
            await service.ValidateDataset(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is("Operation ID was null or empty string on the message from ValidateDataset"));
        }

        [TestMethod]
        public async Task OnValidateDataset_WhenGetDatasetBlobModelIsNull_ThenErrorLogged()
        {
            // Arrange
            Message message = GetValidateDatasetMessage(null);

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();

            DatasetService service = CreateDatasetService(logger: logger, cacheProvider: cacheProvider);

            // Act
            await service.ValidateDataset(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is("Null model was provided to ValidateDataset"));

            await cacheProvider
                .Received(1)
                .SetAsync<DatasetValidationStatusModel>(
                Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                Arg.Is<DatasetValidationStatusModel>(s =>
                    s.CurrentOperation == DatasetValidationStatus.Processing &&
                    s.OperationId == message.UserProperties["operation-id"].ToString()
                    ));

            await cacheProvider
                 .Received(1)
                 .SetAsync<DatasetValidationStatusModel>(
                 Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                 Arg.Is<DatasetValidationStatusModel>(s =>
                     s.CurrentOperation == DatasetValidationStatus.FailedValidation &&
                     s.ErrorMessage == "Null model was provided to ValidateDataset" &&
                     s.OperationId == message.UserProperties["operation-id"].ToString()
                     ));
        }

        [TestMethod]
        public async Task OnValidateDataset_WhenGetDatasetBlobModelValidationIsNull_ThenErrorLogged()
        {
            // Arrange
            Message message = GetValidateDatasetMessage(NewGetDatasetBlobModel());

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();

            IValidator<GetDatasetBlobModel> validator = CreateGetDatasetBlobModelValidator();

            validator
                .ValidateAsync(Arg.Any<GetDatasetBlobModel>())
                .Returns((ValidationResult)null);

            DatasetService service = CreateDatasetService(
                logger: logger,
                cacheProvider: cacheProvider,
                getDatasetBlobModelValidator: validator);

            // Act
            await service.ValidateDataset(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is("GetDatasetBlobModel validation result returned null"));

            await cacheProvider
                .Received(1)
                .SetAsync<DatasetValidationStatusModel>(
                Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                Arg.Is<DatasetValidationStatusModel>(s =>
                    s.CurrentOperation == DatasetValidationStatus.Processing &&
                    s.OperationId == message.UserProperties["operation-id"].ToString()
                    ));

            await cacheProvider
                 .Received(1)
                 .SetAsync<DatasetValidationStatusModel>(
                 Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                 Arg.Is<DatasetValidationStatusModel>(s =>
                     s.CurrentOperation == DatasetValidationStatus.FailedValidation &&
                     s.ErrorMessage == "GetDatasetBlobModel validation result returned null" &&
                     s.OperationId == message.UserProperties["operation-id"].ToString()
                     ));
        }

        [TestMethod]
        public async Task OnValidateDataset_WhenGetDatasetBlobModelValidationReturnsError_ThenErrorLogged()
        {
            // Arrange
            Message message = GetValidateDatasetMessage(NewGetDatasetBlobModel());

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();

            IValidator<GetDatasetBlobModel> validator = CreateGetDatasetBlobModelValidator();

            List<ValidationFailure> validationFailures = new List<ValidationFailure>()
            {
                new ValidationFailure("test1", "Test message 1")
            };

            ValidationResult validationResult = new ValidationResult(validationFailures);

            validator
                .ValidateAsync(Arg.Any<GetDatasetBlobModel>())
                .Returns(validationResult);

            DatasetService service = CreateDatasetService(
                logger: logger,
                cacheProvider: cacheProvider,
                getDatasetBlobModelValidator: validator);

            // Act
            await service.ValidateDataset(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is("GetDatasetBlobModel model error: {0}"), Arg.Any<IList<ValidationFailure>>());

            await cacheProvider
                .Received(1)
                .SetAsync<DatasetValidationStatusModel>(
                Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                Arg.Is<DatasetValidationStatusModel>(s =>
                    s.CurrentOperation == DatasetValidationStatus.Processing &&
                    s.OperationId == message.UserProperties["operation-id"].ToString()
                    ));

            await cacheProvider
                 .Received(1)
                 .SetAsync<DatasetValidationStatusModel>(
                 Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                 Arg.Is<DatasetValidationStatusModel>(s =>
                     s.CurrentOperation == DatasetValidationStatus.FailedValidation &&
                     s.ErrorMessage == "GetDatasetBlobModel model error" &&
                     s.ValidationFailures != null &&
                     s.ValidationFailures.Count == validationFailures.Count &&
                     s.OperationId == message.UserProperties["operation-id"].ToString()
                     ));

        }

        [TestMethod]
        public async Task OnValidateDataset_WhenGetBlobReferenceFromServerAsyncReturnsNull_ThenErrorLogged()
        {
            // Arrange
            GetDatasetBlobModel getDatasetBlobModel = NewGetDatasetBlobModel();

            Message message = GetValidateDatasetMessage(getDatasetBlobModel);

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();

            IValidator<GetDatasetBlobModel> validator = CreateGetDatasetBlobModelValidator();

            List<ValidationFailure> validationFailures = new List<ValidationFailure>();

            ValidationResult validationResult = new ValidationResult(validationFailures);

            validator
                .ValidateAsync(Arg.Any<GetDatasetBlobModel>())
                .Returns(validationResult);

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Any<string>())
                .Returns((ICloudBlob)null);

            DatasetService service = CreateDatasetService(
                logger: logger,
                cacheProvider: cacheProvider,
                getDatasetBlobModelValidator: validator,
                blobClient: blobClient);

            // Act
            await service.ValidateDataset(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is($"Failed to find blob with path: {getDatasetBlobModel.DatasetId}/v{getDatasetBlobModel.Version}/{getDatasetBlobModel.Filename}"));

            await cacheProvider
                .Received(1)
                .SetAsync<DatasetValidationStatusModel>(
                Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                Arg.Is<DatasetValidationStatusModel>(s =>
                    s.CurrentOperation == DatasetValidationStatus.Processing &&
                    s.OperationId == message.UserProperties["operation-id"].ToString()
                    ));

            await cacheProvider
                 .Received(1)
                 .SetAsync<DatasetValidationStatusModel>(
                 Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                 Arg.Is<DatasetValidationStatusModel>(s =>
                     s.CurrentOperation == DatasetValidationStatus.FailedValidation &&
                     s.ErrorMessage == $"Failed to find blob with path: {getDatasetBlobModel.DatasetId}/v{getDatasetBlobModel.Version}/{getDatasetBlobModel.Filename}" &&
                     s.OperationId == message.UserProperties["operation-id"].ToString()
                     ));
        }

        [TestMethod]
        public async Task OnValidateDataset_WhenDownloadToStreamAsyncReturnsNull_ThenErrorLogged()
        {
            // Arrange
            GetDatasetBlobModel getDatasetBlobModel = NewGetDatasetBlobModel();

            Message message = GetValidateDatasetMessage(getDatasetBlobModel);

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();

            IValidator<GetDatasetBlobModel> validator = CreateGetDatasetBlobModelValidator();

            List<ValidationFailure> validationFailures = new List<ValidationFailure>();

            ValidationResult validationResult = new ValidationResult(validationFailures);

            validator
                .ValidateAsync(Arg.Any<GetDatasetBlobModel>())
                .Returns(validationResult);


            ICloudBlob cloubBlob = Substitute.For<ICloudBlob>();
            cloubBlob
                .Name
                .Returns($"{getDatasetBlobModel.DatasetId}/v{getDatasetBlobModel.Version}/{getDatasetBlobModel.Filename}");

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Any<string>())
                .Returns(cloubBlob);

            DatasetService service = CreateDatasetService(
                logger: logger,
                cacheProvider: cacheProvider,
                getDatasetBlobModelValidator: validator,
                blobClient: blobClient);

            // Act
            await service.ValidateDataset(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is($"Blob {getDatasetBlobModel.DatasetId}/v{getDatasetBlobModel.Version}/{getDatasetBlobModel.Filename} contains no data"));

            await cacheProvider
                .Received(1)
                .SetAsync<DatasetValidationStatusModel>(
                Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                Arg.Is<DatasetValidationStatusModel>(s =>
                    s.CurrentOperation == DatasetValidationStatus.Processing &&
                    s.OperationId == message.UserProperties["operation-id"].ToString()
                    ));

            await cacheProvider
                 .Received(1)
                 .SetAsync<DatasetValidationStatusModel>(
                 Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                 Arg.Is<DatasetValidationStatusModel>(s =>
                     s.CurrentOperation == DatasetValidationStatus.FailedValidation &&
                     s.ErrorMessage == $"Blob {getDatasetBlobModel.DatasetId}/v{getDatasetBlobModel.Version}/{getDatasetBlobModel.Filename} contains no data" &&
                     s.OperationId == message.UserProperties["operation-id"].ToString()
                     ));
        }


        [TestMethod]
        public async Task OnValidateDataset_WhenBlobReturnsDuplicateDatasetNameWithDifferentDataSetId_ThenErrorLogged()
        {
            // Arrange
            string dataSetId1 = "id-1";
            string dataSetId2 = "id-2";

            Message message = GetValidateDatasetMessage(NewGetDatasetBlobModel(x=>x.WithDatasetId(dataSetId1)));

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();

            IValidator<GetDatasetBlobModel> validator = CreateGetDatasetBlobModelValidator();

            List<ValidationFailure> validationFailures = new List<ValidationFailure>();

            ValidationResult validationResult = new ValidationResult(validationFailures);

            validator
                .ValidateAsync(Arg.Any<GetDatasetBlobModel>())
                .Returns(validationResult);

            ICloudBlob cloubBlob = Substitute.For<ICloudBlob>();
            cloubBlob
                .Name
                .Returns("dsid/v1/filename.xlsx");

            cloubBlob
                .Metadata["name"]
                .Returns("dataset");

            MemoryStream memoryStream = new MemoryStream(new byte[1]);

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Any<string>())
                .Returns(cloubBlob);

            blobClient
                .DownloadToStreamAsync(Arg.Is(cloubBlob))
                .Returns(memoryStream);

            IEnumerable<Dataset> datasets = new Dataset[] { new Dataset { Name = "dataset", Id = dataSetId2 } };

            IDatasetRepository datasetsRepository = CreateDatasetsRepository();
            datasetsRepository
                 .GetDatasetsByQuery(Arg.Any<Expression<Func<DocumentEntity<Dataset>, bool>>>())
                 .Returns(datasets);

            DatasetService service = CreateDatasetService(
                logger: logger,
                datasetRepository: datasetsRepository,
                cacheProvider: cacheProvider,
                getDatasetBlobModelValidator: validator,
                blobClient: blobClient);

            // Act
            await service.ValidateDataset(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is("Dataset dataset needs to be a unique name"));

            await cacheProvider
                .Received(1)
                .SetAsync<DatasetValidationStatusModel>(
                Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                Arg.Is<DatasetValidationStatusModel>(s =>
                    s.CurrentOperation == DatasetValidationStatus.Processing &&
                    s.OperationId == message.UserProperties["operation-id"].ToString()
                    ));

            await cacheProvider
                 .Received(1)
                 .SetAsync<DatasetValidationStatusModel>(
                 Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                 Arg.Is<DatasetValidationStatusModel>(s =>
                     s.CurrentOperation == DatasetValidationStatus.FailedValidation &&
                     s.ErrorMessage == "Dataset dataset needs to be a unique name" &&
                     s.OperationId == message.UserProperties["operation-id"].ToString()
                     ));
        }

        [TestMethod]
        public async Task OnValidateDataset_WhenBlobReturnsDuplicateDatasetNameWithSameDataSetIdAndDecreasingVersionId_ThenErrorLogged()
        {
            // Arrange
            string dataSetId = "id-1";
            int oldVersionId = 2;
            int newVersionId = 1;

            Message message = GetValidateDatasetMessage(NewGetDatasetBlobModel(x => x.WithDatasetId(dataSetId).WithVersion(newVersionId)));

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();

            IValidator<GetDatasetBlobModel> validator = CreateGetDatasetBlobModelValidator();

            List<ValidationFailure> validationFailures = new List<ValidationFailure>();

            ValidationResult validationResult = new ValidationResult(validationFailures);

            validator
                .ValidateAsync(Arg.Any<GetDatasetBlobModel>())
                .Returns(validationResult);

            ICloudBlob cloubBlob = Substitute.For<ICloudBlob>();
            cloubBlob
                .Name
                .Returns("dsid/v1/filename.xlsx");

            cloubBlob
                .Metadata["name"]
                .Returns("dataset");

            MemoryStream memoryStream = new MemoryStream(new byte[1]);

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Any<string>())
                .Returns(cloubBlob);

            blobClient
                .DownloadToStreamAsync(Arg.Is(cloubBlob))
                .Returns(memoryStream);

            IEnumerable<Dataset> datasets = new Dataset[] { new Dataset { Name = "dataset", Id = dataSetId, Current = new DatasetVersion { Version = oldVersionId } } };

            IDatasetRepository datasetsRepository = CreateDatasetsRepository();
            datasetsRepository
                 .GetDatasetsByQuery(Arg.Any<Expression<Func<DocumentEntity<Dataset>, bool>>>())
                 .Returns(datasets);

            DatasetService service = CreateDatasetService(
                logger: logger,
                datasetRepository: datasetsRepository,
                cacheProvider: cacheProvider,
                getDatasetBlobModelValidator: validator,
                blobClient: blobClient);

            // Act
            await service.ValidateDataset(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is("Dataset dataset needs to be a unique name"));

            await cacheProvider
                .Received(1)
                .SetAsync<DatasetValidationStatusModel>(
                Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                Arg.Is<DatasetValidationStatusModel>(s =>
                    s.CurrentOperation == DatasetValidationStatus.Processing &&
                    s.OperationId == message.UserProperties["operation-id"].ToString()
                    ));

            await cacheProvider
                 .Received(1)
                 .SetAsync<DatasetValidationStatusModel>(
                 Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                 Arg.Is<DatasetValidationStatusModel>(s =>
                     s.CurrentOperation == DatasetValidationStatus.FailedValidation &&
                     s.ErrorMessage == "Dataset dataset needs to be a unique name" &&
                     s.OperationId == message.UserProperties["operation-id"].ToString()
                     ));
        }

        [TestMethod]
        public async Task OnValidateDataset_WhenExcelValidationErrorsReturned_ThenErrorLogged()
        {
            const string testXlsxLocation = @"TestItems/TestCheck.xlsx";

            // Arrange
            GetDatasetBlobModel getDatasetBlobModel = NewGetDatasetBlobModel();

            Message message = GetValidateDatasetMessage(getDatasetBlobModel);

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();

            IValidator<GetDatasetBlobModel> validator = CreateGetDatasetBlobModelValidator();

            List<ValidationFailure> validationFailures = new List<ValidationFailure>();

            ValidationResult validationResult = new ValidationResult(validationFailures);

            validator
                .ValidateAsync(Arg.Any<GetDatasetBlobModel>())
                .Returns(validationResult);


            ICloudBlob cloubBlob = Substitute.For<ICloudBlob>();
            cloubBlob
                .Name
                .Returns($"{getDatasetBlobModel.DatasetId}/v{getDatasetBlobModel.Version}/{getDatasetBlobModel.Filename}");

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Any<string>())
                .Returns(cloubBlob);

            File.Copy(@"TestItems/1718HNStudNumbers.xlsx", testXlsxLocation, true);
            FileInfo testFile = new FileInfo(testXlsxLocation);

            FileStream stream = new System.IO.FileStream(testFile.FullName, FileMode.Open);

            blobClient
                .DownloadToStreamAsync(Arg.Any<ICloudBlob>())
                .Returns(stream);

            IValidator<ExcelPackage> excelPackageValidator = CreateDataWorksheetValidator();

            List<ValidationFailure> excelValidationFailures = new List<ValidationFailure>()
            {
                new ValidationFailure("test", "Test error"),
            };

            ValidationResult excelValidationResult = new ValidationResult(excelValidationFailures);

            excelPackageValidator
                .Validate(Arg.Any<ExcelPackage>())
                .Returns(excelValidationResult);

            DatasetService service = CreateDatasetService(
                logger: logger,
                cacheProvider: cacheProvider,
                getDatasetBlobModelValidator: validator,
                blobClient: blobClient,
                datasetWorksheetValidator: excelPackageValidator);

            // Act
            await service.ValidateDataset(message);

            // Assert
            await cacheProvider
                .Received(1)
                .SetAsync<DatasetValidationStatusModel>(
                Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                Arg.Is<DatasetValidationStatusModel>(s =>
                    s.CurrentOperation == DatasetValidationStatus.Processing &&
                    s.OperationId == message.UserProperties["operation-id"].ToString()
                    ));

            await cacheProvider
                .Received(1)
                .SetAsync<DatasetValidationStatusModel>(
                Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                Arg.Is<DatasetValidationStatusModel>(s =>
                    s.CurrentOperation == DatasetValidationStatus.ValidatingExcelWorkbook &&
                    s.OperationId == message.UserProperties["operation-id"].ToString()
                    ));

            await cacheProvider
                 .Received(1)
                 .SetAsync<DatasetValidationStatusModel>(
                 Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                 Arg.Is<DatasetValidationStatusModel>(s =>
                     s.CurrentOperation == DatasetValidationStatus.FailedValidation &&
                     s.ValidationFailures.Count == 1 &&
                     s.OperationId == message.UserProperties["operation-id"].ToString()
                     ));
        }

        [TestMethod]
        public async Task OnValidateDataset_WhenExceptionThrownDuringExcelValidation_ThenErrorLogged()
        {
            // Arrange
            GetDatasetBlobModel getDatasetBlobModel = NewGetDatasetBlobModel();

            Message message = GetValidateDatasetMessage(getDatasetBlobModel);

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();

            IValidator<GetDatasetBlobModel> validator = CreateGetDatasetBlobModelValidator();

            List<ValidationFailure> validationFailures = new List<ValidationFailure>();

            ValidationResult validationResult = new ValidationResult(validationFailures);

            validator
                .ValidateAsync(Arg.Any<GetDatasetBlobModel>())
                .Returns(validationResult);


            ICloudBlob cloubBlob = Substitute.For<ICloudBlob>();
            cloubBlob
                .Name
                .Returns($"{getDatasetBlobModel.DatasetId}/v{getDatasetBlobModel.Version}/{getDatasetBlobModel.Filename}");

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Any<string>())
                .Returns(cloubBlob);

            MemoryStream stream = new MemoryStream(new byte[] { 0 });

            blobClient
                .DownloadToStreamAsync(Arg.Any<ICloudBlob>())
                .Returns(stream);

            IValidator<ExcelPackage> excelPackageValidator = CreateDataWorksheetValidator();

            excelPackageValidator
                .Validate(Arg.Any<ExcelPackage>())
                .Returns(x => { throw new Exception("Test exception"); });

            DatasetService service = CreateDatasetService(
                logger: logger,
                cacheProvider: cacheProvider,
                getDatasetBlobModelValidator: validator,
                blobClient: blobClient,
                datasetWorksheetValidator: excelPackageValidator);

            // Act
            await service.ValidateDataset(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is("The data source file type is invalid. Check that your file is an xls or xlsx file"));

            await cacheProvider
                .Received(1)
                .SetAsync<DatasetValidationStatusModel>(
                Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                Arg.Is<DatasetValidationStatusModel>(s =>
                    s.CurrentOperation == DatasetValidationStatus.Processing &&
                    s.OperationId == message.UserProperties["operation-id"].ToString()
                    ));

            await cacheProvider
                 .Received(1)
                 .SetAsync<DatasetValidationStatusModel>(
                 Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                 Arg.Is<DatasetValidationStatusModel>(s =>
                     s.CurrentOperation == DatasetValidationStatus.FailedValidation &&
                     s.ValidationFailures.Count == 2 &&
                     s.OperationId == message.UserProperties["operation-id"].ToString()
                     ));
        }

        [TestMethod]
        public async Task OnValidateDataset_WhenDatasetDefinitionLookupReturnsNull_ThenErrorLogged()
        {
            const string testXlsxLocation = @"TestItems/TestCheck.xlsx";

            // Arrange
            GetDatasetBlobModel getDatasetBlobModel = NewGetDatasetBlobModel();
            Message message = GetValidateDatasetMessage(getDatasetBlobModel);

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();

            IValidator<GetDatasetBlobModel> validator = CreateGetDatasetBlobModelValidator();

            List<ValidationFailure> validationFailures = new List<ValidationFailure>();

            ValidationResult validationResult = new ValidationResult(validationFailures);

            validator
                .ValidateAsync(Arg.Any<GetDatasetBlobModel>())
                .Returns(validationResult);


            ICloudBlob cloudBlob = Substitute.For<ICloudBlob>();
            cloudBlob
                .Name
                .Returns("dsid/v1/filename.xlsx");

            Dictionary<string, string> blobMetadata = new Dictionary<string, string>();
            blobMetadata.Add("dataDefinitionId", getDatasetBlobModel.DefinitionId);

            cloudBlob
                .Metadata
                .Returns(blobMetadata);

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Any<string>())
                .Returns(cloudBlob);

            File.Copy(@"TestItems/1718HNStudNumbers.xlsx", testXlsxLocation, true);
            FileInfo testFile = new FileInfo(testXlsxLocation);

            FileStream stream = new System.IO.FileStream(testFile.FullName, FileMode.Open);

            blobClient
                .DownloadToStreamAsync(Arg.Any<ICloudBlob>())
                .Returns(stream);

            IValidator<ExcelPackage> excelPackageValidator = CreateDataWorksheetValidator();

            List<ValidationFailure> excelValidationFailures = new List<ValidationFailure>()
            {
            };

            ValidationResult excelValidationResult = new ValidationResult(excelValidationFailures);

            excelPackageValidator
                .Validate(Arg.Any<ExcelPackage>())
                .Returns(excelValidationResult);

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DocumentEntity<DatasetDefinition>, bool>>>())
                .Returns(Enumerable.Empty<DatasetDefinition>());

            DatasetService service = CreateDatasetService(
                logger: logger,
                cacheProvider: cacheProvider,
                getDatasetBlobModelValidator: validator,
                blobClient: blobClient,
                datasetWorksheetValidator: excelPackageValidator,
                datasetRepository: datasetRepository);

            // Act
            await service.ValidateDataset(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is($"Unable to find a data definition for id: {getDatasetBlobModel.DefinitionId}, for blob: {getDatasetBlobModel.DatasetId}/v{getDatasetBlobModel.Version}/{getDatasetBlobModel.Filename}"));

            await cacheProvider
                .Received(1)
                .SetAsync<DatasetValidationStatusModel>(
                Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                Arg.Is<DatasetValidationStatusModel>(s =>
                    s.CurrentOperation == DatasetValidationStatus.Processing &&
                    s.OperationId == message.UserProperties["operation-id"].ToString()
                    ));

            await cacheProvider
                .Received(1)
                .SetAsync<DatasetValidationStatusModel>(
                Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                Arg.Is<DatasetValidationStatusModel>(s =>
                    s.CurrentOperation == DatasetValidationStatus.ValidatingExcelWorkbook &&
                    s.OperationId == message.UserProperties["operation-id"].ToString()
                    ));

            await cacheProvider
                 .Received(1)
                 .SetAsync<DatasetValidationStatusModel>(
                 Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                 Arg.Is<DatasetValidationStatusModel>(s =>
                     s.CurrentOperation == DatasetValidationStatus.FailedValidation &&
                     s.ErrorMessage == $"Unable to find a data definition for id: {getDatasetBlobModel.DefinitionId}, for blob: {getDatasetBlobModel.DatasetId}/v{getDatasetBlobModel.Version}/{getDatasetBlobModel.Filename}" &&
                     s.OperationId == message.UserProperties["operation-id"].ToString()
                     ));
        }

        [TestMethod]
        public async Task OnValidateDataset_GivenValidDatasetBlobModel_ThenUpdatesJobServiceWithProgress()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            const string dataDefinitionId = "definition-id";
            const string authorId = "author-id";
            const string authorName = "author-name";
            const string datasetId = "dataset-id";
            const string name = "name";
            const string description = "updated description";
            const string operationId = "operationId";
            const string jobId = "job-id";

            IDictionary<string, string> metaData = new Dictionary<string, string>
                {
                    { "dataDefinitionId", dataDefinitionId },
                    { "authorName", authorName },
                    { "authorId", authorId },
                    { "datasetId", datasetId },
                    { "name", name },
                    { "description", description }
                };

            GetDatasetBlobModel model = new GetDatasetBlobModel
            {
                DatasetId = "dataset-id",
                Version = 1,
                Filename = "ds.xlsx",
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            Message message = new Message(byteArray);

            message.UserProperties.Add("operation-id", operationId);
            message.UserProperties.Add("jobId", jobId);

            ILogger logger = CreateLogger();

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);

            MemoryStream memoryStream = new MemoryStream(CreateTestExcelPackage());

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Is(blobPath))
                .Returns(blob);
            blobClient
                .DownloadToStreamAsync(Arg.Is(blob))
                .Returns(memoryStream);

            DatasetDefinition datasetDefinition = new DatasetDefinition
            {
                Id = dataDefinitionId
            };

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                datasetDefinition
            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DocumentEntity<DatasetDefinition>, bool>>>())
                .Returns(datasetDefinitions);

            datasetRepository
                .SaveDataset(Arg.Any<Dataset>())
                .Returns(HttpStatusCode.OK);

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult{ GlobalErrors = new List<DatasetValidationError>() }
            };

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();

            ICacheProvider cacheProvider = CreateCacheProvider();

            IJobsApiClient jobsApiClient = CreateJobsApiClient();

            ApiResponse<ProviderVersion> providerVersionResponse = new ApiResponse<ProviderVersion>(HttpStatusCode.OK, new ProviderVersion
            {
                Providers = new[]
                {
                    new Common.ApiClient.Providers.Models.Provider()
                }
            });

            IJobManagement jobManagement = CreateJobManagement();

            IProvidersApiClient providersApiClient = CreateProvidersApiClient();
            providersApiClient
                .GetAllMasterProviders()
                .Returns(providerVersionResponse);

            DatasetService service = CreateDatasetService(logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                searchRepository: searchRepository,
                cacheProvider: cacheProvider,
                jobsApiClient: jobsApiClient,
                jobManagement: jobManagement,
                providersApiClient: providersApiClient);

            // Act
            Func<Task> result = async () => { await service.ValidateDataset(message); };

            // Assert
            result
                .Should().NotThrow();

            // Ensure initial version is set
            await datasetRepository
                .Received(1)
                .SaveDataset(Arg.Is<Dataset>(d =>
                d.Current.Version == 1
                ));

            // Ensure comment is null
            await datasetRepository
                .Received(1)
                .SaveDataset(Arg.Is<Dataset>(d =>
                string.IsNullOrWhiteSpace(d.Current.Comment)
                ));

            // Ensure the rest of the properties are set
            await datasetRepository
                .Received(1)
                .SaveDataset(Arg.Is<Dataset>(d =>
                d.Definition.Id == dataDefinitionId &&
                d.Current.Author.Name == authorName &&
                d.Current.Author.Id == authorId &&
                d.Name == name &&
                d.Description == description
                ));

            await searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<DatasetIndex>>(
                    d => d.First().Id == datasetId &&
                    d.First().DefinitionId == dataDefinitionId &&
                    d.First().DefinitionName == datasetDefinition.Name &&
                    d.First().Name == name &&
                    d.First().Status == "Draft" &&
                    d.First().Version == 1 &&
                    d.First().Description == description &&
                    d.First().LastUpdatedById == authorId &&
                    d.First().LastUpdatedByName == authorName &&
                    d.First().ChangeNote == ""
              ));

            await cacheProvider
                 .Received(1)
                 .SetAsync<DatasetValidationStatusModel>(
                 Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                 Arg.Is<DatasetValidationStatusModel>(s =>
                     s.CurrentOperation == DatasetValidationStatus.Validated &&
                     s.ErrorMessage == null &&
                     s.OperationId == operationId
                     ));

            for (int percentComplete = 0; percentComplete < 75; percentComplete += 25)
            {
                await jobManagement
                    .Received(1)
                    .UpdateJobStatus(Arg.Is(jobId), percentComplete, null, null);
            }
            await jobManagement
                .Received(1)
                .UpdateJobStatus(Arg.Is(jobId), 100, true, "Dataset passed validation");
        }

        private GetDatasetBlobModel NewGetDatasetBlobModel(Action<GetDatasetBlobModelBuilder> setUp = null)
        {
            GetDatasetBlobModelBuilder getDatasetBlobModelBuilder = new GetDatasetBlobModelBuilder()
                .WithVersion(1);

            setUp?.Invoke(getDatasetBlobModelBuilder);

            return getDatasetBlobModelBuilder.Build();
        }

        private Message GetValidateDatasetMessage(GetDatasetBlobModel model)
        {
            const string operationId = "operationId";

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            Message message = new Message(byteArray);
            message.UserProperties.Add("jobId", "job1");
            message.UserProperties.Add("operation-id", operationId);

            return message;
        }

    }
}
