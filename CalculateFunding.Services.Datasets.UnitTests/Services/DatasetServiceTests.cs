using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Models.MappingProfiles;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.DataImporter.Validators.Models;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Primitives;
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
    public class DatasetServiceTests
    {
        const string DatasetName = "test-dataset";
        const string Username = "test-user";
        const string UserId = "33d7a71b-f570-4425-801b-250b9129f3d3";
        const string DataDefintionId = "45d7a71b-f570-4425-801b-250b9129f124";
        const string SpecificationId = "d557a71b-f570-4425-801b-250b9129f111";
        const string BuildProjectId = "d557a71b-f570-4425-801b-250b9129f111";
        const string DatasetId = "e557a71b-f570-4436-801b-250b9129f999";

        [TestMethod]
        public async Task GetDatasetByName_GivenDatasetNameDoesNotExist_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            DatasetService service = CreateDatasetService(logger: logger);

            // Act
            IActionResult result = await service.GetDatasetByName(request);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No dataset name was provided to GetDatasetByName"));
        }

        [TestMethod]
        public async Task GetDatasetByName_GivenDatasetWasNotFound_ReturnsNotFound()
        {
            //Arrange
            IEnumerable<Dataset> datasets = Enumerable.Empty<Dataset>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "datasetName", new StringValues(DatasetName) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            IDatasetRepository datasetsRepository = CreateDatasetsRepository();
            datasetsRepository
                 .GetDatasetsByQuery(Arg.Any<Expression<Func<Dataset, bool>>>())
                 .Returns(datasets);

            DatasetService service = CreateDatasetService(datasetRepository: datasetsRepository, logger: logger);

            // Act
            IActionResult result = await service.GetDatasetByName(request);

            // Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Information(Arg.Is($"Dataset was not found for name: {DatasetName}"));
        }

        [TestMethod]
        public async Task GetDatasetByName_GivenDatasetWasFound_ReturnsOkResult()
        {
            //Arrange
            IEnumerable<Dataset> datasets = new[]
            {
                new Dataset()
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "datasetName", new StringValues(DatasetName) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            IDatasetRepository datasetsRepository = CreateDatasetsRepository();
            datasetsRepository
                 .GetDatasetsByQuery(Arg.Any<Expression<Func<Dataset, bool>>>())
                 .Returns(datasets);

            DatasetService service = CreateDatasetService(datasetRepository: datasetsRepository, logger: logger);

            // Act
            IActionResult result = await service.GetDatasetByName(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            Dataset objContent = (Dataset)((OkObjectResult)result).Value;

            objContent
                .Should()
                .NotBeNull();

            logger
                .Received(1)
                .Information(Arg.Is($"Dataset found for name: {DatasetName}"));
        }

        [TestMethod]
        public async Task CreateNewDataset_GivenNullModel_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            DatasetService service = CreateDatasetService(logger: logger);

            // Act
            IActionResult result = await service.CreateNewDataset(request);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("Null model name was provided to CreateNewDataset"));
        }

        [TestMethod]
        public async Task CreateNewDataset_GivenInvalidModel_ReturnsBadRequest()
        {
            //Arrange
            CreateNewDatasetModel model = new CreateNewDatasetModel();
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

            IValidator<CreateNewDatasetModel> validator = CreateNewDatasetModelValidator(validationResult);


            DatasetService service = CreateDatasetService(logger: logger, createNewDatasetModelValidator: validator);

            // Act
            IActionResult result = await service.CreateNewDataset(request);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task CreateNewDataset_GivenValidModel_ReturnsOKResult()
        {
            //Arrange
            const string blobUrl = "http://this-is-a-bloburl?this=is-a-token";

            CreateNewDatasetModel model = new CreateNewDatasetModel
            {
                Filename = "test.xlsx"
            };
            NewDatasetVersionResponseModel responseModel = new NewDatasetVersionResponseModel
            {
                Filename = "test.xlsx"
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            ClaimsPrincipal principle = new ClaimsPrincipal(new[]
            {
                new ClaimsIdentity(new []{ new Claim(ClaimTypes.Sid, UserId), new Claim(ClaimTypes.Name, Username) })
            });

            HttpContext context = Substitute.For<HttpContext>();
            context
                .User
                .Returns(principle);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);
            request
                .HttpContext
                .Returns(context);

            ILogger logger = CreateLogger();

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobSasUrl(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<SharedAccessBlobPermissions>())
                .Returns(blobUrl);

            IMapper mapper = CreateMapper();
            mapper
                .Map<NewDatasetVersionResponseModel>(Arg.Any<CreateNewDatasetModel>())
                .Returns(responseModel);

            DatasetService service = CreateDatasetService(logger: logger, blobClient: blobClient, mapper: mapper);

            // Act
            IActionResult result = await service.CreateNewDataset(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            responseModel
                .DatasetId
                .Should()
                .NotBeNullOrWhiteSpace();

            responseModel
                .BlobUrl
                .Should()
                .Be(blobUrl);

            responseModel
                .Author
                .Name
                .Should()
                .Be(Username);

            responseModel
               .Author
               .Id
               .Should()
               .Be(UserId);
        }

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

            IExcelDatasetReader datasetReader = CreateExcelDatasetReader();
            datasetReader
                .Read(Arg.Any<Stream>(), Arg.Is(datasetDefinition))
                .Returns(tableLoadResults.ToList());

            ICacheProvider cacheProvider = CreateCacheProvider();

            DatasetService service = CreateDatasetService(
                logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                excelDatasetReader: datasetReader,
                datasetUploadValidator: datasetUploadValidator,
                cacheProvider: cacheProvider);

            // Act
            Func<Task> result = async () => { await service.ValidateDataset(message); };

            // Assert
            result
               .ShouldNotThrow();

            await cacheProvider
                .Received(1)
                .SetAsync(Arg.Is($"{CacheKeys.DatasetValidationStatus}:{operationId}"), Arg.Is<DatasetValidationStatusModel>(v =>
                v.OperationId == operationId &&
                v.ValidationFailures.Count == 3));
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

            DatasetService service = CreateDatasetService(logger: logger, blobClient: blobClient, datasetRepository: datasetRepository,
                excelDatasetReader: datasetReader, datasetUploadValidator: datasetUploadValidator,
                cacheProvider: cacheProvider);

            // Act
            Func<Task> result = async () => { await service.ValidateDataset(message); };


            // Assert
            result
                .ShouldNotThrow();

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

            IExcelDatasetReader datasetReader = CreateExcelDatasetReader();
            datasetReader
                .Read(Arg.Any<Stream>(), Arg.Is(datasetDefinition))
                .Returns(tableLoadResults.ToList());

            DatasetService service = CreateDatasetService(logger: logger, blobClient: blobClient, datasetRepository: datasetRepository,
                excelDatasetReader: datasetReader);

            // Act
            Func<Task> result = async () => { await service.ValidateDataset(message); };

            // Assert
            result
                .ShouldThrow<InvalidOperationException>()
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

            DatasetService service = CreateDatasetService(logger: logger, blobClient: blobClient, datasetRepository: datasetRepository,
                excelDatasetReader: datasetReader, searchRepository: searchRepository);

            // Act
            Func<Task> result = async () => { await service.ValidateDataset(message); };

            // Assert
            result
                .ShouldThrow<InvalidOperationException>()
                .WithMessage("Failed to save dataset for id: dataset-id in search with errors ");
        }

        [TestMethod]
        public async Task OnValidateDataset_GivenTableResultsAndMetadataValidatesAndSavesWhenFirstDatasetVersionReturnsOKResult()
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
                    { "description", description },
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

            IExcelDatasetReader datasetReader = CreateExcelDatasetReader();
            datasetReader
                .Read(Arg.Any<Stream>(), Arg.Is(datasetDefinition))
                .Returns(tableLoadResults.ToList());

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();

            DatasetService service = CreateDatasetService(logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                excelDatasetReader: datasetReader,
                 searchRepository: searchRepository);

            // Act
            Func<Task> result = async () => { await service.ValidateDataset(message); };

            // Assert
            result
                .ShouldNotThrow();

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
                d.Current.Commment == null
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
                    d.First().Description == description
                    ));
        }

        [TestMethod]
        public async Task OnValidateDataset_GivenTableResultsAndMetadataValidatesAndSavesWhenUpdatingExistingDatasetReturnsOKResult()
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
            const int rowcount = 20;
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

            IExcelDatasetReader datasetReader = CreateExcelDatasetReader();
            datasetReader
                .Read(Arg.Any<Stream>(), Arg.Is(datasetDefinition))
                .Returns(tableLoadResults.ToList());

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();

            DatasetService service = CreateDatasetService(logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                excelDatasetReader: datasetReader,
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
                .ShouldNotThrow();

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
                d.Current.Commment == updateComment
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
                    d.First().Description == updatedDescription
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

            IExcelDatasetReader datasetReader = CreateExcelDatasetReader();
            datasetReader
                .Read(Arg.Any<Stream>(), Arg.Is(datasetDefinition))
                .Returns(tableLoadResults.ToList());

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();

            DatasetService service = CreateDatasetService(logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                excelDatasetReader: datasetReader,
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
               .ShouldThrow<InvalidOperationException>()
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

            IExcelDatasetReader datasetReader = CreateExcelDatasetReader();
            datasetReader
                .Read(Arg.Any<Stream>(), Arg.Is(datasetDefinition))
                .Returns(tableLoadResults.ToList());

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();

            DatasetService service = CreateDatasetService(logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                excelDatasetReader: datasetReader,
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
                .ShouldThrow<InvalidOperationException>()
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

            IExcelDatasetReader datasetReader = CreateExcelDatasetReader();
            datasetReader
                .Read(Arg.Any<Stream>(), Arg.Is(datasetDefinition))
                .Returns(tableLoadResults.ToList());

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();

            DatasetService service = CreateDatasetService(logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                excelDatasetReader: datasetReader,
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
                .ShouldThrow<InvalidOperationException>()
                .WithMessage("Failed to save dataset for id: dataset-id with status code InternalServerError");

            logger
                .Received(1)
                .Warning(Arg.Is("Failed to save dataset for id: dataset-id with status code InternalServerError"));
        }

        [TestMethod]
        public async Task OnValidateDataset_GivenTableResultsAndMetadataValidatesAndSavesWhenUpdatingSearchThenErrorIsThrown()
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

            IExcelDatasetReader datasetReader = CreateExcelDatasetReader();
            datasetReader
                .Read(Arg.Any<Stream>(), Arg.Is(datasetDefinition))
                .Returns(tableLoadResults.ToList());

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();

            DatasetService service = CreateDatasetService(logger: logger,
                blobClient: blobClient,
                datasetRepository: datasetRepository,
                excelDatasetReader: datasetReader,
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
                .ShouldThrow<InvalidOperationException>()
                .WithMessage("Failed to save dataset for id: dataset-id in search with errors Error in dataset ID for search");

            logger
                .Received(1)
                .Warning(Arg.Is("Failed to save dataset for id: dataset-id in search with errors Error in dataset ID for search"));
        }

        [TestMethod]
        async public Task GetDatasetsByDefinitionId_WhenNoDefinitionIdIsProvided_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            DatasetService service = CreateDatasetService(logger: logger);

            // Act
            IActionResult result = await service.GetDatasetsByDefinitionId(request);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No definitionId was provided to GetDatasetsByDefinitionId"));
        }

        [TestMethod]
        async public Task GetDatasetsByDefinitionId_WhenNullDatasetsReturned_ReturnsOKResult()
        {
            // Arrange
            IEnumerable<Dataset> datasets = Enumerable.Empty<Dataset>();

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "definitionId", new StringValues(DataDefintionId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetsByQuery(Arg.Any<Expression<Func<Dataset, bool>>>())
                .Returns((IEnumerable<Dataset>)null);

            DatasetService service = CreateDatasetService(datasetRepository: datasetRepository);

            // Act
            IActionResult result = await service.GetDatasetsByDefinitionId(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okResult = result as OkObjectResult;

            IEnumerable<DatasetViewModel> data = okResult.Value as IEnumerable<DatasetViewModel>;

            data
                .Any()
                .Should()
                .BeFalse();
        }

        [TestMethod]
        async public Task GetDatasetsByDefinitionId_WhenDatasetsReturned_ReturnsOKResult()
        {
            //Arrange
            IEnumerable<Dataset> datasets = new[]
             {
                new Dataset()
            };

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "definitionId", new StringValues(DataDefintionId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetsByQuery(Arg.Any<Expression<Func<Dataset, bool>>>())
                .Returns(datasets);

            DatasetViewModel datasetViewModel = new DatasetViewModel();

            IMapper mapper = CreateMapper();
            mapper
                .Map<DatasetViewModel>(Arg.Any<Dataset>())
                .Returns(datasetViewModel);

            DatasetService service = CreateDatasetService(datasetRepository: datasetRepository, mapper: mapper);

            // Act
            IActionResult result = await service.GetDatasetsByDefinitionId(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okResult = result as OkObjectResult;

            IEnumerable<DatasetViewModel> data = okResult.Value as IEnumerable<DatasetViewModel>;

            data
                .Any()
                .Should()
                .BeTrue();

            data
                .Count()
                .Should()
                .Be(1);
        }

        [TestMethod]
        public void ProcessDataset_GivenNullMessage_ThrowsArgumentNullException()
        {
            //Arrange
            Message message = null;

            DatasetService service = CreateDatasetService();

            // Act
            Func<Task> test = () => service.ProcessDataset(message);

            // Assert
            test
                .ShouldThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void ProcessDataset_GivenNullPayload_ThrowsArgumentException()
        {
            //Arrange
            Message message = new Message(new byte[0]);

            DatasetService service = CreateDatasetService();

            // Act
            Func<Task> test = () => service.ProcessDataset(message);

            // Assert
            test
                .ShouldThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void ProcessDataset_GivenPayloadButNoSpecificationIdKeyinProperties_ThrowsKeyNotFoundException()
        {
            //Arrange
            Dataset dataset = new Dataset();

            var json = JsonConvert.SerializeObject(dataset);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            DatasetService service = CreateDatasetService();

            // Act
            Func<Task> test = () => service.ProcessDataset(message);

            // Assert
            test
                .ShouldThrowExactly<KeyNotFoundException>();
        }

        [TestMethod]
        public void ProcessDataset_GivenPayloadButNoSpecificationIdValueinProperties_ThrowsArgumentException()
        {
            //Arrange
            Dataset dataset = new Dataset();

            var json = JsonConvert.SerializeObject(dataset);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
                .UserProperties
                .Add("specification-id", "");

            DatasetService service = CreateDatasetService();

            // Act
            Func<Task> test = () => service.ProcessDataset(message);

            // Assert
            test
                .ShouldThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void ProcessDataset_GivenPayloadButDatasetDefinitionCouldNotBeFound_DoesNotProcess()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            DatasetVersion datasetVersion = new DatasetVersion { BlobName = blobPath, Version = 1, };

            Dataset dataset = new Dataset
            {
                Definition = new Reference { Id = DataDefintionId },
                Current = datasetVersion,
                History = new List<DatasetVersion>()
                {
                    datasetVersion,
                }
            };


            var json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
                .UserProperties
                .Add("specification-id", SpecificationId);

            message
               .UserProperties
               .Add("relationship-id", relationshipId);

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
                .Returns((IEnumerable<DatasetDefinition>)null);

            DefinitionSpecificationRelationship definitionSpecificationRelationship = new DefinitionSpecificationRelationship()
            {
                DatasetVersion = new DatasetRelationshipVersion()
                {
                    Version = 1,
                },
                DatasetDefinition = new Reference("df1", "Name"),
            };

            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(relationshipId))
                .Returns(definitionSpecificationRelationship);

            ILogger logger = CreateLogger();

            DatasetService service = CreateDatasetService(datasetRepository: datasetRepository, logger: logger);

            // Act
            Action action = () =>
            {
                service.ProcessDataset(message).Wait();
            };

            // Assert
            action
                .ShouldThrow<AggregateException>()
                .WithInnerException<Exception>()
                .WithMessage("One or more errors occurred. (Unable to find a data definition for id: 45d7a71b-f570-4425-801b-250b9129f124, for blob: dataset-id/v1/ds.xlsx)");

            logger
                .Received(1)
                .Error(Arg.Is($"Unable to find a data definition for id: {DataDefintionId}, for blob: {blobPath}"));

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Any<string>());
        }

        [TestMethod]
        public void ProcessDataset_GivenPayloadButBuildProjectCouldNotBeFound_DoesNotProcess()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            DatasetVersion datasetVersion = new DatasetVersion { BlobName = blobPath, Version = 1, };

            Dataset dataset = new Dataset
            {
                Definition = new Reference { Id = DataDefintionId },
                Current = datasetVersion,
                History = new List<DatasetVersion>()
                {
                    datasetVersion,
                }
            };

            var json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
                .UserProperties
                .Add("specification-id", SpecificationId);

            message
               .UserProperties
               .Add("relationship-id", relationshipId);

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                new DatasetDefinition{ Id = DataDefintionId }
            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
                .Returns(datasetDefinitions);

            ILogger logger = CreateLogger();

            ICalcsRepository calcsRepository = CreateCalcsRepository();
            calcsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns((BuildProject)null);

            DefinitionSpecificationRelationship definitionSpecificationRelationship = new DefinitionSpecificationRelationship()
            {
                DatasetVersion = new DatasetRelationshipVersion()
                {
                    Version = 1,
                },
                DatasetDefinition = new Reference(datasetDefinitions.First().Id, "Name"),
            };

            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(relationshipId))
                .Returns(definitionSpecificationRelationship);

            DatasetService service = CreateDatasetService(datasetRepository: datasetRepository, logger: logger, calcsRepository: calcsRepository);

            // Act
            Action action = () =>
            {
                service.ProcessDataset(message).Wait();
            };

            // Assert
            action
                .ShouldThrow<AggregateException>()
                .WithInnerException<Exception>()
                .WithMessage("One or more errors occurred. (Unable to find a build project for id: d557a71b-f570-4425-801b-250b9129f111)");

            logger
                .Received(1)
                .Error(Arg.Is($"Unable to find a build project for specification id: {SpecificationId}"));

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Any<string>());
        }

        [TestMethod]
        public void ProcessDataset_GivenPayloadButBlobNotFound_DoesNotProcess()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            DatasetVersion datasetVersion = new DatasetVersion { BlobName = blobPath, Version = 1, };

            Dataset dataset = new Dataset
            {
                Definition = new Reference { Id = DataDefintionId },
                Current = datasetVersion,
                History = new List<DatasetVersion>()
                {
                    datasetVersion,
                }
            };

            var json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
                .UserProperties
                .Add("specification-id", SpecificationId);

            message
               .UserProperties
               .Add("relationship-id", relationshipId);

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                new DatasetDefinition{ Id = DataDefintionId }
            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
                .Returns(datasetDefinitions);

            ILogger logger = CreateLogger();

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Is(blobPath))
                .Returns((ICloudBlob)null);

            BuildProject buildProject = new BuildProject();

            ICalcsRepository calcsRepository = CreateCalcsRepository();
            calcsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns(buildProject);

            DefinitionSpecificationRelationship definitionSpecificationRelationship = new DefinitionSpecificationRelationship()
            {
                DatasetVersion = new DatasetRelationshipVersion()
                {
                    Version = 1,
                },
                DatasetDefinition = new Reference(datasetDefinitions.First().Id, "Name"),
            };

            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(relationshipId))
                .Returns(definitionSpecificationRelationship);

            DatasetService service = CreateDatasetService(
                datasetRepository: datasetRepository, logger: logger,
                calcsRepository: calcsRepository, blobClient: blobClient);

            // Act
            Action action = () =>
            {
                service.ProcessDataset(message).Wait();
            };

            // Assert
            action
                .ShouldThrow<AggregateException>()
                .WithInnerException<Exception>()
                .WithMessage("One or more errors occurred. (Failed to find blob with path: dataset-id/v1/ds.xlsx)");

            logger
                .Received(1)
                .Error(Arg.Is($"Failed to find blob with path: {blobPath}"));

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Any<string>());
        }

        [TestMethod]
        public void ProcessDataset_GivenPayloadAndBlobFoundButEmptyFile_DoesNotProcess()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            DatasetVersion datasetVersion = new DatasetVersion { BlobName = blobPath, Version = 1 };

            Dataset dataset = new Dataset
            {
                Definition = new Reference { Id = DataDefintionId },
                Current = datasetVersion,
                History = new List<DatasetVersion>()
                {
                    datasetVersion
                }
            };

            var json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
                .UserProperties
                .Add("specification-id", SpecificationId);

            message
               .UserProperties
               .Add("relationship-id", relationshipId);

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                new DatasetDefinition{ Id = DataDefintionId, },

            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
                .Returns(datasetDefinitions);

            ILogger logger = CreateLogger();

            ICloudBlob blob = Substitute.For<ICloudBlob>();

            MemoryStream stream = new MemoryStream(new byte[0]);

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Is(blobPath))
                .Returns(blob);
            blobClient
                .DownloadToStreamAsync(Arg.Is(blob))
                .Returns(stream);

            BuildProject buildProject = new BuildProject();

            ICalcsRepository calcsRepository = CreateCalcsRepository();
            calcsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns(buildProject);

            DatasetService service = CreateDatasetService(
                datasetRepository: datasetRepository, logger: logger,
                calcsRepository: calcsRepository, blobClient: blobClient);

            DefinitionSpecificationRelationship definitionSpecificationRelationship = new DefinitionSpecificationRelationship()
            {
                DatasetVersion = new DatasetRelationshipVersion()
                {
                    Version = 1,
                },
                DatasetDefinition = new Reference(datasetDefinitions.First().Id, "Name"),
            };

            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(relationshipId))
                .Returns(definitionSpecificationRelationship);

            // Act
            Action action = () =>
           {
               service.ProcessDataset(message).Wait();
           };

            // Assert
            action
                .ShouldThrow<ArgumentException>()
                .WithMessage("Invalid blob returned: dataset-id/v1/ds.xlsx");

            logger
                .Received(1)
                .Error(Arg.Is($"Invalid blob returned: {blobPath}"));

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Any<string>());
        }

        [TestMethod]
        public void ProcessDataset_GivenPayloadAndBlobFoundButNoTableResultsReturned_DoesNotProcess()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            DatasetVersion datasetVersion = new DatasetVersion { BlobName = blobPath, Version = 1, };

            Dataset dataset = new Dataset
            {
                Definition = new Reference { Id = DataDefintionId },
                Current = datasetVersion,
                History = new List<DatasetVersion>()
                {
                    datasetVersion,
                }
            };

            var json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
                .UserProperties
                .Add("specification-id", SpecificationId);

            message
               .UserProperties
               .Add("relationship-id", relationshipId);

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                new DatasetDefinition{ Id = DataDefintionId }
            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
                .Returns(datasetDefinitions);

            ILogger logger = CreateLogger();

            ICloudBlob blob = Substitute.For<ICloudBlob>();

            MemoryStream stream = new MemoryStream(new byte[100]);

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Is(blobPath))
                .Returns(blob);
            blobClient
                .DownloadToStreamAsync(Arg.Is(blob))
                .Returns(stream);

            BuildProject buildProject = new BuildProject();

            ICalcsRepository calcsRepository = CreateCalcsRepository();
            calcsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns(buildProject);

            DatasetService service = CreateDatasetService(
                datasetRepository: datasetRepository, logger: logger,
                calcsRepository: calcsRepository, blobClient: blobClient);

            DefinitionSpecificationRelationship definitionSpecificationRelationship = new DefinitionSpecificationRelationship()
            {
                DatasetVersion = new DatasetRelationshipVersion()
                {
                    Version = 1,
                },
                DatasetDefinition = new Reference(datasetDefinitions.First().Id, "Name"),
            };

            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(relationshipId))
                .Returns(definitionSpecificationRelationship);

            Action action = () =>
            {
                service.ProcessDataset(message).Wait();
            };

            // Assert
            action
                .ShouldThrow<AggregateException>()
                .WithInnerException<Exception>()
                .WithMessage("One or more errors occurred. (Failed to load table result)");

            logger
                .Received(1)
                .Error(Arg.Is($"Failed to load table result"));

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Any<string>());
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadAndTableResultsButNoDatasetRelationshipSummaries_DoesNotProcess()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            string dataset_cache_key = $"ds-table-rows:{DatasetService.GetBlobNameCacheKey(blobPath)}:{DataDefintionId}";

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult()
            };

            DatasetVersion datasetVersion = new DatasetVersion { BlobName = blobPath, Version = 1, };

            Dataset dataset = new Dataset
            {
                Definition = new Reference { Id = DataDefintionId },
                Current = datasetVersion,
                History = new List<DatasetVersion>()
                {
                    datasetVersion,
                }
            };

            var json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
                .UserProperties
                .Add("specification-id", SpecificationId);

            message
               .UserProperties
               .Add("relationship-id", relationshipId);

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                new DatasetDefinition{ Id = DataDefintionId }
            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
                .Returns(datasetDefinitions);

            ILogger logger = CreateLogger();

            IBlobClient blobClient = CreateBlobClient();

            blobClient
                .GetBlobReferenceFromServerAsync(blobPath)
                .Returns(Substitute.For<ICloudBlob>());

            Stream mockedExcelStream = Substitute.For<Stream>();
            mockedExcelStream
                .Length
                .Returns(1);

            blobClient
                .DownloadToStreamAsync(Arg.Any<ICloudBlob>())
                .Returns(mockedExcelStream);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<TableLoadResult[]>(Arg.Is(dataset_cache_key))
                .Returns(tableLoadResults.ToArraySafe());

            BuildProject buildProject = new BuildProject { Id = BuildProjectId };

            ICalcsRepository calcsRepository = CreateCalcsRepository();
            calcsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns(buildProject);

            IExcelDatasetReader excelDatasetReader = CreateExcelDatasetReader();
            excelDatasetReader
                .Read(Arg.Any<Stream>(), Arg.Any<DatasetDefinition>())
                .Returns(tableLoadResults.ToArraySafe());

            DatasetService service = CreateDatasetService(
                datasetRepository: datasetRepository,
                logger: logger,
                calcsRepository: calcsRepository,
                blobClient: blobClient,
                cacheProvider: cacheProvider,
                excelDatasetReader: excelDatasetReader);

            DefinitionSpecificationRelationship definitionSpecificationRelationship = new DefinitionSpecificationRelationship()
            {
                DatasetVersion = new DatasetRelationshipVersion()
                {
                    Version = 1,
                },
                DatasetDefinition = new Reference(datasetDefinitions.First().Id, "Name"),
            };

            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(relationshipId))
                .Returns(definitionSpecificationRelationship);

            // Act
            await service.ProcessDataset(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is($"No dataset relationships found for build project with id : '{BuildProjectId}' for specification '{SpecificationId}'"));
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadAndTableResultsButNoDatasetRelationshipSummaryCouldBeFound_DoesNotProcess()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            string dataset_cache_key = $"ds-table-rows:{DatasetService.GetBlobNameCacheKey(blobPath)}:{DataDefintionId}";

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult()
            };

            DatasetVersion datasetVersion = new DatasetVersion { BlobName = blobPath, Version = 1, };

            Dataset dataset = new Dataset
            {
                Definition = new Reference { Id = DataDefintionId },
                Current = datasetVersion,
                History = new List<DatasetVersion>()
                {
                    datasetVersion,
                }
            };

            var json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
                .UserProperties
                .Add("specification-id", SpecificationId);

            message
               .UserProperties
               .Add("relationship-id", relationshipId);

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                new DatasetDefinition{ Id = DataDefintionId }
            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
                .Returns(datasetDefinitions);

            ILogger logger = CreateLogger();

            IBlobClient blobClient = CreateBlobClient();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<TableLoadResult[]>(Arg.Is(dataset_cache_key))
                .Returns(tableLoadResults.ToArraySafe());

            BuildProject buildProject = new BuildProject
            {
                Id = BuildProjectId,
                DatasetRelationships = new List<DatasetRelationshipSummary>()
            };

            ICalcsRepository calcsRepository = CreateCalcsRepository();
            calcsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns(buildProject);

            blobClient
                .GetBlobReferenceFromServerAsync(blobPath)
                .Returns(Substitute.For<ICloudBlob>());

            Stream mockedExcelStream = Substitute.For<Stream>();
            mockedExcelStream
                .Length
                .Returns(1);

            blobClient
                .DownloadToStreamAsync(Arg.Any<ICloudBlob>())
                .Returns(mockedExcelStream);

            IExcelDatasetReader excelDatasetReader = CreateExcelDatasetReader();
            excelDatasetReader
                .Read(Arg.Any<Stream>(), Arg.Any<DatasetDefinition>())
                .Returns(tableLoadResults.ToArraySafe());

            DatasetService service = CreateDatasetService(
                datasetRepository: datasetRepository, logger: logger,
                calcsRepository: calcsRepository, blobClient: blobClient, cacheProvider: cacheProvider,
                excelDatasetReader: excelDatasetReader);

            DefinitionSpecificationRelationship definitionSpecificationRelationship = new DefinitionSpecificationRelationship()
            {
                DatasetVersion = new DatasetRelationshipVersion()
                {
                    Version = 1,
                },
                DatasetDefinition = new Reference(datasetDefinitions.First().Id, "Name"),
            };

            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(relationshipId))
                .Returns(definitionSpecificationRelationship);

            // Act
            await service.ProcessDataset(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is($"No dataset relationship found for build project with id : {buildProject.Id} with data definition id {DataDefintionId} and relationshipId '{relationshipId}'"));
        }

        [TestMethod]
        async public Task ProcessDataset_GivenPayloadAndTableResultsButNoRowsFoundToProcess_DoesNotSaveResults()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            string dataset_cache_key = $"ds-table-rows-{blobPath}-{DataDefintionId}";

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult
                {
                    Rows = new List<RowLoadResult>()
                }
            };

            DatasetVersion datasetVersion = new DatasetVersion { BlobName = blobPath, Version = 1, };

            Dataset dataset = new Dataset
            {
                Definition = new Reference { Id = DataDefintionId },
                Current = datasetVersion,
                History = new List<DatasetVersion>()
                {
                    datasetVersion,
                }
            };

            var json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
                .UserProperties
                .Add("specification-id", SpecificationId);

            message
               .UserProperties
               .Add("relationship-id", relationshipId);

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                new DatasetDefinition{ Id = DataDefintionId }
            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
                .Returns(datasetDefinitions);

            ILogger logger = CreateLogger();

            IBlobClient blobClient = CreateBlobClient();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<TableLoadResult[]>(Arg.Is(dataset_cache_key))
                .Returns(tableLoadResults.ToArraySafe());

            BuildProject buildProject = new BuildProject
            {
                Id = BuildProjectId,
                DatasetRelationships = new List<DatasetRelationshipSummary>
                {
                    new DatasetRelationshipSummary{ DatasetDefinition = new DatasetDefinition { Id = DataDefintionId } }
                },
                SpecificationId = SpecificationId,
            };

            ICalcsRepository calcsRepository = CreateCalcsRepository();
            calcsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns(buildProject);

            IProvidersResultsRepository resultsRepository = CreateProviderResultsRepository();

            DefinitionSpecificationRelationship definitionSpecificationRelationship = new DefinitionSpecificationRelationship()
            {
                DatasetVersion = new DatasetRelationshipVersion()
                {
                    Version = 1,
                },
                DatasetDefinition = new Reference(datasetDefinitions.First().Id, "Name"),
            };

            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(relationshipId))
                .Returns(definitionSpecificationRelationship);

            DatasetService service = CreateDatasetService(
                datasetRepository: datasetRepository, logger: logger,
                calcsRepository: calcsRepository, blobClient: blobClient, cacheProvider: cacheProvider,
                providerResultsRepository: resultsRepository);

            // Act
            Action action = () =>
            {
                service.ProcessDataset(message).Wait();
            };

            // Assert
            action
                .ShouldThrow<AggregateException>()
                .WithInnerException<Exception>()
                .WithMessage("One or more errors occurred. (Invalid blob returned: dataset-id/v1/ds.xlsx)");

            // Assert
            await
                resultsRepository
                    .DidNotReceive()
                    .UpdateCurrentProviderSourceDatasets(Arg.Any<IEnumerable<ProviderSourceDatasetCurrent>>());

            await
                resultsRepository
                    .DidNotReceive()
                    .UpdateProviderSourceDatasetHistory(Arg.Any<IEnumerable<ProviderSourceDatasetHistory>>());
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadAndTableResultsButNoIdentifiersFound_DoesNotSaveResults()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            string dataset_cache_key = $"ds-table-rows-{blobPath}-{DataDefintionId}";

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult
                {
                    Rows = new List<RowLoadResult>
                    {
                        new RowLoadResult { Fields = new Dictionary<string, object>{ { "UPIN", "123456" } } }
                    }
                }
            };

            DatasetVersion datasetVersion = new DatasetVersion { BlobName = blobPath, Version = 1, };

            Dataset dataset = new Dataset
            {
                Definition = new Reference { Id = DataDefintionId },
                Current = datasetVersion,
                History = new List<DatasetVersion>()
                {
                    datasetVersion,
                }
            };

            var json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
                .UserProperties
                .Add("specification-id", SpecificationId);

            message
               .UserProperties
               .Add("relationship-id", relationshipId);

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                new DatasetDefinition{ Id = DataDefintionId }
            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
                .Returns(datasetDefinitions);

            ILogger logger = CreateLogger();

            IBlobClient blobClient = CreateBlobClient();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<TableLoadResult[]>(Arg.Is(dataset_cache_key))
                .Returns(tableLoadResults.ToArraySafe());

            BuildProject buildProject = new BuildProject
            {
                Id = BuildProjectId,
                DatasetRelationships = new List<DatasetRelationshipSummary>
                {
                    new DatasetRelationshipSummary{ DatasetDefinition = new DatasetDefinition { Id = DataDefintionId } }
                }
            };

            ICalcsRepository calcsRepository = CreateCalcsRepository();
            calcsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns(buildProject);

            IProvidersResultsRepository resultsRepository = CreateProviderResultsRepository();

            DefinitionSpecificationRelationship definitionSpecificationRelationship = new DefinitionSpecificationRelationship()
            {
                DatasetVersion = new DatasetRelationshipVersion()
                {
                    Version = 1,
                },
                DatasetDefinition = new Reference(datasetDefinitions.First().Id, "Name"),
            };

            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(relationshipId))
                .Returns(definitionSpecificationRelationship);

            DatasetService service = CreateDatasetService(
                datasetRepository: datasetRepository, logger: logger,
                calcsRepository: calcsRepository, blobClient: blobClient, cacheProvider: cacheProvider,
                providerResultsRepository: resultsRepository);

            // Act
            Action action = () =>
            {
                service.ProcessDataset(message).Wait();
            };

            // Assert
            action
                .ShouldThrow<AggregateException>()
                .WithInnerException<Exception>()
                .WithMessage("One or more errors occurred. (Invalid blob returned: dataset-id/v1/ds.xlsx)");


            await
                resultsRepository
                    .DidNotReceive()
                    .UpdateCurrentProviderSourceDatasets(Arg.Any<IEnumerable<ProviderSourceDatasetCurrent>>());

            await
                resultsRepository
                    .DidNotReceive()
                    .UpdateProviderSourceDatasetHistory(Arg.Any<IEnumerable<ProviderSourceDatasetHistory>>());
        }

        [TestMethod]
        async public Task ProcessDataset_GivenPayloadAndTableResultsButNoProviderIds_DoesNotSaveResults()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            string dataset_cache_key = $"ds-table-rows-{blobPath}-{DataDefintionId}";

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult
                {
                    Rows = new List<RowLoadResult>
                    {
                        new RowLoadResult { IdentifierFieldType = IdentifierFieldType.UPIN, Fields = new Dictionary<string, object>{ { "UPIN", "123456" } } }
                    }
                }
            };

            DatasetVersion datasetVersion = new DatasetVersion { BlobName = blobPath, Version = 1, };

            Dataset dataset = new Dataset
            {
                Definition = new Reference { Id = DataDefintionId },
                Current = datasetVersion,
                History = new List<DatasetVersion>()
                {
                    datasetVersion,
                }
            };

            var json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
                .UserProperties
                .Add("specification-id", SpecificationId);

            message
               .UserProperties
               .Add("relationship-id", relationshipId);

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                new DatasetDefinition
                {
                    Id = DataDefintionId,
                    TableDefinitions = new List<TableDefinition>
                    {
                        new TableDefinition
                        {
                            FieldDefinitions = new List<FieldDefinition>
                            {
                                new FieldDefinition
                                {
                                    IdentifierFieldType = IdentifierFieldType.UPIN,
                                    Name = "UPIN",
                                }
                            }
                        }
                    }
                }
            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
                .Returns(datasetDefinitions);

            ILogger logger = CreateLogger();

            IBlobClient blobClient = CreateBlobClient();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<TableLoadResult[]>(Arg.Is(dataset_cache_key))
                .Returns(tableLoadResults.ToArraySafe());

            BuildProject buildProject = new BuildProject
            {
                Id = BuildProjectId,
                DatasetRelationships = new List<DatasetRelationshipSummary>
                {
                    new DatasetRelationshipSummary{ DatasetDefinition = new DatasetDefinition { Id = DataDefintionId } }
                },
                SpecificationId = SpecificationId,
            };

            ICalcsRepository calcsRepository = CreateCalcsRepository();
            calcsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns(buildProject);

            IProvidersResultsRepository resultsRepository = CreateProviderResultsRepository();

            DefinitionSpecificationRelationship definitionSpecificationRelationship = new DefinitionSpecificationRelationship()
            {
                DatasetVersion = new DatasetRelationshipVersion()
                {
                    Version = 1,
                },
                DatasetDefinition = new Reference(datasetDefinitions.First().Id, "Name"),
            };

            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(relationshipId))
                .Returns(definitionSpecificationRelationship);

            DatasetService service = CreateDatasetService(
                datasetRepository: datasetRepository, logger: logger,
                calcsRepository: calcsRepository, blobClient: blobClient, cacheProvider: cacheProvider,
                providerResultsRepository: resultsRepository);

            // Act
            Action action = () =>
            {
                service.ProcessDataset(message).Wait();
            };

            // Assert
            action
                .ShouldThrow<AggregateException>()
                .WithInnerException<Exception>()
                .WithMessage("One or more errors occurred. (Invalid blob returned: dataset-id/v1/ds.xlsx)");

            await
                resultsRepository
                    .DidNotReceive()
                    .UpdateCurrentProviderSourceDatasets(Arg.Any<IEnumerable<ProviderSourceDatasetCurrent>>());

            await
                resultsRepository
                    .DidNotReceive()
                    .UpdateProviderSourceDatasetHistory(Arg.Any<IEnumerable<ProviderSourceDatasetHistory>>());
        }

        [TestMethod]
        async public Task ProcessDataset_GivenPayloadAndTableResultsWithProviderIds_SavesDataset()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            string dataset_cache_key = $"ds-table-rows:{blobPath}:{DataDefintionId}";

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult
                {
                    Rows = new List<RowLoadResult>
                    {
                        new RowLoadResult { Identifier = "123456", IdentifierFieldType = IdentifierFieldType.UPIN, Fields = new Dictionary<string, object>{ { "UPIN", "123456" } } }
                    }
                }
            };

            DatasetVersion datasetVersion = new DatasetVersion { BlobName = blobPath, Version = 1, };

            Dataset dataset = new Dataset
            {
                Definition = new Reference { Id = DataDefintionId },
                Current = datasetVersion,
                History = new List<DatasetVersion>()
                {
                    datasetVersion,
                }
            };

            var json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";
            string relationshipName = "Relationship Name";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
                .UserProperties
                .Add("specification-id", SpecificationId);

            message
               .UserProperties
               .Add("relationship-id", relationshipId);

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                new DatasetDefinition
                {
                    Id = DataDefintionId,
                    TableDefinitions = new List<TableDefinition>
                    {
                        new TableDefinition
                        {
                            FieldDefinitions = new List<FieldDefinition>
                            {
                                new FieldDefinition
                                {
                                    IdentifierFieldType = IdentifierFieldType.UPIN,
                                    Name = "UPIN",
                                }
                            }
                        }
                    }
                }
            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
                .Returns(datasetDefinitions);

            ILogger logger = CreateLogger();

            IBlobClient blobClient = CreateBlobClient();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<TableLoadResult[]>(Arg.Is(dataset_cache_key))
                .Returns(tableLoadResults.ToArraySafe());

            BuildProject buildProject = new BuildProject
            {
                Id = BuildProjectId,
                DatasetRelationships = new List<DatasetRelationshipSummary>
                {
                    new DatasetRelationshipSummary{
                        DatasetDefinition = new DatasetDefinition { Id = DataDefintionId },
                        Relationship = new Reference(relationshipId, relationshipName),
                    }
                },
                SpecificationId = SpecificationId,
            };

            ICalcsRepository calcsRepository = CreateCalcsRepository();
            calcsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns(buildProject);

            IEnumerable<ProviderSummary> summaries = new[]
            {
                new ProviderSummary { Id = "123",  UPIN = "123456" },
            };

            IProviderRepository resultsRepository = CreateProviderRepository();
            resultsRepository
                .GetAllProviderSummaries()
                .Returns(summaries);

            IProvidersResultsRepository providerResultsRepository = CreateProviderResultsRepository();

            DefinitionSpecificationRelationship definitionSpecificationRelationship = new DefinitionSpecificationRelationship()
            {
                DatasetVersion = new DatasetRelationshipVersion()
                {
                    Version = 1,
                },
                DatasetDefinition = new Reference(datasetDefinitions.First().Id, "Name"),
            };

            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(relationshipId))
                .Returns(definitionSpecificationRelationship);

            blobClient
                .GetBlobReferenceFromServerAsync(blobPath)
                .Returns(Substitute.For<ICloudBlob>());

            Stream mockedExcelStream = Substitute.For<Stream>();
            mockedExcelStream
                .Length
                .Returns(1);

            blobClient
                .DownloadToStreamAsync(Arg.Any<ICloudBlob>())
                .Returns(mockedExcelStream);

            IExcelDatasetReader excelDatasetReader = CreateExcelDatasetReader();
            excelDatasetReader
                .Read(Arg.Any<Stream>(), Arg.Any<DatasetDefinition>())
                .Returns(tableLoadResults.ToArraySafe());

            DatasetService service = CreateDatasetService(
                datasetRepository: datasetRepository,
                logger: logger,
                calcsRepository: calcsRepository,
                blobClient: blobClient,
                cacheProvider: cacheProvider,
                providerRepository: resultsRepository,
                providerResultsRepository: providerResultsRepository,
                excelDatasetReader: excelDatasetReader);

            // Act
            await service.ProcessDataset(message);

            // Assert
            await
                providerResultsRepository
                    .Received(1)
                    .UpdateCurrentProviderSourceDatasets(Arg.Is<IEnumerable<ProviderSourceDatasetCurrent>>(
                        m => m.First().DataDefinition.Id == DataDefintionId &&
                             m.First().DataGranularity == DataGranularity.SingleRowPerProvider &&
                             m.First().DefinesScope == false &&
                             !string.IsNullOrWhiteSpace(m.First().Id) &&
                             m.First().SpecificationId == SpecificationId &&
                             m.First().ProviderId == "123"
                        ));

            await
                providerResultsRepository
                    .Received(1)
                    .UpdateProviderSourceDatasetHistory(Arg.Is<IEnumerable<ProviderSourceDatasetHistory>>(
                        m => m.First().DataDefinition.Id == DataDefintionId &&
                             m.First().DataGranularity == DataGranularity.SingleRowPerProvider &&
                             m.First().DefinesScope == false &&
                             !string.IsNullOrWhiteSpace(m.First().Id) &&
                             m.First().SpecificationId == SpecificationId &&
                             m.First().ProviderId == "123"
                        ));
        }

        [TestMethod]
        async public Task ProcessDataset_GivenPayloadAndTableResultsWithMultipleProviderIds_DoesNotSaveDataset()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            string dataset_cache_key = $"ds-table-rows:{DatasetService.GetBlobNameCacheKey(blobPath)}:{DataDefintionId}";

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult
                {
                    Rows = new List<RowLoadResult>
                    {
                        new RowLoadResult { Identifier = "123456", IdentifierFieldType = IdentifierFieldType.UPIN, Fields = new Dictionary<string, object>{ { "UPIN", "123456" } } },
                        new RowLoadResult { Identifier = "222333", IdentifierFieldType = IdentifierFieldType.UPIN, Fields = new Dictionary<string, object>{ { "UPIN", "222333" } } }
                    }
                }
            };

            DatasetVersion datasetVersion = new DatasetVersion { BlobName = blobPath, Version = 1, };

            Dataset dataset = new Dataset
            {
                Definition = new Reference { Id = DataDefintionId },
                Current = datasetVersion,
                History = new List<DatasetVersion>()
                {
                    datasetVersion,
                }
            };

            var json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";
            string relationshipName = "Relationship Name";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
                .UserProperties
                .Add("specification-id", SpecificationId);

            message
               .UserProperties
               .Add("relationship-id", relationshipId);

            IEnumerable<DatasetDefinition> datasetDefinitions = new[]
            {
                new DatasetDefinition
                {
                    Id = DataDefintionId,
                    TableDefinitions = new List<TableDefinition>
                    {
                        new TableDefinition
                        {
                            FieldDefinitions = new List<FieldDefinition>
                            {
                                new FieldDefinition
                                {
                                    IdentifierFieldType = IdentifierFieldType.UPIN,
                                    Name = "UPIN",
                                }
                            }
                        }
                    }
                }
            };

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
                .Returns(datasetDefinitions);

            ILogger logger = CreateLogger();

            IBlobClient blobClient = CreateBlobClient();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<TableLoadResult[]>(Arg.Is(dataset_cache_key))
                .Returns(tableLoadResults.ToArraySafe());

            BuildProject buildProject = new BuildProject
            {
                Id = BuildProjectId,
                DatasetRelationships = new List<DatasetRelationshipSummary>
                {
                    new DatasetRelationshipSummary{
                        DatasetDefinition = new DatasetDefinition { Id = DataDefintionId },
                        Relationship = new Reference(relationshipId, relationshipName),
                    }
                },
                SpecificationId = SpecificationId,
            };

            ICalcsRepository calcsRepository = CreateCalcsRepository();
            calcsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns(buildProject);

            IEnumerable<ProviderSummary> summaries = new[]
            {
                new ProviderSummary { Id = "123",  UPIN = "123456" },
                new ProviderSummary { Id = "456", UPIN = "222333" },
            };

            IProviderRepository resultsRepository = CreateProviderRepository();
            resultsRepository
                .GetAllProviderSummaries()
                .Returns(summaries);

            IProvidersResultsRepository providerResultsRepository = CreateProviderResultsRepository();

            DefinitionSpecificationRelationship definitionSpecificationRelationship = new DefinitionSpecificationRelationship()
            {
                DatasetVersion = new DatasetRelationshipVersion()
                {
                    Version = 1,
                },
                DatasetDefinition = new Reference(datasetDefinitions.First().Id, "Name"),
            };

            datasetRepository
                .GetDefinitionSpecificationRelationshipById(Arg.Is(relationshipId))
                .Returns(definitionSpecificationRelationship);

            blobClient
                .GetBlobReferenceFromServerAsync(blobPath)
                .Returns(Substitute.For<ICloudBlob>());

            Stream mockedExcelStream = Substitute.For<Stream>();
            mockedExcelStream
                .Length
                .Returns(1);

            blobClient
                .DownloadToStreamAsync(Arg.Any<ICloudBlob>())
                .Returns(mockedExcelStream);

            IExcelDatasetReader excelDatasetReader = CreateExcelDatasetReader();
            excelDatasetReader
                .Read(Arg.Any<Stream>(), Arg.Any<DatasetDefinition>())
                .Returns(tableLoadResults.ToArraySafe());

            DatasetService service = CreateDatasetService(
                datasetRepository: datasetRepository,
                logger: logger,
                calcsRepository: calcsRepository,
                blobClient: blobClient,
                cacheProvider: cacheProvider,
                providerRepository: resultsRepository,
                providerResultsRepository: providerResultsRepository,
                excelDatasetReader: excelDatasetReader);

            // Act
            await service.ProcessDataset(message);

            // Assert
            await
                providerResultsRepository
                    .Received(1)
                    .UpdateCurrentProviderSourceDatasets(Arg.Any<IEnumerable<ProviderSourceDatasetCurrent>>());

            await
                providerResultsRepository
                    .Received(1)
                    .UpdateProviderSourceDatasetHistory(Arg.Any<IEnumerable<ProviderSourceDatasetHistory>>());
        }

        [TestMethod]
        public async Task DownloadDatasetFile_GivenNoDatasetIdProvided_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            DatasetService service = CreateDatasetService(logger: logger);

            // Act
            IActionResult result = await service.DownloadDatasetFile(request);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No currentDatasetId was provided to DownloadDatasetFile"));
        }

        [TestMethod]
        public async Task DownloadDatasetFile_GivenDatasetCouldNotBeFound_ReturnsPreConditionFailed()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "datasetId", new StringValues(DatasetId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            DatasetService service = CreateDatasetService(logger: logger);

            // Act
            IActionResult result = await service.DownloadDatasetFile(request);

            // Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(412);

            logger
                .Received(1)
                .Error($"A dataset could not be found for dataset id: {DatasetId}");
        }

        [TestMethod]
        public async Task DownloadDatasetFile_GivenDatasetCurrentBlobNameDoesnotExist_ReturnsPreConditionFailed()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "datasetId", new StringValues(DatasetId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            Dataset dataset = new Dataset();

            ILogger logger = CreateLogger();

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetByDatasetId(Arg.Is(DatasetId))
                .Returns(dataset);

            DatasetService service = CreateDatasetService(logger: logger, datasetRepository: datasetRepository);

            // Act
            IActionResult result = await service.DownloadDatasetFile(request);

            // Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(412);

            logger
                .Received(1)
                .Error($"A blob name could not be found for dataset id: {DatasetId}");
        }

        [TestMethod]
        public async Task DownloadDatasetFile_GivenBlobDoesNotExist_ReturnsNotFoundResult()
        {
            //Arrange
            const string blobName = "blob-name.xlsx";

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "datasetId", new StringValues(DatasetId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            Dataset dataset = new Dataset
            {
                Current = new DatasetVersion
                {
                    BlobName = blobName
                }
            };

            ILogger logger = CreateLogger();

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetByDatasetId(Arg.Is(DatasetId))
                .Returns(dataset);

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Is(blobName))
                .Returns((ICloudBlob)null);

            DatasetService service = CreateDatasetService(logger: logger, datasetRepository: datasetRepository, blobClient: blobClient);

            // Act
            IActionResult result = await service.DownloadDatasetFile(request);

            // Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();

            logger
                .Received(1)
                .Error($"Failed to find blob with path: {blobName}");
        }

        [TestMethod]
        public async Task DownloadDatasetFile_GivenBlobExists_ReturnsOKResult()
        {
            //Arrange
            const string blobUrl = "http://this-is-a-bloburl?this=is-a-token";
            const string blobName = "blob-name.xlsx";

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "datasetId", new StringValues(DatasetId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            Dataset dataset = new Dataset
            {
                Current = new DatasetVersion
                {
                    BlobName = blobName
                }
            };

            ILogger logger = CreateLogger();

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetByDatasetId(Arg.Is(DatasetId))
                .Returns(dataset);

            ICloudBlob cloudBlob = Substitute.For<ICloudBlob>();

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Is(blobName))
                .Returns(cloudBlob);

            blobClient
                    .GetBlobSasUrl(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<SharedAccessBlobPermissions>())
                    .Returns(blobUrl);

            DatasetService service = CreateDatasetService(logger: logger, datasetRepository: datasetRepository, blobClient: blobClient);

            // Act
            IActionResult result = await service.DownloadDatasetFile(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        public async Task DatasetVersionUpdate_WhenValidDatasetVersionUpdateRequested_ThenDatasetVersionAdded()
        {
            // Arrange
            const string authorId = "authId";
            const string authorName = "Change Author";
            const string datasetId = "ds1";
            const string expectedBloblUrl = "https://blob.com/result";

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            IBlobClient blobClient = CreateBlobClient();
            IMapper mapper = CreateMapperWithDatasetsConfiguration();

            DatasetService datasetService = CreateDatasetService(datasetRepository: datasetRepository,
                blobClient: blobClient,
                mapper: mapper);

            DatasetVersionUpdateModel model = new DatasetVersionUpdateModel
            {
                DatasetId = datasetId,
                Filename = "ds.xlsx",
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Sid, authorId),
                new Claim(ClaimTypes.Name, authorName)
            };

            request
                .HttpContext.User.Claims
                .Returns(claims.AsEnumerable());

            DatasetVersion existingDatasetVersion = new DatasetVersion()
            {
                Version = 1,
            };

            Dataset existingDataset = new Dataset()
            {
                Id = datasetId,
                Current = existingDatasetVersion,
                Definition = new Reference("defId", "Definition Name"),
                Description = "Description v1",
                History = new List<DatasetVersion>()
                {
                    existingDatasetVersion
                },
                Name = "Dataset Name",
                Published = null,
            };

            datasetRepository
                .GetDatasetByDatasetId(Arg.Is(datasetId))
                .Returns(existingDataset);

            blobClient
                .GetBlobSasUrl(Arg.Is($"{datasetId}/v2/{model.Filename}"), Arg.Any<DateTimeOffset>(), Arg.Any<SharedAccessBlobPermissions>())
                .Returns(expectedBloblUrl);


            // Act
            IActionResult result = await datasetService.DatasetVersionUpdate(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which.Value
                .ShouldBeEquivalentTo(new NewDatasetVersionResponseModel()
                {
                    DatasetId = datasetId,
                    Author = new Reference(authorId, authorName),
                    BlobUrl = expectedBloblUrl,
                    DefinitionId = existingDataset.Definition.Id,
                    Description = existingDataset.Description,
                    Filename = model.Filename,
                    Name = existingDataset.Name,
                    Version = 2,
                });
        }

        [TestMethod]
        public async Task DatasetVersionUpdate_WhenProvidedModelIsInvalid_ThenBadRequestReturned()
        {
            // Arrange
            IDatasetRepository datasetRepository = CreateDatasetsRepository();

            ValidationResult validationResult = new ValidationResult(
                new List<ValidationFailure>()
                    {
                         new ValidationFailure("datasetId", "error Message")
                    }
            );

            IValidator<DatasetVersionUpdateModel> validator = CreateDatasetVersionUpdateModelValidator(validationResult);

            DatasetService datasetService = CreateDatasetService(
                datasetRepository: datasetRepository,
                datasetVersionUpdateModelValidator: validator);

            DatasetVersionUpdateModel model = new DatasetVersionUpdateModel
            {
                DatasetId = "ds1",
                Filename = "ds.xlsx",
            };

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            // Act
            IActionResult result = await datasetService.DatasetVersionUpdate(request);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task DatasetVersionUpdate_WhenNullDatasetVersionUpdateModel_ThenBadRequestReturned()
        {
            // Arrange
            ILogger logger = CreateLogger();

            DatasetService datasetService = CreateDatasetService(logger: logger);

            DatasetVersionUpdateModel model = null;

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            // Act
            IActionResult result = await datasetService.DatasetVersionUpdate(request);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should().Be("Null model name was provided");

            logger
                .Received(1)
                .Warning(Arg.Is("Null model was provided to DatasetVersionUpdate"));
        }

        [TestMethod]
        public async Task DatasetVersionUpdate_WhenDatasetLookupReturnsNull_ThenPreconditionFailedReturned()
        {
            // Arrange
            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            ILogger logger = CreateLogger();

            DatasetService datasetService = CreateDatasetService(datasetRepository: datasetRepository, logger: logger);

            DatasetVersionUpdateModel model = new DatasetVersionUpdateModel
            {
                DatasetId = "dsnotfound",
                Filename = "ds.xlsx",
            };
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            datasetRepository
               .GetDatasetByDatasetId(Arg.Any<string>())
               .Returns((Dataset)null);

            // Act
            IActionResult result = await datasetService.DatasetVersionUpdate(request);

            // Assert
            result
                .Should()
                .BeOfType<PreconditionFailedResult>()
                .Which
                .Value
                .Should().Be($"Dataset was not found with ID {model.DatasetId} when trying to add new dataset version");

            logger
                .Received(1)
                .Warning(Arg.Is("Dataset was not found with ID {datasetId} when trying to add new dataset version"), Arg.Is(model.DatasetId));
        }

        [TestMethod]
        public async Task GetCurrentDatasetVersionByDatasetId_WhenDatasetFoundWithOnlyOneVersionThenCurrentDatasetVersionReturned()
        {
            // Arrange
            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            IMapper mapper = CreateMapperWithDatasetsConfiguration();

            DatasetService datasetService = CreateDatasetService(
                datasetRepository: datasetRepository,
                mapper: mapper
                );

            const string datasetId = "ds1";

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "datasetId", new StringValues(datasetId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            DatasetVersion datasetVersion = new DatasetVersion()
            {
                Version = 1,
                Author = new Reference("authorId", "Author Name"),
                BlobName = "file/name.xlsx",
                Commment = "My update comment",
                Date = new DateTime(2018, 12, 1, 3, 4, 5),
                PublishStatus = Models.Versioning.PublishStatus.Draft,
            };

            Dataset dataset = new Dataset()
            {
                Id = datasetId,
                Current = datasetVersion,
                History = new List<DatasetVersion>() { datasetVersion },
                Definition = new Reference("defId", "definitionName"),
                Description = "Description",
                Name = "Dataset Name",
                Published = null,
            };

            DocumentEntity<Dataset> documentEntity = new DocumentEntity<Dataset>(dataset)
            {
                UpdatedAt = new DateTime(2018, 12, 1, 3, 4, 5),
            };

            datasetRepository
                .GetDatasetDocumentByDatasetId(Arg.Is(datasetId))
                .Returns(documentEntity);

            // Act
            IActionResult result = await datasetService.GetCurrentDatasetVersionByDatasetId(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .ShouldBeEquivalentTo(new DatasetVersionResponseViewModel()
                {
                    Id = datasetId,
                    Name = dataset.Name,
                    Author = new Reference("authorId", "Author Name"),
                    BlobName = "file/name.xlsx",
                    Comment = "My update comment",
                    LastUpdatedDate = new DateTime(2018, 12, 1, 3, 4, 5),
                    PublishStatus = Models.Versioning.PublishStatus.Draft,
                    Definition = new Reference("defId", "definitionName"),
                    Description = "Description",
                    Version = 1,
                });
        }

        [TestMethod]
        public async Task GetCurrentDatasetVersionByDatasetId_WhenDatasetFoundWithMultipleVersionsThenCurrentDatasetVersionReturned()
        {
            // Arrange
            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            IMapper mapper = CreateMapperWithDatasetsConfiguration();

            DatasetService datasetService = CreateDatasetService(
                datasetRepository: datasetRepository,
                mapper: mapper
                );

            const string datasetId = "ds1";

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "datasetId", new StringValues(datasetId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            DatasetVersion datasetVersion1 = new DatasetVersion()
            {
                Version = 1,
                Author = new Reference("authorId", "Author Name"),
                BlobName = "file/name.xlsx",
                Commment = "My update comment",
                Date = new DateTime(2018, 12, 1, 3, 4, 5),
                PublishStatus = Models.Versioning.PublishStatus.Draft,
            };

            DatasetVersion datasetVersion2 = new DatasetVersion()
            {
                Version = 2,
                Author = new Reference("authorId2", "Author Name Two"),
                BlobName = "file/name2.xlsx",
                Commment = "My update comment for second",
                Date = new DateTime(2018, 12, 1, 3, 2, 2),
                PublishStatus = Models.Versioning.PublishStatus.Draft,
            };

            Dataset dataset = new Dataset()
            {
                Id = datasetId,
                Current = datasetVersion2,
                History = new List<DatasetVersion>() { datasetVersion1, datasetVersion2 },
                Definition = new Reference("defId", "definitionName"),
                Description = "Description",
                Name = "Dataset Name",
                Published = null,
            };

            DocumentEntity<Dataset> documentEntity = new DocumentEntity<Dataset>(dataset)
            {
                UpdatedAt = new DateTime(2018, 12, 1, 3, 2, 2),
            };

            datasetRepository
                .GetDatasetDocumentByDatasetId(Arg.Is(datasetId))
                .Returns(documentEntity);

            // Act
            IActionResult result = await datasetService.GetCurrentDatasetVersionByDatasetId(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .ShouldBeEquivalentTo(new DatasetVersionResponseViewModel()
                {
                    Id = datasetId,
                    Name = dataset.Name,
                    Author = new Reference("authorId2", "Author Name Two"),
                    BlobName = "file/name2.xlsx",
                    Comment = "My update comment for second",
                    LastUpdatedDate = new DateTime(2018, 12, 1, 3, 2, 2),
                    PublishStatus = Models.Versioning.PublishStatus.Draft,
                    Definition = new Reference("defId", "definitionName"),
                    Description = "Description",
                    Version = 2,
                });
        }

        [TestMethod]
        public async Task GetCurrentDatasetVersionByDatasetId_WhenNoDatasetIdIsProvided_ThenBadRequestReturned()
        {
            // Arrange
            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            IMapper mapper = CreateMapperWithDatasetsConfiguration();
            ILogger logger = CreateLogger();

            DatasetService datasetService = CreateDatasetService(
                datasetRepository: datasetRepository,
                mapper: mapper,
                logger: logger
                );

            const string datasetId = "";

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "datasetId", new StringValues(datasetId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            // Act
            IActionResult result = await datasetService.GetCurrentDatasetVersionByDatasetId(request);

            // Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should().Be("Null or empty datasetId provided");

            logger
                .Received(1)
                .Warning(Arg.Is("No datasetId was provided to GetCurrentDatasetVersionByDatasetId"));
        }

        [TestMethod]
        public async Task GetCurrentDatasetVersionByDatasetId_WhenNoDatasetIdIsNotFound_ThenNotFoundReturned()
        {
            // Arrange
            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            IMapper mapper = CreateMapperWithDatasetsConfiguration();
            ILogger logger = CreateLogger();

            DatasetService datasetService = CreateDatasetService(
                datasetRepository: datasetRepository,
                mapper: mapper,
                logger: logger
                );

            const string datasetId = "notfound";

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "datasetId", new StringValues(datasetId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            datasetRepository
                .GetDatasetDocumentByDatasetId(Arg.Is(datasetId))
                .Returns((DocumentEntity<Dataset>)null);

            // Act
            IActionResult result = await datasetService.GetCurrentDatasetVersionByDatasetId(request);

            // Assert
            result
                .Should()
                .BeOfType<NotFoundObjectResult>()
                .Which
                .Value
                .Should().Be("Unable to find dataset with ID: notfound");

            await datasetRepository
                .Received(1)
                .GetDatasetDocumentByDatasetId(Arg.Is(datasetId));
        }

        [TestMethod]
        public async Task GetCurrentDatasetVersionByDatasetId_WhenDatasetHasNullContent_ThenNotFoundReturned()
        {
            // Arrange
            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            IMapper mapper = CreateMapperWithDatasetsConfiguration();
            ILogger logger = CreateLogger();

            DatasetService datasetService = CreateDatasetService(
                datasetRepository: datasetRepository,
                mapper: mapper,
                logger: logger
                );

            const string datasetId = "notfound";

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "datasetId", new StringValues(datasetId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            datasetRepository
                .GetDatasetDocumentByDatasetId(Arg.Is(datasetId))
                .Returns(new DocumentEntity<Dataset>(null));

            // Act
            IActionResult result = await datasetService.GetCurrentDatasetVersionByDatasetId(request);

            // Assert
            result
                .Should()
                .BeOfType<NotFoundObjectResult>()
                .Which
                .Value
                .Should().Be("Unable to find dataset with ID: notfound. Content is null");

            await datasetRepository
                .Received(1)
                .GetDatasetDocumentByDatasetId(Arg.Is(datasetId));
        }

        [TestMethod]
        public async Task GetCurrentDatasetVersionByDatasetId_WhenDatasetHasNullCurrentObject_ThenNotFoundReturned()
        {
            // Arrange
            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            IMapper mapper = CreateMapperWithDatasetsConfiguration();
            ILogger logger = CreateLogger();

            DatasetService datasetService = CreateDatasetService(
                datasetRepository: datasetRepository,
                mapper: mapper,
                logger: logger
                );

            const string datasetId = "notfound";

            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "datasetId", new StringValues(datasetId) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            datasetRepository
                .GetDatasetDocumentByDatasetId(Arg.Is(datasetId))
                .Returns(new DocumentEntity<Dataset>(new Dataset()));

            // Act
            IActionResult result = await datasetService.GetCurrentDatasetVersionByDatasetId(request);

            // Assert
            result
                .Should()
                .BeOfType<NotFoundObjectResult>()
                .Which
                .Value
                .Should().Be("Unable to find dataset with ID: notfound. Current version is null");

            await datasetRepository
                .Received(1)
                .GetDatasetDocumentByDatasetId(Arg.Is(datasetId));
        }

        static DatasetService CreateDatasetService(
            IBlobClient blobClient = null,
            ILogger logger = null,
            IDatasetRepository datasetRepository = null,
            IValidator<CreateNewDatasetModel> createNewDatasetModelValidator = null,
            IValidator<DatasetVersionUpdateModel> datasetVersionUpdateModelValidator = null,
            IMapper mapper = null,
            IValidator<DatasetMetadataModel> datasetMetadataModelValidator = null,
            ISearchRepository<DatasetIndex> searchRepository = null,
            IValidator<GetDatasetBlobModel> getDatasetBlobModelValidator = null,
            ISpecificationsRepository specificationsRepository = null,
            IMessengerService messengerService = null,
            IExcelDatasetReader excelDatasetReader = null,
            ICacheProvider cacheProvider = null,
            ICalcsRepository calcsRepository = null,
            IProviderRepository providerRepository = null,
            IProvidersResultsRepository providerResultsRepository = null,
            ITelemetry telemetry = null,
            IDatasetsResiliencePolicies datasetsResiliencePolicies = null,
            IValidator<ExcelPackage> datasetWorksheetValidator = null,
            IValidator<DatasetUploadValidationModel> datasetUploadValidator = null)
        {

            return new DatasetService(
                blobClient ?? CreateBlobClient(),
                logger ?? CreateLogger(),
                datasetRepository ?? CreateDatasetsRepository(),
                createNewDatasetModelValidator ?? CreateNewDatasetModelValidator(),
                datasetVersionUpdateModelValidator ?? CreateDatasetVersionUpdateModelValidator(),
                mapper ?? CreateMapper(),
                datasetMetadataModelValidator ?? CreateDatasetMetadataModelValidator(),
                searchRepository ?? CreateSearchRepository(),
                getDatasetBlobModelValidator ?? CreateGetDatasetBlobModelValidator(),
                specificationsRepository ?? CreateSpecificationsRepository(),
                messengerService ?? CreateMessengerService(),
                excelDatasetReader ?? CreateExcelDatasetReader(),
                cacheProvider ?? CreateCacheProvider(), calcsRepository ?? CreateCalcsRepository(),
                providerRepository ?? CreateProviderRepository(),
                providerResultsRepository ?? CreateProviderResultsRepository(),
                telemetry ?? CreateTelemetry(),
                datasetsResiliencePolicies ?? DatasetsResilienceTestHelper.GenerateTestPolicies(),
                datasetWorksheetValidator ?? CreateDataWorksheetValidator(),
                datasetUploadValidator ?? CreateDatasetUploadValidator());
        }

        static ICalcsRepository CreateCalcsRepository()
        {
            return Substitute.For<ICalcsRepository>();
        }

        static ITelemetry CreateTelemetry()
        {
            return Substitute.For<ITelemetry>();
        }

        static IProviderRepository CreateProviderRepository()
        {
            return Substitute.For<IProviderRepository>();
        }

        static IProvidersResultsRepository CreateProviderResultsRepository()
        {
            return Substitute.For<IProvidersResultsRepository>();
        }

        static IExcelDatasetReader CreateExcelDatasetReader()
        {
            return Substitute.For<IExcelDatasetReader>();
        }

        static ISearchRepository<DatasetIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<DatasetIndex>>();
        }

        static ISpecificationsRepository CreateSpecificationsRepository()
        {
            return Substitute.For<ISpecificationsRepository>();
        }

        static IMessengerService CreateMessengerService()
        {
            return Substitute.For<IMessengerService>();
        }

        static ServiceBusSettings CreateEventHubSettings()
        {
            return new ServiceBusSettings();
        }

        static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        static IValidator<DatasetUploadValidationModel> CreateDatasetUploadValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<DatasetUploadValidationModel> validator = Substitute.For<IValidator<DatasetUploadValidationModel>>();

            validator
                .Validate(Arg.Any<DatasetUploadValidationModel>())
                .Returns(validationResult);

            return validator;
        }

        static IValidator<CreateNewDatasetModel> CreateNewDatasetModelValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<CreateNewDatasetModel> validator = Substitute.For<IValidator<CreateNewDatasetModel>>();

            validator
               .ValidateAsync(Arg.Any<CreateNewDatasetModel>())
               .Returns(validationResult);

            return validator;
        }

        static IValidator<DatasetVersionUpdateModel> CreateDatasetVersionUpdateModelValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<DatasetVersionUpdateModel> validator = Substitute.For<IValidator<DatasetVersionUpdateModel>>();

            validator
               .ValidateAsync(Arg.Any<DatasetVersionUpdateModel>())
               .Returns(validationResult);

            return validator;
        }

        static IValidator<DatasetMetadataModel> CreateDatasetMetadataModelValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<DatasetMetadataModel> validator = Substitute.For<IValidator<DatasetMetadataModel>>();

            validator
               .ValidateAsync(Arg.Any<DatasetMetadataModel>())
               .Returns(validationResult);

            return validator;
        }

        static IValidator<GetDatasetBlobModel> CreateGetDatasetBlobModelValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<GetDatasetBlobModel> validator = Substitute.For<IValidator<GetDatasetBlobModel>>();

            validator
               .ValidateAsync(Arg.Any<GetDatasetBlobModel>())
               .Returns(validationResult);

            return validator;
        }

        static IValidator<ExcelPackage> CreateDataWorksheetValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<ExcelPackage> validator = Substitute.For<IValidator<ExcelPackage>>();

            validator
               .ValidateAsync(Arg.Any<ExcelPackage>())
               .Returns(validationResult);

            return validator;
        }

        static IBlobClient CreateBlobClient()
        {
            return Substitute.For<IBlobClient>();
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static IMapper CreateMapper()
        {
            return Substitute.For<IMapper>();
        }

        static IMapper CreateMapperWithDatasetsConfiguration()
        {
            MapperConfiguration config = new MapperConfiguration(c =>
            {
                c.AddProfile<DatasetsMappingProfile>();
            });

            return new Mapper(config);
        }

        static IDatasetRepository CreateDatasetsRepository()
        {
            return Substitute.For<IDatasetRepository>();
        }

        static byte[] CreateTestExcelPackage()
        {
            using (ExcelPackage package = new ExcelPackage())
            {
                ExcelWorksheet workSheet = package.Workbook.Worksheets.Add("Test Worksheet");

                workSheet.Cells["A1"].Value = "1";
                workSheet.Cells["B1"].Value = "2";
                workSheet.Cells["C1"].Value = "3";

                return package.GetAsByteArray();
            }
        }
    }
}
