using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Services.Core.Extensions;
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
using BadRequestObjectResult = Microsoft.AspNetCore.Mvc.BadRequestObjectResult;
using ValidationResult = FluentValidation.Results.ValidationResult;

namespace CalculateFunding.Services.Datasets.Services
{
    [TestClass]
    public class DatasetServiceTests : DatasetServiceTestsBase
    {
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
                .Should().BeEquivalentTo(new NewDatasetVersionResponseModel()
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
                .Should().BeEquivalentTo(new DatasetVersionResponseViewModel()
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
                .Should().BeEquivalentTo(new DatasetVersionResponseViewModel()
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


    }
}
