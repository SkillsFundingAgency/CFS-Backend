using AutoMapper;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
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
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets.Services
{
    [TestClass]
    public class DatasetServiceTests
    {
        const string DatasetName = "test-dataset";
        const string Username = "test-user";
        const string UserId = "33d7a71b-f570-4425-801b-250b9129f3d3";

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

            IDataSetsRepository datasetsRepository = CreateDatasetsRepository();
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

            IDataSetsRepository datasetsRepository = CreateDatasetsRepository();
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
        public void SaveNewDataset_GivenNullBlobProvided_ThrowsArgumentNullException()
        {
            //Arrange
            ICloudBlob blob = null;

            ILogger logger = CreateLogger();
            DatasetService service = CreateDatasetService(logger: logger);

            //Act
            Func<Task> test = async () => await service.SaveNewDataset(blob);

            //Assert
            test
              .ShouldThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void SaveNewDataset_GivenNullBlobMetadataFound_ThrowsArgumentNullException()
        {
            //Arrange
            IDictionary<string, string> metaData = null;

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);

            ILogger logger = CreateLogger();
            DatasetService service = CreateDatasetService(logger: logger);

            //Act
            Func<Task> test = async () => await service.SaveNewDataset(blob);

            //Assert
            test
              .ShouldThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void SaveNewDataset_GivenBlobWithMetaDataButFailsValidations_ThrowsException()
        {
            //Arrange
            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
               .Name
               .Returns("testname");

            ILogger logger = CreateLogger();

            ValidationResult validationResult = new ValidationResult(new[]{
                    new ValidationFailure("prop1", "any error")
                });

            IValidator<DatasetMetadataModel> validator = CreateDatasetMetadataModelValidator(validationResult);

            DatasetService service = CreateDatasetService(logger: logger, datasetMetadataModelValidator: validator);

            //Act
            Func<Task> test = async () => await service.SaveNewDataset(blob);

            //Assert
            test();

            logger
                .Received(1)
                .Error(Arg.Is($"Invalid metadata on blob: testname"));

            test
              .ShouldThrowExactly<Exception>();
        }

        [TestMethod]
        public void SaveNewDataset_GivenDataDefintionCouldNotBeFound_ThrowsException()
        {
            //Arrange
            const string dataDefinitionId = "definition-id";

            IDictionary<string, string> metaData = new Dictionary<string, string>
            {
                { "dataDefinitionId", dataDefinitionId }
            };

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);
            blob
                .Name
                .Returns("testname");

            ILogger logger = CreateLogger();

            IDataSetsRepository dataSetsRepository = CreateDatasetsRepository();
            dataSetsRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
                .Returns(Enumerable.Empty<DatasetDefinition>());

            DatasetService service = CreateDatasetService(logger: logger, datasetRepository: dataSetsRepository);

            //Act
            Func<Task> test = async () => await service.SaveNewDataset(blob);

            //Assert
            test();

            logger
                .Received(1)
                .Error(Arg.Is($"Unable to find a data definition for id: definition-id, for blob: testname"));

            test
              .ShouldThrowExactly<Exception>();
        }

        [TestMethod]
        public void SaveNewDataset_GivenModelButFailedToSave_ThrowsException()
        {
            //Arrange
            const string dataDefinitionId = "definition-id";

            IDictionary<string, string> metaData = new Dictionary<string, string>
            {
                { "dataDefinitionId", dataDefinitionId }
            };

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob
                .Metadata
                .Returns(metaData);
            blob
                .Name
                .Returns("testname");

            ILogger logger = CreateLogger();

            IDataSetsRepository dataSetsRepository = CreateDatasetsRepository();
            dataSetsRepository
                .GetDatasetDefinitionsByQuery(Arg.Any<Expression<Func<DatasetDefinition, bool>>>())
                .Returns(Enumerable.Empty<DatasetDefinition>());

            DatasetService service = CreateDatasetService(logger: logger, datasetRepository: dataSetsRepository);

            //Act
            Func<Task> test = async () => await service.SaveNewDataset(blob);

            //Assert
            test();

            logger
                .Received(1)
                .Error(Arg.Is($"Unable to find a data definition for id: definition-id, for blob: testname"));

            test
              .ShouldThrowExactly<Exception>();
        }

        static DatasetService CreateDatasetService(IBlobClient blobClient = null, ILogger logger = null, 
            IDataSetsRepository datasetRepository = null, 
            IValidator<CreateNewDatasetModel> createNewDatasetModelValidator = null, IMapper mapper = null,
            IValidator<DatasetMetadataModel> datasetMetadataModelValidator = null, ISearchRepository<DatasetIndex> searchRepository = null)
        {
            return new DatasetService(blobClient ?? CreateBlobClient(), logger ?? CreateLogger(), 
                datasetRepository ?? CreateDatasetsRepository(), 
                createNewDatasetModelValidator ?? CreateNewDatasetModelValidator(), mapper ?? CreateMapper(),
                datasetMetadataModelValidator ?? CreateDatasetMetadataModelValidator(), searchRepository ?? CreateSearchRepository());
        }

        static ISearchRepository<DatasetIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<DatasetIndex>>();
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

        static IDataSetsRepository CreateDatasetsRepository()
        {
            return Substitute.For<IDataSetsRepository>();
        }
    }
}
