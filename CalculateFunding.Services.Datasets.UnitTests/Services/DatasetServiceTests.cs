using AutoMapper;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Interfaces.Caching;
using Newtonsoft.Json.Linq;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Interfaces.Logging;
using Microsoft.Azure.ServiceBus;

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

        [TestMethod]
        public async Task GetDatasetByName_GivenDatasetNameDoesNotExist_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            DatasetService service = CreateDatasetService(logger: logger);

            //Act
            IActionResult result = await service.GetDatasetByName(request);

            //Assert
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

            //Act
            IActionResult result = await service.GetDatasetByName(request);

            //Assert
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

            //Act
            IActionResult result = await service.GetDatasetByName(request);

            //Assert
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

            //Act
            IActionResult result = await service.CreateNewDataset(request);

            //Assert
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

            //Act
            IActionResult result = await service.CreateNewDataset(request);

            //Assert
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
            CreateNewDatasetResponseModel responseModel = new CreateNewDatasetResponseModel
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
                .Map<CreateNewDatasetResponseModel>(Arg.Any<CreateNewDatasetModel>())
                .Returns(responseModel);

            DatasetService service = CreateDatasetService(logger: logger, blobClient: blobClient, mapper: mapper);

            //Act
            IActionResult result = await service.CreateNewDataset(request);

            //Assert
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

            //Act
            IActionResult result = await service.ValidateDataset(request);

            //Assert
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

            //Act
            IActionResult result = await service.ValidateDataset(request);

            //Assert
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

            //Act
            IActionResult result = await service.ValidateDataset(request);

            //Assert
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

            IDictionary<string, string> metaData = new Dictionary<string, string>();
            metaData.Add("dataDefinitionId", DataDefintionId);

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

            //Act
            IActionResult result = await service.ValidateDataset(request);

            //Assert
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

            IDictionary<string, string> metaData = new Dictionary<string, string>();
            metaData.Add("dataDefinitionId", DataDefintionId);

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);

            IBlobClient blobClient = CreateBlobClient();
            blobClient
                .GetBlobReferenceFromServerAsync(Arg.Is(blobPath))
                .Returns(blob);

            IEnumerable<DatasetDefinition> datasetDefinitions = Enumerable.Empty<DatasetDefinition>();

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
                .Returns(datasetDefinitions);

            DatasetService service = CreateDatasetService(logger: logger, blobClient: blobClient, datasetRepository: datasetRepository);

            //Act
            IActionResult result = await service.ValidateDataset(request);

            //Assert
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
                .Error(Arg.Is($"Unable to find a data definition for id: {DataDefintionId}, for blob: {blobPath}"));
        }


        [TestMethod]
        public async Task ValidateDataset_GivenTableResultsContainsOneError_ReturnsOKResultWithMessage()
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

            IDictionary<string, string> metaData = new Dictionary<string, string>();
            metaData.Add("dataDefinitionId", DataDefintionId);

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);

            MemoryStream memoryStream = new MemoryStream(new byte[100]);

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

            List<DatasetValidationError> errors = new List<DatasetValidationError>();
            errors.Add(new DatasetValidationError { ErrorMessage = "error" });

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult{ GlobalErrors = errors }
            };

            IExcelDatasetReader datasetReader = CreateExcelDatasetReader();
            datasetReader
                .Read(Arg.Any<Stream>(), Arg.Is(datasetDefinition))
                .Returns(tableLoadResults.ToList());

            DatasetService service = CreateDatasetService(logger: logger, blobClient: blobClient, datasetRepository: datasetRepository,
                excelDatasetReader: datasetReader);

            //Act
            IActionResult result = await service.ValidateDataset(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okResult = result as OkObjectResult;

            DatasetValidationErrorResponse resultObject = okResult.Value as DatasetValidationErrorResponse;

            resultObject
                .Message
                .Should()
                .Be("The dataset failed to validate with 1 error");
        }

        [TestMethod]
        public async Task ValidateDataset_GivenTableResultsContainsThreeErrors_ReturnsOKResultWithMessage()
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

            IDictionary<string, string> metaData = new Dictionary<string, string>();
            metaData.Add("dataDefinitionId", DataDefintionId);

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);

            MemoryStream memoryStream = new MemoryStream(new byte[100]);

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

            List<DatasetValidationError> errors = new List<DatasetValidationError>();
            errors.Add(new DatasetValidationError { ErrorMessage = "error" });
            errors.Add(new DatasetValidationError { ErrorMessage = "error" });
            errors.Add(new DatasetValidationError { ErrorMessage = "error" });

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult{ GlobalErrors = errors }
            };

            IExcelDatasetReader datasetReader = CreateExcelDatasetReader();
            datasetReader
                .Read(Arg.Any<Stream>(), Arg.Is(datasetDefinition))
                .Returns(tableLoadResults.ToList());

            DatasetService service = CreateDatasetService(logger: logger, blobClient: blobClient, datasetRepository: datasetRepository,
                excelDatasetReader: datasetReader);

            //Act
            IActionResult result = await service.ValidateDataset(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult okResult = result as OkObjectResult;

            DatasetValidationErrorResponse resultObject = okResult.Value as DatasetValidationErrorResponse;

            resultObject
                .Message
                .Should()
                .Be("The dataset failed to validate with 3 errors");
        }

        [TestMethod]
        public async Task ValidateDataset_GivenTableResultsContainNoErrorsButFailsToSave_ReturnsStatusCode500()
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

            IDictionary<string, string> metaData = new Dictionary<string, string>();
            metaData.Add("dataDefinitionId", DataDefintionId);

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);

            MemoryStream memoryStream = new MemoryStream(new byte[100]);

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

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult{ GlobalErrors = new List<DatasetValidationError>() }
            };

            IExcelDatasetReader datasetReader = CreateExcelDatasetReader();
            datasetReader
                .Read(Arg.Any<Stream>(), Arg.Is(datasetDefinition))
                .Returns(tableLoadResults.ToList());

            ValidationResult validationResult = new ValidationResult(new[] { new ValidationFailure("any", "error") });

            IValidator<DatasetMetadataModel> datasetMetaDataModelValidator = CreateDatasetMetadataModelValidator(validationResult);

            DatasetService service = CreateDatasetService(logger: logger, blobClient: blobClient, datasetRepository: datasetRepository,
                excelDatasetReader: datasetReader, datasetMetadataModelValidator: datasetMetaDataModelValidator);

            //Act
            IActionResult result = await service.ValidateDataset(request);

            //Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(500);

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is("Failed to save the new dataset"));
        }

        [TestMethod]
        public async Task ValidateDataset_GivenTableResultsAndMetadatValidatesButFailsToSave_ReturnsInternalServerError()
        {
            //Arrange
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            const string dataDefinitionId = "definition-id";
            const string authorId = "author-id";
            const string authorName = "author-name";
            const string datasetId = "datatset-id";
            const string name = "name";
            const string description = "test description";

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
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);

            MemoryStream memoryStream = new MemoryStream(new byte[100]);

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

            //Act
            IActionResult result = await service.ValidateDataset(request);

            //Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(500);

            logger
                .Received(1)
                .Error(Arg.Is($"Failed to save dataset for id: {datasetId} with status code InternalServerError"));

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is("Failed to save the new dataset"));
        }

        [TestMethod]
        public async Task ValidateDataset_GivenTableResultsAndMetadatValidatesButFailsToSaveToSearch_ReturnsInternalServerError()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            const string dataDefinitionId = "definition-id";
            const string authorId = "author-id";
            const string authorName = "author-name";
            const string datasetId = "datatset-id";
            const string name = "name";
            const string description = "test description";

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
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);

            MemoryStream memoryStream = new MemoryStream(new byte[100]);

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

            List<IndexError> indexErrors = new List<IndexError>();
            indexErrors.Add(new IndexError());

            ISearchRepository<DatasetIndex> searchRepository = CreateSearchRepository();
            searchRepository
                .Index(Arg.Any<List<DatasetIndex>>())
                .Returns(indexErrors);

            DatasetService service = CreateDatasetService(logger: logger, blobClient: blobClient, datasetRepository: datasetRepository,
                excelDatasetReader: datasetReader, searchRepository: searchRepository);

            //Act
            IActionResult result = await service.ValidateDataset(request);

            //Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(500);
        }

        [TestMethod]
        public async Task ValidateDataset_GivenTableResultsAndMetadatValidatesAndSavesReturnsOKResult()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            const string dataDefinitionId = "definition-id";
            const string authorId = "author-id";
            const string authorName = "author-name";
            const string datasetId = "datatset-id";
            const string name = "name";
            const string description = "test description";

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
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);

            MemoryStream memoryStream = new MemoryStream(new byte[100]);

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

            DatasetService service = CreateDatasetService(logger: logger, blobClient: blobClient, datasetRepository: datasetRepository,
                excelDatasetReader: datasetReader);

            //Act
            IActionResult result = await service.ValidateDataset(request);

            //Assert
            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        async public Task GetDatasetsByDefinitionId_WhenNoDefinitionIdIsProvided_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            DatasetService service = CreateDatasetService(logger: logger);

            //Act
            IActionResult result = await service.GetDatasetsByDefinitionId(request);

            //Assert
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
            //Arrange
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

            //Act
            IActionResult result = await service.GetDatasetsByDefinitionId(request);

            //Assert
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

            //Act
            IActionResult result = await service.GetDatasetsByDefinitionId(request);

            //Assert
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

            //Act
            Func<Task> test = () => service.ProcessDataset(message);

            //Assert
            test
                .ShouldThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void ProcessDataset_GivenNullPayload_ThrowsArgumentException()
        {
            //Arrange
            Message message = new Message(new byte[0]);

            DatasetService service = CreateDatasetService();

            //Act
            Func<Task> test = () => service.ProcessDataset(message);

            //Assert
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

            //Act
            Func<Task> test = () => service.ProcessDataset(message);

            //Assert
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

            //Act
            Func<Task> test = () => service.ProcessDataset(message);

            //Assert
            test
                .ShouldThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        async public Task ProcessDataset_GivenPayloadButDatasetDefinitionCounldNotBeFound_DoesNotProcess()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            Dataset dataset = new Dataset
            {
                Definition = new Reference { Id = DataDefintionId },
                Current = new DatasetVersion { BlobName = blobPath }
            };

            var json = JsonConvert.SerializeObject(dataset);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
                 .UserProperties
                 .Add("specification-id", SpecificationId);

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
                .Returns((IEnumerable<DatasetDefinition>)null);

            ILogger logger = CreateLogger();

            DatasetService service = CreateDatasetService(datasetRepository: datasetRepository, logger: logger);

            //Act
            await service.ProcessDataset(message);

            //Assert
            logger
                .Received(1)
                .Error(Arg.Is($"Unable to find a data definition for id: {DataDefintionId}, for blob: {blobPath}"));

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Any<string>());
        }

        [TestMethod]
        async public Task ProcessDataset_GivenPayloadButBuildProjectCouldNotBeFound_DoesNotProcess()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            Dataset dataset = new Dataset
            {
                Definition = new Reference { Id = DataDefintionId },
                Current = new DatasetVersion { BlobName = blobPath }
            };

            var json = JsonConvert.SerializeObject(dataset);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
                 .UserProperties
                 .Add("specification-id", SpecificationId);

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

            DatasetService service = CreateDatasetService(datasetRepository: datasetRepository, logger: logger, calcsRepository: calcsRepository);

            //Act
            await service.ProcessDataset(message);

            //Assert
            logger
                .Received(1)
                .Error(Arg.Is($"Unable to find a build project for specification id: {SpecificationId}"));

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Any<string>());
        }

        [TestMethod]
        async public Task ProcessDataset_GivenPayloadButBlobNotFound_DoesNotProcess()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            Dataset dataset = new Dataset
            {
                Definition = new Reference { Id = DataDefintionId },
                Current = new DatasetVersion { BlobName = blobPath }
            };

            var json = JsonConvert.SerializeObject(dataset);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
                .UserProperties
                .Add("specification-id", SpecificationId);

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

            DatasetService service = CreateDatasetService(
                datasetRepository: datasetRepository, logger: logger,
                calcsRepository: calcsRepository, blobClient: blobClient);

            //Act
            await service.ProcessDataset(message);

            //Assert
            logger
                .Received(1)
                .Error(Arg.Is($"Failed to find blob with path: {blobPath}"));

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Any<string>());
        }

        [TestMethod]
        async public Task ProcessDataset_GivenPayloadAndBlobFoundButEmptyFile_DoesNotProcess()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            Dataset dataset = new Dataset
            {
                Definition = new Reference { Id = DataDefintionId },
                Current = new DatasetVersion { BlobName = blobPath }
            };

            var json = JsonConvert.SerializeObject(dataset);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
                .UserProperties
                .Add("specification-id", SpecificationId);

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

            //Act
            await service.ProcessDataset(message);

            //Assert
            logger
                .Received(1)
                .Error(Arg.Is($"Invalid blob returned: {blobPath}"));

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Any<string>());
        }

        [TestMethod]
        async public Task ProcessDataset_GivenPayloadAndBlobFoundButNoTableResultsReturned_DoesNotProcess()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            Dataset dataset = new Dataset
            {
                Definition = new Reference { Id = DataDefintionId },
                Current = new DatasetVersion { BlobName = blobPath }
            };

            var json = JsonConvert.SerializeObject(dataset);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
                .UserProperties
                .Add("specification-id", SpecificationId);

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

            //Act
            await service.ProcessDataset(message);

            //Assert
            logger
                .Received(1)
                .Error(Arg.Is($"Failed to load table result"));

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Any<string>());
        }

        [TestMethod]
        async public Task ProcessDataset_GivenPayloadAndTableResultsButNoDatasetRelationshipSummaries_DoesNotProcess()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            string dataset_cache_key = $"ds-table-rows-{blobPath}-{DataDefintionId}";

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult()
            };

            Dataset dataset = new Dataset
            {
                Definition = new Reference { Id = DataDefintionId },
                Current = new DatasetVersion { BlobName = blobPath }
            };

            var json = JsonConvert.SerializeObject(dataset);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
                .UserProperties
                .Add("specification-id", SpecificationId);

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

            BuildProject buildProject = new BuildProject { Id = BuildProjectId };

            ICalcsRepository calcsRepository = CreateCalcsRepository();
            calcsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns(buildProject);

            DatasetService service = CreateDatasetService(
                datasetRepository: datasetRepository, logger: logger,
                calcsRepository: calcsRepository, blobClient: blobClient, cacheProvider: cacheProvider);

            //Act
            await service.ProcessDataset(message);

            //Assert
            logger
                .Received(1)
                .Error(Arg.Is($"No dataset relationships found for build project with id : {BuildProjectId}"));

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Any<string>());
        }

        [TestMethod]
        async public Task ProcessDataset_GivenPayloadAndTableResultsButNoDatasetRelationshipSummaryCouldBeFound_DoesNotProcess()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            string dataset_cache_key = $"ds-table-rows-{blobPath}-{DataDefintionId}";

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult()
            };

            Dataset dataset = new Dataset
            {
                Definition = new Reference { Id = DataDefintionId },
                Current = new DatasetVersion { BlobName = blobPath }
            };

            var json = JsonConvert.SerializeObject(dataset);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
                .UserProperties
                .Add("specification-id", SpecificationId);

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

            DatasetService service = CreateDatasetService(
                datasetRepository: datasetRepository, logger: logger,
                calcsRepository: calcsRepository, blobClient: blobClient, cacheProvider: cacheProvider);

            //Act
            await service.ProcessDataset(message);

            //Assert
            logger
                .Received(1)
                .Error(Arg.Is($"No dataset relationship found for build project with id : {buildProject.Id} with data definition id {DataDefintionId}"));

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Any<string>());
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

            Dataset dataset = new Dataset
            {
                Definition = new Reference { Id = DataDefintionId },
                Current = new DatasetVersion { BlobName = blobPath }
            };

            var json = JsonConvert.SerializeObject(dataset);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
                .UserProperties
                .Add("specification-id", SpecificationId);

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
                Specification = new SpecificationSummary { Id = SpecificationId }
            };

            ICalcsRepository calcsRepository = CreateCalcsRepository();
            calcsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns(buildProject);

            IProviderRepository resultsRepository = CreateProviderRepository();

            DatasetService service = CreateDatasetService(
                datasetRepository: datasetRepository, logger: logger,
                calcsRepository: calcsRepository, blobClient: blobClient, cacheProvider: cacheProvider,
                providerRepository: resultsRepository);

            //Act
            await service.ProcessDataset(message);

            //Assert
            await
                resultsRepository
                    .DidNotReceive()
                    .UpdateProviderSourceDataset(Arg.Any<ProviderSourceDataset>());
        }

        [TestMethod]
        async public Task ProcessDataset_GivenPayloadAndTableResultsButNoIdentifiersFound_DoesNotSaveResults()
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

            Dataset dataset = new Dataset
            {
                Definition = new Reference { Id = DataDefintionId },
                Current = new DatasetVersion { BlobName = blobPath }
            };

            var json = JsonConvert.SerializeObject(dataset);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
                .UserProperties
                .Add("specification-id", SpecificationId);

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

            IProviderRepository resultsRepository = CreateProviderRepository();

            DatasetService service = CreateDatasetService(
                datasetRepository: datasetRepository, logger: logger,
                calcsRepository: calcsRepository, blobClient: blobClient, cacheProvider: cacheProvider,
                providerRepository: resultsRepository);

            //Act
            await service.ProcessDataset(message);

            //Assert
            await
                resultsRepository
                    .DidNotReceive()
                    .UpdateProviderSourceDataset(Arg.Any<ProviderSourceDataset>());
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

            Dataset dataset = new Dataset
            {
                Definition = new Reference { Id = DataDefintionId },
                Current = new DatasetVersion { BlobName = blobPath }
            };

            var json = JsonConvert.SerializeObject(dataset);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
                .UserProperties
                .Add("specification-id", SpecificationId);

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
                Specification = new SpecificationSummary { Id = SpecificationId }
            };

            ICalcsRepository calcsRepository = CreateCalcsRepository();
            calcsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns(buildProject);

            IProviderRepository resultsRepository = CreateProviderRepository();

            DatasetService service = CreateDatasetService(
                datasetRepository: datasetRepository, logger: logger,
                calcsRepository: calcsRepository, blobClient: blobClient, cacheProvider: cacheProvider,
                providerRepository: resultsRepository);

            //Act
            await service.ProcessDataset(message);

            //Assert
            await
                resultsRepository
                    .DidNotReceive()
                    .UpdateProviderSourceDataset(Arg.Any<ProviderSourceDataset>());
        }

        [TestMethod]
        async public Task ProcessDataset_GivenPayloadAndTableResultsWithProviderIds_SavesDataset()
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
                        new RowLoadResult { Identifier = "123456", IdentifierFieldType = IdentifierFieldType.UPIN, Fields = new Dictionary<string, object>{ { "UPIN", "123456" } } }
                    }
                }
            };

            Dataset dataset = new Dataset
            {
                Definition = new Reference { Id = DataDefintionId },
                Current = new DatasetVersion { BlobName = blobPath }
            };

            var json = JsonConvert.SerializeObject(dataset);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
                .UserProperties
                .Add("specification-id", SpecificationId);

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
                Specification = new SpecificationSummary { Id = SpecificationId }
            };

            ICalcsRepository calcsRepository = CreateCalcsRepository();
            calcsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns(buildProject);

            IEnumerable<ProviderSummary> summaries = new[] { new ProviderSummary { UPIN = "123456" } };

            IProviderRepository resultsRepository = CreateProviderRepository();
            resultsRepository
                .GetAllProviderSummaries()
                .Returns(summaries);

            IProvidersResultsRepository providerResultsRepository = CreateProvidesrResultsRepository();

            DatasetService service = CreateDatasetService(
                datasetRepository: datasetRepository, logger: logger,
                calcsRepository: calcsRepository, blobClient: blobClient, cacheProvider: cacheProvider,
                providerRepository: resultsRepository, providersResultsRepository: providerResultsRepository);

            //Act
            await service.ProcessDataset(message);

            //Assert
            await
                providerResultsRepository
                    .Received(1)
                    .UpdateSourceDatsets(Arg.Is<IEnumerable<ProviderSourceDataset>>(
                        m => m.First().DataDefinition.Id == DataDefintionId &&
                             m.First().DataGranularity == DataGranularity.SingleRowPerProvider &&
                             m.First().DefinesScope == false &&
                             !string.IsNullOrWhiteSpace(m.First().Id) &&
                             m.First().Specification.Id == SpecificationId &&
                             m.First().Provider.Id == "123456"
                        ), Arg.Is(SpecificationId));
        }

        [TestMethod]
        async public Task ProcessDataset_GivenPayloadAndTableResultsWithMultipleProviderIds_DoesNotSavesDataset()
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
                        new RowLoadResult { Identifier = "123456", IdentifierFieldType = IdentifierFieldType.UPIN, Fields = new Dictionary<string, object>{ { "UPIN", "123456" } } },
                        new RowLoadResult { Identifier = "222333", IdentifierFieldType = IdentifierFieldType.UPIN, Fields = new Dictionary<string, object>{ { "UPIN", "222333" } } }
                    }
                }
            };

            Dataset dataset = new Dataset
            {
                Definition = new Reference { Id = DataDefintionId },
                Current = new DatasetVersion { BlobName = blobPath }
            };

            var json = JsonConvert.SerializeObject(dataset);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
                .UserProperties
                .Add("specification-id", SpecificationId);

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
                Specification = new SpecificationSummary { Id = SpecificationId }
            };

            ICalcsRepository calcsRepository = CreateCalcsRepository();
            calcsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns(buildProject);

            IEnumerable<ProviderSummary> summaries = new[] { new ProviderSummary { UPIN = "123456" }, new ProviderSummary { UPIN = "222333" } };

            IProviderRepository resultsRepository = CreateProviderRepository();
            resultsRepository
                .GetAllProviderSummaries()
                .Returns(summaries);

            IProvidersResultsRepository providerResultsRepository = CreateProvidesrResultsRepository();

            DatasetService service = CreateDatasetService(
                datasetRepository: datasetRepository, logger: logger,
                calcsRepository: calcsRepository, blobClient: blobClient, cacheProvider: cacheProvider,
                providerRepository: resultsRepository, providersResultsRepository: providerResultsRepository);

            //Act
            await service.ProcessDataset(message);

            //Assert
            await
                providerResultsRepository
                    .Received(1)
                    .UpdateSourceDatsets(Arg.Any<IEnumerable<ProviderSourceDataset>>(), Arg.Is(SpecificationId));
        }

        static DatasetService CreateDatasetService(IBlobClient blobClient = null, ILogger logger = null,
            IDatasetRepository datasetRepository = null,
            IValidator<CreateNewDatasetModel> createNewDatasetModelValidator = null, IMapper mapper = null,
            IValidator<DatasetMetadataModel> datasetMetadataModelValidator = null, ISearchRepository<DatasetIndex> searchRepository = null,
            IValidator<GetDatasetBlobModel> getDatasetBlobModelValidator = null, ISpecificationsRepository specificationsRepository = null,
            IMessengerService messengerService = null, ServiceBusSettings eventHubSettings = null, IExcelDatasetReader excelDatasetReader = null,
            ICacheProvider cacheProvider = null, ICalcsRepository calcsRepository = null, IProviderRepository providerRepository = null, IProvidersResultsRepository providersResultsRepository = null,
            ITelemetry telemetry = null)
        {
            return new DatasetService(blobClient ?? CreateBlobClient(), logger ?? CreateLogger(),
                datasetRepository ?? CreateDatasetsRepository(),
                createNewDatasetModelValidator ?? CreateNewDatasetModelValidator(), mapper ?? CreateMapper(),
                datasetMetadataModelValidator ?? CreateDatasetMetadataModelValidator(),
                searchRepository ?? CreateSearchRepository(), getDatasetBlobModelValidator ?? CreateGetDatasetBlobModelValidator(),
                specificationsRepository ?? CreateSpecificationsRepository(), messengerService ?? CreateMessengerService(),
                eventHubSettings ?? CreateEventHubSettings(), excelDatasetReader ?? CreateExcelDatasetReader(),
                cacheProvider ?? CreateCacheProvider(), calcsRepository ?? CreateCalcsRepository(), providerRepository ?? CreateProviderRepository(),
                providersResultsRepository ?? CreateProvidesrResultsRepository(), telemetry ?? CreateTelemetry());
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

        static IProvidersResultsRepository CreateProvidesrResultsRepository()
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

        static IValidator<CreateNewDatasetModel> CreateNewDatasetModelValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
                validationResult = new ValidationResult();

            IValidator<CreateNewDatasetModel> validator = Substitute.For<IValidator<CreateNewDatasetModel>>();

            validator
               .ValidateAsync(Arg.Any<CreateNewDatasetModel>())
               .Returns(validationResult);

            return validator;
        }

        static IValidator<DatasetMetadataModel> CreateDatasetMetadataModelValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
                validationResult = new ValidationResult();

            IValidator<DatasetMetadataModel> validator = Substitute.For<IValidator<DatasetMetadataModel>>();

            validator
               .ValidateAsync(Arg.Any<DatasetMetadataModel>())
               .Returns(validationResult);

            return validator;
        }

        static IValidator<GetDatasetBlobModel> CreateGetDatasetBlobModelValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
                validationResult = new ValidationResult();

            IValidator<GetDatasetBlobModel> validator = Substitute.For<IValidator<GetDatasetBlobModel>>();

            validator
               .ValidateAsync(Arg.Any<GetDatasetBlobModel>())
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

        static IDatasetRepository CreateDatasetsRepository()
        {
            return Substitute.For<IDatasetRepository>();
        }
    }
}
