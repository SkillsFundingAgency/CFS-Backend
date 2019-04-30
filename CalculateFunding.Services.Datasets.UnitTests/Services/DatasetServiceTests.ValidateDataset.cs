using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Repositories.Common.Search;
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Blob;
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
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            DatasetService service = CreateDatasetService(logger: logger);

            // Act
            IActionResult result = await service.ValidateDataset(request);

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
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ValidationResult validationResult = new ValidationResult(new[]{
                    new ValidationFailure("prop1", "any error")
                });

            IValidator<GetDatasetBlobModel> validator = CreateGetDatasetBlobModelValidator(validationResult);


            DatasetService service = CreateDatasetService(logger: logger, getDatasetBlobModelValidator: validator);

            // Act
            IActionResult result = await service.ValidateDataset(request);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task ValidateDataset_GivenModelButBlobNotFound_ReturnsPreConditionFailed()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            GetDatasetBlobModel model = new GetDatasetBlobModel
            {
                DatasetId = "dataset-id",
                Version = 1,
                Filename = "ds.xlsx"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Is(blobPath))
                .Returns((ICloudBlob)null);

            DatasetService service = CreateDatasetService(logger: logger, blobClient: blobClient);

            // Act
            IActionResult result = await service.ValidateDataset(request);

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
            const string blobPath = "dataset-id/v1/ds.xlsx";

            GetDatasetBlobModel model = new GetDatasetBlobModel
            {
                DatasetId = "dataset-id",
                Version = 1,
                Filename = "ds.xlsx"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

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
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
                .Returns(datasetDefinitions);

            DatasetService service = CreateDatasetService(logger: logger, blobClient: blobClient, datasetRepository: datasetRepository);

            // Act
            IActionResult result = await service.ValidateDataset(request);

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
            const string blobPath = "dataset-id/v1/ds.xlsx";

            GetDatasetBlobModel model = new GetDatasetBlobModel
            {
                DatasetId = "dataset-id",
                Version = 1,
                Filename = "ds.xlsx"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

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
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
                .Returns(datasetDefinitions);

            DatasetService service = CreateDatasetService(logger: logger, blobClient: blobClient, datasetRepository: datasetRepository);

            // Act
            IActionResult result = await service.ValidateDataset(request);

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
        public async Task OnValidateDataset_GivenTableResultsContainsOneError_ReturnsOKResultWithMessage()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";
            const string operationId = "operationId";

            GetDatasetBlobModel model = new GetDatasetBlobModel
            {
                DatasetId = "dataset-id",
                Version = 1,
                Filename = "ds.xlsx"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            Message message = new Message(byteArray);
            message.UserProperties.Add("jobId", "job1");
            message.UserProperties.Add("operation-id", operationId);

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
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
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

            ICacheProvider cacheProvider = CreateCacheProvider();

            DatasetService service = CreateDatasetService(
                logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                datasetUploadValidator: datasetUploadValidator,
                cacheProvider: cacheProvider);

            // Act
            Func<Task> result = async () => { await service.ValidateDataset(message); };

            // Assert
            result
               .Should().NotThrow();

            await cacheProvider
                .Received(1)
                .SetAsync(Arg.Is($"{CacheKeys.DatasetValidationStatus}:{operationId}"), Arg.Is<DatasetValidationStatusModel>(v =>
                v.OperationId == operationId &&
                v.ValidationFailures.Count == 3));
        }

        [TestMethod]
        public async Task ValidateDataset_GivenDatasetBlobModel_CallsJobServiceToQueueJob()
        {
            // Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            GetDatasetBlobModel model = new GetDatasetBlobModel
            {
                DatasetId = "dataset-id",
                Version = 1,
                Filename = "ds.xlsx",
                Comment = "Change comment",
                DefinitionId = DataDefintionId,
                Description = "My change description",
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

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
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
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
            IActionResult result = await service.ValidateDataset(request);

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
            const string blobPath = "dataset-id/v1/ds.xlsx";
            const string authorId = "user1";
            const string authorName = "user 1";

            GetDatasetBlobModel model = new GetDatasetBlobModel
            {
                DatasetId = "dataset-id",
                Version = 1,
                Filename = "ds.xlsx",
                Comment = "Change comment",
                DefinitionId = DataDefintionId,
                Description = "My change description",
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

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
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
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
            IActionResult result = await service.ValidateDataset(request);

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
        public async Task OnValidateDataset_GivenTableResultsContainsThreeErrors_ReturnsOKResultWithMessage()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";
            const string operationId = "operationId";

            GetDatasetBlobModel model = new GetDatasetBlobModel
            {
                DatasetId = "dataset-id",
                Version = 1,
                Filename = "ds.xlsx"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            Message message = new Message(byteArray);
            message.UserProperties.Add("jobId", "job1");
            message.UserProperties.Add("operation-id", operationId);

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
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
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

            DatasetService service = CreateDatasetService(
                logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                datasetUploadValidator: datasetUploadValidator,
                cacheProvider: cacheProvider);

            // Act
            Func<Task> result = async () => { await service.ValidateDataset(message); };


            // Assert
            result
                .Should().NotThrow();

            await cacheProvider
                .Received(1)
                .SetAsync(Arg.Is($"{CacheKeys.DatasetValidationStatus}:{operationId}"), Arg.Is<DatasetValidationStatusModel>(v =>
                v.OperationId == operationId &&
                v.ValidationFailures.Count == 3));
        }

        [TestMethod]
        public void OnValidateDataset_GivenTableResultsAndMetadatValidatesButFailsToSave_ReturnsInternalServerError()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            const string dataDefinitionId = "definition-id";
            const string authorId = "author-id";
            const string authorName = "author-name";
            const string datasetId = "dataset-id";
            const string name = "name";
            const string description = "test description";
            const string operationId = "operationId";

            IDictionary<string, string> metaData = new Dictionary<string, string>
                {
                    { "dataDefinitionId", dataDefinitionId },
                    { "authorName", authorName },
                    { "authorId", authorId },
                    { "datasetId", datasetId },
                    { "name", name },
                    { "description", description },
                };

            GetDatasetBlobModel model = new GetDatasetBlobModel
            {
                DatasetId = "dataset-id",
                Version = 1,
                Filename = "ds.xlsx"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            Message message = new Message(byteArray);
            message.UserProperties.Add("jobId", "job1");
            message.UserProperties.Add("operation-id", operationId);

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
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
                .Returns(datasetDefinitions);
            datasetRepository
               .SaveDataset(Arg.Any<Dataset>())
               .Returns(HttpStatusCode.InternalServerError);

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult{ GlobalErrors = new List<DatasetValidationError>() }
            };

            DatasetService service = CreateDatasetService(
                logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository
                );

            // Act
            Func<Task> result = async () => { await service.ValidateDataset(message); };

            // Assert
            result
                .Should().Throw<InvalidOperationException>()
                .WithMessage($"Failed to save dataset for id: {datasetId} with status code InternalServerError");

            logger
                .Received(1)
                .Error(Arg.Is($"Failed to save dataset for id: {datasetId} with status code InternalServerError"));

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is("Failed to save the dataset or dataset version during validation"));
        }

        [TestMethod]
        public void OnValidateDataset_GivenTableResultsAndMetadatValidatesButFailsToSaveToSearch_ReturnsInternalServerError()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            const string dataDefinitionId = "definition-id";
            const string authorId = "author-id";
            const string authorName = "author-name";
            const string datasetId = "dataset-id";
            const string name = "name";
            const string description = "test description";
            const string operationId = "operationId";

            IDictionary<string, string> metaData = new Dictionary<string, string>
                {
                    { "dataDefinitionId", dataDefinitionId },
                    { "authorName", authorName },
                    { "authorId", authorId },
                    { "datasetId", datasetId },
                    { "name", name },
                    { "description", description },
                };

            GetDatasetBlobModel model = new GetDatasetBlobModel
            {
                DatasetId = "dataset-id",
                Version = 1,
                Filename = "ds.xlsx"
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            Message message = new Message(byteArray);
            message.UserProperties.Add("jobId", "job1");
            message.UserProperties.Add("operation-id", operationId);

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
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
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

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Index(Arg.Any<List<DatasetIndex>>())
                .Returns(indexErrors);

            DatasetService service = CreateDatasetService(
                logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                searchRepository: searchRepository);

            // Act
            Func<Task> result = async () => { await service.ValidateDataset(message); };

            // Assert
            result
                .Should().Throw<InvalidOperationException>()
                .WithMessage("Failed to save dataset for id: dataset-id in search with errors ");
        }

        [TestMethod]
        public async Task OnValidateDataset_GivenTableResultsAndMetadataValidatesAndSavesWhenFirstDatasetVersion_ThenValidationSuccessful()
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
            message.UserProperties.Add("jobId", "job1");
            message.UserProperties.Add("operation-id", operationId);

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
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
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

            DatasetService service = CreateDatasetService(logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                 searchRepository: searchRepository,
                 cacheProvider: cacheProvider);

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
        }

        [TestMethod]
        public async Task OnValidateDataset_GivenTableResultsAndMetadataValidatesAndSavesWhenUpdatingExistingDataset_ThenValidationSuccessful()
        {
            //Arrange
            const int newDatasetVersion = 2;
            const string filename = "ds.xlsx";

            const string blobPath = "dataset-id/v2/ds.xlsx";

            const string dataDefinitionId = "definition-id";
            const string authorId = "author-id";
            const string authorName = "author-name";
            const string datasetId = "dataset-id";
            const string name = "name";
            const string initialDescription = "test description";
            const string updatedDescription = "Updated description";
            const string updateComment = "Update comment";
            const string operationId = "operationId";

            IDictionary<string, string> metaData = new Dictionary<string, string>
                {
                    { "dataDefinitionId", dataDefinitionId },
                    { "authorName", authorName },
                    { "authorId", authorId },
                    { "datasetId", datasetId },
                    { "name", name },
                    { "description", initialDescription },
                    { "comment", updateComment },
                };

            GetDatasetBlobModel model = new GetDatasetBlobModel
            {
                DatasetId = datasetId,
                Version = newDatasetVersion,
                Filename = "ds.xlsx",
                Comment = updateComment,
                Description = updatedDescription,
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            Message message = new Message(byteArray);
            message.UserProperties.Add("jobId", "job1");
            message.UserProperties.Add("operation-id", operationId);
            message.UserProperties.Add("user-id", authorId);
            message.UserProperties.Add("user-name", authorName);

            ILogger logger = CreateLogger();

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);

            blob
                .Name
                .Returns(filename);

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
                Id = dataDefinitionId,
                Name = "Dataset Definition Name",
            };

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                datasetDefinition
            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
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

            DatasetService service = CreateDatasetService(
                logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                searchRepository: searchRepository,
                cacheProvider: cacheProvider);

            DatasetVersion existingDatasetVersion = new DatasetVersion()
            {
                Version = 1,
            };

            Dataset existingDataset = new Dataset()
            {
                Id = datasetId,
                Current = existingDatasetVersion,
                History = new List<DatasetVersion>() { existingDatasetVersion },
                Definition = new Reference(datasetDefinition.Id, datasetDefinition.Name),
                Description = initialDescription,
                Name = name,
                Published = null,
            };

            datasetRepository
                .GetDatasetByDatasetId(Arg.Is(datasetId))
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
                    d.Definition.Id == dataDefinitionId &&
                    d.Current.Author.Name == authorName &&
                    d.Current.Author.Id == authorId &&
                    d.Name == name
                ));

            await searchRepository
                .Received(1)
                .Index(Arg.Is<IEnumerable<DatasetIndex>>(
                    d => d.First().Id == datasetId &&
                    d.First().DefinitionId == dataDefinitionId &&
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
                     s.OperationId == operationId
                     ));
        }

        [TestMethod]
        public void OnValidateDataset_GivenTableResultsAndMetadataValidatesAndProvidedVersionIsNotDeterminedToBeNext_ThenExceptionIsThrown()
        {
            //Arrange
            const int providedNewDatasetVersion = 2;
            const string filename = "ds.xlsx";

            const string blobPath = "dataset-id/v2/ds.xlsx";

            const string dataDefinitionId = "definition-id";
            const string authorId = "author-id";
            const string authorName = "author-name";
            const string datasetId = "dataset-id";
            const string name = "name";
            const string initialDescription = "test description";
            const string updatedDescription = "Updated description";
            const string updateComment = "Update comment";
            const string operationId = "operationId";

            IDictionary<string, string> metaData = new Dictionary<string, string>
                {
                    { "dataDefinitionId", dataDefinitionId },
                    { "authorName", authorName },
                    { "authorId", authorId },
                    { "datasetId", datasetId },
                    { "name", name },
                    { "description", initialDescription },
                };

            GetDatasetBlobModel model = new GetDatasetBlobModel
            {
                DatasetId = datasetId,
                Version = providedNewDatasetVersion,
                Filename = "ds.xlsx",
                Comment = updateComment,
                Description = updatedDescription,
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            Message message = new Message(byteArray);
            message.UserProperties.Add("jobId", "job1");
            message.UserProperties.Add("operation-id", operationId);

            ILogger logger = CreateLogger();

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);

            blob
                .Name
                .Returns(filename);

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
                Id = dataDefinitionId,
                Name = "Dataset Definition Name",
            };

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                datasetDefinition
            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
                .Returns(datasetDefinitions);

            datasetRepository
                .SaveDataset(Arg.Any<Dataset>())
                .Returns(HttpStatusCode.OK);

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult{ GlobalErrors = new List<DatasetValidationError>() }
            };

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();

            DatasetService service = CreateDatasetService(logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                 searchRepository: searchRepository);

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
                Id = datasetId,
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
                .GetDatasetByDatasetId(Arg.Is(datasetId))
                .Returns(existingDataset);

            // Act
            Func<Task> result = async () => { await service.ValidateDataset(message); };

            // Assert
            result
               .Should().Throw<InvalidOperationException>()
               .WithMessage("Failed to save dataset or dataset version for id: dataset-id due to version mismatch. Expected next version to be 3 but request provided '2'");

            logger
                .Received(1)
                .Error(Arg.Is($"Failed to save dataset or dataset version for id: {model.DatasetId} due to version mismatch. Expected next version to be 3 but request provided '2'"));
        }

        [TestMethod]
        public void OnValidateDataset_GivenTableResultsAndMetadataValidatesAndExistingDatasetIsNull_ThenExceptionIsThrown()
        {
            //Arrange
            const int providedNewDatasetVersion = 2;
            const string filename = "ds.xlsx";

            const string blobPath = "dataset-id/v2/ds.xlsx";

            const string dataDefinitionId = "definition-id";
            const string authorId = "author-id";
            const string authorName = "author-name";
            const string datasetId = "dataset-id";
            const string name = "name";
            const string initialDescription = "test description";
            const string updatedDescription = "Updated description";
            const string updateComment = "Update comment";
            const string operationId = "operationID";

            IDictionary<string, string> metaData = new Dictionary<string, string>
                {
                    { "dataDefinitionId", dataDefinitionId },
                    { "authorName", authorName },
                    { "authorId", authorId },
                    { "datasetId", datasetId },
                    { "name", name },
                    { "description", initialDescription },
                };

            GetDatasetBlobModel model = new GetDatasetBlobModel
            {
                DatasetId = datasetId,
                Version = providedNewDatasetVersion,
                Filename = "ds.xlsx",
                Comment = updateComment,
                Description = updatedDescription,
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("operation-id", operationId);
            message.UserProperties.Add("jobId", "job1");

            ILogger logger = CreateLogger();

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);

            blob
                .Name
                .Returns(filename);

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
                Id = dataDefinitionId,
                Name = "Dataset Definition Name",
            };

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                datasetDefinition
            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
                .Returns(datasetDefinitions);

            datasetRepository
                .SaveDataset(Arg.Any<Dataset>())
                .Returns(HttpStatusCode.OK);

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult{ GlobalErrors = new List<DatasetValidationError>() }
            };

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();

            DatasetService service = CreateDatasetService(logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                 searchRepository: searchRepository);

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
                .GetDatasetByDatasetId(Arg.Is(datasetId))
                .Returns(existingDataset);

            // Act
            Func<Task> action = async () => { await service.ValidateDataset(message); };

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
            const int newDatasetVersion = 2;
            const string filename = "ds.xlsx";

            const string blobPath = "dataset-id/v2/ds.xlsx";

            const string dataDefinitionId = "definition-id";
            const string authorId = "author-id";
            const string authorName = "author-name";
            const string datasetId = "dataset-id";
            const string name = "name";
            const string initialDescription = "test description";
            const string updatedDescription = "Updated description";
            const string updateComment = "Update comment";
            const string operationId = "operationId";

            IDictionary<string, string> metaData = new Dictionary<string, string>
                {
                    { "dataDefinitionId", dataDefinitionId },
                    { "authorName", authorName },
                    { "authorId", authorId },
                    { "datasetId", datasetId },
                    { "name", name },
                    { "description", initialDescription },
                };

            GetDatasetBlobModel model = new GetDatasetBlobModel
            {
                DatasetId = datasetId,
                Version = newDatasetVersion,
                Filename = "ds.xlsx",
                Comment = updateComment,
                Description = updatedDescription,
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            Message message = new Message(byteArray);
            message.UserProperties.Add("jobId", "job1");
            message.UserProperties.Add("operation-id", operationId);

            ILogger logger = CreateLogger();

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);

            blob
                .Name
                .Returns(filename);

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
                Id = dataDefinitionId,
                Name = "Dataset Definition Name",
            };

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                datasetDefinition
            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
                .Returns(datasetDefinitions);

            datasetRepository
                .SaveDataset(Arg.Any<Dataset>())
                .Returns(HttpStatusCode.InternalServerError);

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult{ GlobalErrors = new List<DatasetValidationError>() }
            };

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();

            DatasetService service = CreateDatasetService(logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                 searchRepository: searchRepository);

            DatasetVersion existingDatasetVersion = new DatasetVersion()
            {
                Version = 1,
            };

            Dataset existingDataset = new Dataset()
            {
                Id = datasetId,
                Current = existingDatasetVersion,
                History = new List<DatasetVersion>() { existingDatasetVersion },
                Definition = new Reference(datasetDefinition.Id, datasetDefinition.Name),
                Description = initialDescription,
                Name = name,
                Published = null,
            };

            datasetRepository
                .GetDatasetByDatasetId(Arg.Is(datasetId))
                .Returns(existingDataset);

            // Act
            Func<Task> result = async () => { await service.ValidateDataset(message); };

            // Assert
            result
                .Should().Throw<InvalidOperationException>()
                .WithMessage("Failed to save dataset for id: dataset-id with status code InternalServerError");

            logger
                .Received(1)
                .Warning(Arg.Is("Failed to save dataset for id: dataset-id with status code InternalServerError"));
        }

        [TestMethod]
        public void OnValidateDataset_GivenTableResultsAndMetadataValidatesAndSavesWhenUpdatingSearchThenErrorIsThrown()
        {
            //Arrange
            const int newDatasetVersion = 2;
            const string filename = "ds.xlsx";

            const string blobPath = "dataset-id/v2/ds.xlsx";

            const string dataDefinitionId = "definition-id";
            const string authorId = "author-id";
            const string authorName = "author-name";
            const string datasetId = "dataset-id";
            const string name = "name";
            const string initialDescription = "test description";
            const string updatedDescription = "Updated description";
            const string updateComment = "Update comment";
            const string operationId = "operationId";

            IDictionary<string, string> metaData = new Dictionary<string, string>
                {
                    { "dataDefinitionId", dataDefinitionId },
                    { "authorName", authorName },
                    { "authorId", authorId },
                    { "datasetId", datasetId },
                    { "name", name },
                    { "description", initialDescription },
                };

            GetDatasetBlobModel model = new GetDatasetBlobModel
            {
                DatasetId = datasetId,
                Version = newDatasetVersion,
                Filename = "ds.xlsx",
                Comment = updateComment,
                Description = updatedDescription,
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            Message message = new Message(byteArray);
            message.UserProperties.Add("jobId", "job1");
            message.UserProperties.Add("operation-id", operationId);

            ILogger logger = CreateLogger();

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);

            blob
                .Name
                .Returns(filename);

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
                Id = dataDefinitionId,
                Name = "Dataset Definition Name",
            };

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                datasetDefinition
            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
                .Returns(datasetDefinitions);

            datasetRepository
                .SaveDataset(Arg.Any<Dataset>())
                .Returns(HttpStatusCode.OK);

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult{ GlobalErrors = new List<DatasetValidationError>() }
            };

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();

            DatasetService service = CreateDatasetService(logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                 searchRepository: searchRepository);

            DatasetVersion existingDatasetVersion = new DatasetVersion()
            {
                Version = 1,
            };

            Dataset existingDataset = new Dataset()
            {
                Id = datasetId,
                Current = existingDatasetVersion,
                History = new List<DatasetVersion>() { existingDatasetVersion },
                Definition = new Reference(datasetDefinition.Id, datasetDefinition.Name),
                Description = initialDescription,
                Name = name,
                Published = null,
            };

            datasetRepository
                .GetDatasetByDatasetId(Arg.Is(datasetId))
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
            Func<Task> result = async () => { await service.ValidateDataset(message); };

            // Assert
            result
                .Should().Throw<InvalidOperationException>()
                .WithMessage("Failed to save dataset for id: dataset-id in search with errors Error in dataset ID for search");

            logger
                .Received(1)
                .Warning(Arg.Is("Failed to save dataset for id: dataset-id in search with errors Error in dataset ID for search"));
        }

        [TestMethod]
        public async Task OnValidateDataset_WhenNoOperationIdProvided_ThenErrorLogged()
        {
            // Arrange
            GetDatasetBlobModel model = new GetDatasetBlobModel
            {

            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            Message message = new Message(byteArray);

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
            GetDatasetBlobModel model = new GetDatasetBlobModel
            {

            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            Message message = new Message(byteArray);

            message.UserProperties.Add("operation-id", null);

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
            GetDatasetBlobModel model = new GetDatasetBlobModel
            {

            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            Message message = new Message(byteArray);

            message.UserProperties.Add("operation-id", string.Empty);

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
            const string operationId = "operationId";

            GetDatasetBlobModel model = null;

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            Message message = new Message(byteArray);
            message.UserProperties.Add("jobId", "job1");
            message.UserProperties.Add("operation-id", operationId);

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
                    s.OperationId == operationId
                    ));

            await cacheProvider
                 .Received(1)
                 .SetAsync<DatasetValidationStatusModel>(
                 Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                 Arg.Is<DatasetValidationStatusModel>(s =>
                     s.CurrentOperation == DatasetValidationStatus.FailedValidation &&
                     s.ErrorMessage == "Null model was provided to ValidateDataset" &&
                     s.OperationId == operationId
                     ));
        }

        [TestMethod]
        public async Task OnValidateDataset_WhenGetDatasetBlobModelValidationIsNull_ThenErrorLogged()
        {
            // Arrange
            const string operationId = "operationId";

            GetDatasetBlobModel model = new GetDatasetBlobModel()
            {

            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            Message message = new Message(byteArray);
            message.UserProperties.Add("jobId", "job1");
            message.UserProperties.Add("operation-id", operationId);

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
                    s.OperationId == operationId
                    ));

            await cacheProvider
                 .Received(1)
                 .SetAsync<DatasetValidationStatusModel>(
                 Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                 Arg.Is<DatasetValidationStatusModel>(s =>
                     s.CurrentOperation == DatasetValidationStatus.FailedValidation &&
                     s.ErrorMessage == "GetDatasetBlobModel validation result returned null" &&
                     s.OperationId == operationId
                     ));
        }

        [TestMethod]
        public async Task OnValidateDataset_WhenGetDatasetBlobModelValidationReturnsError_ThenErrorLogged()
        {
            // Arrange
            const string operationId = "operationId";

            GetDatasetBlobModel model = new GetDatasetBlobModel()
            {

            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            Message message = new Message(byteArray);
            message.UserProperties.Add("jobId", "job1");
            message.UserProperties.Add("operation-id", operationId);

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
                    s.OperationId == operationId
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
                     s.OperationId == operationId
                     ));

        }

        [TestMethod]
        public async Task OnValidateDataset_WhenGetBlobReferenceFromServerAsyncReturnsNull_ThenErrorLogged()
        {
            // Arrange
            const string operationId = "operationId";

            GetDatasetBlobModel model = new GetDatasetBlobModel()
            {
                Filename = "filename.xlsx",
                Version = 1,
                DatasetId = "dsid",
                DefinitionId = "defId",
            };


            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            Message message = new Message(byteArray);
            message.UserProperties.Add("jobId", "job1");
            message.UserProperties.Add("operation-id", operationId);

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
                .Error(Arg.Is("Failed to find blob with path: dsid/v1/filename.xlsx"));

            await cacheProvider
                .Received(1)
                .SetAsync<DatasetValidationStatusModel>(
                Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                Arg.Is<DatasetValidationStatusModel>(s =>
                    s.CurrentOperation == DatasetValidationStatus.Processing &&
                    s.OperationId == operationId
                    ));

            await cacheProvider
                 .Received(1)
                 .SetAsync<DatasetValidationStatusModel>(
                 Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                 Arg.Is<DatasetValidationStatusModel>(s =>
                     s.CurrentOperation == DatasetValidationStatus.FailedValidation &&
                     s.ErrorMessage == "Failed to find blob with path: dsid/v1/filename.xlsx" &&
                     s.OperationId == operationId
                     ));
        }

        [TestMethod]
        public async Task OnValidateDataset_WhenDownloadToStreamAsyncReturnsNull_ThenErrorLogged()
        {
            // Arrange
            const string operationId = "operationId";

            GetDatasetBlobModel model = new GetDatasetBlobModel()
            {
                Filename = "filename.xlsx",
                Version = 1,
                DatasetId = "dsid",
                DefinitionId = "defId",
            };


            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            Message message = new Message(byteArray);
            message.UserProperties.Add("jobId", "job1");
            message.UserProperties.Add("operation-id", operationId);

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
                .Error(Arg.Is("Blob dsid/v1/filename.xlsx contains no data"));

            await cacheProvider
                .Received(1)
                .SetAsync<DatasetValidationStatusModel>(
                Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                Arg.Is<DatasetValidationStatusModel>(s =>
                    s.CurrentOperation == DatasetValidationStatus.Processing &&
                    s.OperationId == operationId
                    ));

            await cacheProvider
                 .Received(1)
                 .SetAsync<DatasetValidationStatusModel>(
                 Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                 Arg.Is<DatasetValidationStatusModel>(s =>
                     s.CurrentOperation == DatasetValidationStatus.FailedValidation &&
                     s.ErrorMessage == "Blob dsid/v1/filename.xlsx contains no data" &&
                     s.OperationId == operationId
                     ));
        }

        [TestMethod]
        public async Task OnValidateDataset_WhenExcelValidationErrorsReturned_ThenErrorLogged()
        {
            const string testXlsxLocation = @"TestItems/TestCheck.xlsx";

            // Arrange
            const string operationId = "operationId";

            GetDatasetBlobModel model = new GetDatasetBlobModel()
            {
                Filename = "filename.xlsx",
                Version = 1,
                DatasetId = "dsid",
                DefinitionId = "defId",
            };


            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            Message message = new Message(byteArray);
            message.UserProperties.Add("jobId", "job1");
            message.UserProperties.Add("operation-id", operationId);

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
                    s.OperationId == operationId
                    ));

            await cacheProvider
                .Received(1)
                .SetAsync<DatasetValidationStatusModel>(
                Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                Arg.Is<DatasetValidationStatusModel>(s =>
                    s.CurrentOperation == DatasetValidationStatus.ValidatingExcelWorkbook &&
                    s.OperationId == operationId
                    ));

            await cacheProvider
                 .Received(1)
                 .SetAsync<DatasetValidationStatusModel>(
                 Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                 Arg.Is<DatasetValidationStatusModel>(s =>
                     s.CurrentOperation == DatasetValidationStatus.FailedValidation &&
                     s.ValidationFailures.Count == 1 &&
                     s.OperationId == operationId
                     ));
        }

        [TestMethod]
        public async Task OnValidateDataset_WhenExceptionThrownDuringExcelValidation_ThenErrorLogged()
        {
            // Arrange
            const string operationId = "operationId";

            GetDatasetBlobModel model = new GetDatasetBlobModel()
            {
                Filename = "filename.xlsx",
                Version = 1,
                DatasetId = "dsid",
                DefinitionId = "defId",
            };


            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            Message message = new Message(byteArray);
            message.UserProperties.Add("jobId", "job1");
            message.UserProperties.Add("operation-id", operationId);

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
                    s.OperationId == operationId
                    ));

            await cacheProvider
                 .Received(1)
                 .SetAsync<DatasetValidationStatusModel>(
                 Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                 Arg.Is<DatasetValidationStatusModel>(s =>
                     s.CurrentOperation == DatasetValidationStatus.FailedValidation &&
                     s.ValidationFailures.Count == 2 &&
                     s.OperationId == operationId
                     ));
        }

        [TestMethod]
        public async Task OnValidateDataset_WhenDatasetDefinitionLookupReturnsNull_ThenErrorLogged()
        {
            const string testXlsxLocation = @"TestItems/TestCheck.xlsx";

            // Arrange
            const string operationId = "operationId";

            GetDatasetBlobModel model = new GetDatasetBlobModel()
            {
                Filename = "filename.xlsx",
                Version = 1,
                DatasetId = "dsid",
                DefinitionId = "defId",
            };


            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            Message message = new Message(byteArray);
            message.UserProperties.Add("jobId", "job1");
            message.UserProperties.Add("operation-id", operationId);

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
            blobMetadata.Add("dataDefinitionId", model.DefinitionId);

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
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
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
                .Error(Arg.Is("Unable to find a data definition for id: defId, for blob: dsid/v1/filename.xlsx"));

            await cacheProvider
                .Received(1)
                .SetAsync<DatasetValidationStatusModel>(
                Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                Arg.Is<DatasetValidationStatusModel>(s =>
                    s.CurrentOperation == DatasetValidationStatus.Processing &&
                    s.OperationId == operationId
                    ));

            await cacheProvider
                .Received(1)
                .SetAsync<DatasetValidationStatusModel>(
                Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                Arg.Is<DatasetValidationStatusModel>(s =>
                    s.CurrentOperation == DatasetValidationStatus.ValidatingExcelWorkbook &&
                    s.OperationId == operationId
                    ));

            await cacheProvider
                 .Received(1)
                 .SetAsync<DatasetValidationStatusModel>(
                 Arg.Is<string>(a => a.StartsWith(CacheKeys.DatasetValidationStatus)),
                 Arg.Is<DatasetValidationStatusModel>(s =>
                     s.CurrentOperation == DatasetValidationStatus.FailedValidation &&
                     s.ErrorMessage == "Unable to find a data definition for id: defId, for blob: dsid/v1/filename.xlsx" &&
                     s.OperationId == operationId
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
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
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

            DatasetService service = CreateDatasetService(logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                searchRepository: searchRepository,
                cacheProvider: cacheProvider,
                jobsApiClient: jobsApiClient);

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

            await jobsApiClient
                .Received(1)
                .AddJobLog(Arg.Is<string>(jobId), Arg.Is<JobLogUpdateModel>(j => j.ItemsProcessed == 0 && !j.CompletedSuccessfully.HasValue));
            await jobsApiClient
                .Received(1)
                .AddJobLog(Arg.Is<string>(jobId), Arg.Is<JobLogUpdateModel>(j => j.ItemsProcessed == 25 && !j.CompletedSuccessfully.HasValue));
            await jobsApiClient
                .Received(1)
                .AddJobLog(Arg.Is<string>(jobId), Arg.Is<JobLogUpdateModel>(j => j.ItemsProcessed == 50 && !j.CompletedSuccessfully.HasValue));
            await jobsApiClient
                .Received(1)
                .AddJobLog(Arg.Is<string>(jobId), Arg.Is<JobLogUpdateModel>(j => j.ItemsProcessed == 75 && !j.CompletedSuccessfully.HasValue));
            await jobsApiClient
                .Received(1)
                .AddJobLog(Arg.Is<string>(jobId), Arg.Is<JobLogUpdateModel>(j => j.ItemsProcessed == 100 && j.CompletedSuccessfully == true));
        }
    }
}
