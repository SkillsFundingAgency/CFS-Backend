using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Results.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Storage.Blob;
using NSubstitute;
using Serilog;
using BadRequestObjectResult = Microsoft.AspNetCore.Mvc.BadRequestObjectResult;
using ValidationResult = FluentValidation.Results.ValidationResult;
using CalculateFunding.Models.Messages;
using Microsoft.AspNetCore.Http.Internal;

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
                 .GetDatasetsByQuery(Arg.Any<Expression<Func<DocumentEntity<Dataset>, bool>>>())
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
                 .GetDatasetsByQuery(Arg.Any<Expression<Func<DocumentEntity<Dataset>, bool>>>())
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
            ILogger logger = CreateLogger();

            DatasetService service = CreateDatasetService(logger: logger);

            // Act
            IActionResult result = await service.CreateNewDataset(null, null);

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
            
            ILogger logger = CreateLogger();

            ValidationResult validationResult = new ValidationResult(new[]{
                    new ValidationFailure("prop1", "any error")
                });

            IValidator<CreateNewDatasetModel> validator = CreateNewDatasetModelValidator(validationResult);


            DatasetService service = CreateDatasetService(logger: logger, createNewDatasetModelValidator: validator);

            // Act
            IActionResult result = await service.CreateNewDataset(model, null);

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

            Reference reference = new Reference(UserId, Username);

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
            IActionResult result = await service.CreateNewDataset(model, reference);

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
            ILogger logger = CreateLogger();

            DatasetService service = CreateDatasetService(logger: logger);

            // Act
            IActionResult result = await service.GetDatasetsByDefinitionId(null);

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
            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetsByQuery(Arg.Any<Expression<Func<DocumentEntity<Dataset>, bool>>>())
                .Returns((IEnumerable<Dataset>)null);

            DatasetService service = CreateDatasetService(datasetRepository: datasetRepository);

            // Act
            IActionResult result = await service.GetDatasetsByDefinitionId(DataDefintionId);

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

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetsByQuery(Arg.Any<Expression<Func<DocumentEntity<Dataset>, bool>>>())
                .Returns(datasets);

            DatasetViewModel datasetViewModel = new DatasetViewModel();

            IMapper mapper = CreateMapper();
            mapper
                .Map<DatasetViewModel>(Arg.Any<Dataset>())
                .Returns(datasetViewModel);

            DatasetService service = CreateDatasetService(datasetRepository: datasetRepository, mapper: mapper);

            // Act
            IActionResult result = await service.GetDatasetsByDefinitionId(DataDefintionId);

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
            ILogger logger = CreateLogger();

            DatasetService service = CreateDatasetService(logger: logger);

            // Act
            IActionResult result = await service.DownloadDatasetFile(null, null);

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
            ILogger logger = CreateLogger();

            DatasetService service = CreateDatasetService(logger: logger);

            // Act
            IActionResult result = await service.DownloadDatasetFile(DatasetId, null);

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
	        Dataset dataset = new Dataset()
	        {
		        History = new List<DatasetVersion>()
		        {
			        new DatasetVersion()
			        {
				        BlobName = "BlobName"
			        }
		        }
	        };

            ILogger logger = CreateLogger();

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetByDatasetId(Arg.Is(DatasetId))
                .Returns(dataset);

            DatasetService service = CreateDatasetService(logger: logger, datasetRepository: datasetRepository);

            // Act
            IActionResult result = await service.DownloadDatasetFile(DatasetId, null);

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

            Dataset dataset = new Dataset
            {
                Current = new DatasetVersion
                {
                    BlobName = blobName
                },
	            History = new List<DatasetVersion>()
	            {
		            new DatasetVersion()
		            {
			            BlobName = "BlobName"
		            }
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
            IActionResult result = await service.DownloadDatasetFile(DatasetId, null);

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

            Dataset dataset = new Dataset
            {
                Current = new DatasetVersion
                {
                    BlobName = blobName
                },
	            History = new List<DatasetVersion>()
	            {
		            new DatasetVersion()
		            {
			            BlobName = "BlobName"
		            }
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
            IActionResult result = await service.DownloadDatasetFile(DatasetId, null);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

	        OkObjectResult okObjectResult = result as OkObjectResult;
	        DatasetDownloadModel objectResult = okObjectResult.Value as DatasetDownloadModel;

	        objectResult.Url.Should().Be(blobUrl);
        }

		[TestMethod]
		public async Task DownloadDatasetFile_GivenVersionIsSpecifiedAndVersionExists_ReturnsOKResult()
		{
			//Arrange
			const string blobUrl = "http://this-is-a-bloburl?this=is-a-token";
			const string blobName = "blob-name.xlsx";
			const int versionSpecified = 2;

			Dataset dataset = new Dataset
			{
				Current = new DatasetVersion
				{
					BlobName = "CurrentsBlobName"
				},
				History = new List<DatasetVersion>()
				{
					new DatasetVersion()
					{
						BlobName = "blobName v1",
						Version = 1
					},
					new DatasetVersion()
					{
						BlobName = blobName,
						Version = versionSpecified
					}
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
					.GetBlobSasUrl(blobName, Arg.Any<DateTimeOffset>(), Arg.Any<SharedAccessBlobPermissions>())
					.Returns(blobUrl);

			DatasetService service = CreateDatasetService(logger: logger, datasetRepository: datasetRepository, blobClient: blobClient);

			// Act
			IActionResult result = await service.DownloadDatasetFile(DatasetId, versionSpecified.ToString());

			// Assert
			result
				.Should()
				.BeOfType<OkObjectResult>();

			OkObjectResult okObjectResult = result as OkObjectResult;
			DatasetDownloadModel objectResult = okObjectResult.Value as DatasetDownloadModel;

			objectResult.Url.Should().Be(blobUrl);
		}

		[TestMethod]
		public async Task DownloadDatasetFile_GivenVersionIsSpecifiedButDoesNotExist_ReturnsPreconditionFailedResult()
		{
			//Arrange
			const string blobUrl = "http://this-is-a-bloburl?this=is-a-token";
			const string blobName = "blob-name.xlsx";
			const int versionSpecified = 2;

			Dataset dataset = new Dataset
			{
				Current = new DatasetVersion
				{
					BlobName = "CurrentsBlobName"
				},
				History = new List<DatasetVersion>()
				{
					new DatasetVersion()
					{
						BlobName = "blobName v1",
						Version = 1
					},
					new DatasetVersion()
					{
						BlobName = blobName,
						Version = 3
					}
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
					.GetBlobSasUrl(blobName, Arg.Any<DateTimeOffset>(), Arg.Any<SharedAccessBlobPermissions>())
					.Returns(blobUrl);

			DatasetService service = CreateDatasetService(logger: logger, datasetRepository: datasetRepository, blobClient: blobClient);

			// Act
			IActionResult result = await service.DownloadDatasetFile(DatasetId, versionSpecified.ToString());

			// Assert
			result
				.Should()
				.BeOfType<StatusCodeResult>()
				.Which
				.StatusCode
				.Should().Be(412);
			
		}

		[TestMethod]
	    public async Task DownloadDatasetFile_GivenAnInvalidVersionWasProvided_ReturnsBadRequest()
	    {
		    //Arrange

			ILogger logger = CreateLogger();

		    DatasetService service = CreateDatasetService(logger: logger);

		    // Act
		    IActionResult result = await service.DownloadDatasetFile(DatasetId, "one");

		    // Assert
		    result
			    .Should()
			    .BeOfType<BadRequestObjectResult>();
	    }

        [TestMethod]
        public async Task UploadDatasetFile_GivenNewFIle_ReturnsOkResult()
        {
            const string authorId = "authId";
            const string authorName = "Change Author";
            const string datasetId = "ds1";
            const string datasetName = "ds1";
            const string dataDefinitionId = "dd1";
            const string filename = "file.xls";

            byte[] byteArray = Encoding.UTF8.GetBytes(filename);

            DatasetMetadataViewModel datasetMetadataViewModel = new DatasetMetadataViewModel
            {
                AuthorId = authorId,
                AuthorName = authorName,
                DataDefinitionId = dataDefinitionId,
                DatasetId = datasetId,
                Name = datasetName,
                Stream = byteArray
            };

            IBlobClient blobClient = CreateBlobClient();

            ICloudBlob cloudBlob = CreateBlob();

            DatasetService datasetService = CreateDatasetService(blobClient: blobClient);

            blobClient.GetBlockBlobReference($"{datasetMetadataViewModel.DatasetId}/v1/{filename}")
                .Returns(cloudBlob);

            // Act
            IActionResult result = await datasetService.UploadDatasetFile(filename, datasetMetadataViewModel);

            // Assert
            result
                .Should()
                .BeOfType<OkResult>();

            await cloudBlob
                .Received()
                .UploadFromStreamAsync(Arg.Any<Stream>());

            cloudBlob.Metadata["dataDefinitionId"]
                .Should()
                .Be(datasetMetadataViewModel.DataDefinitionId);

            cloudBlob.Metadata["datasetId"]
                .Should()
                .Be(datasetMetadataViewModel.DatasetId);

            cloudBlob.Metadata["authorId"]
                .Should()
                .Be(datasetMetadataViewModel.AuthorId);

            cloudBlob.Metadata["authorName"]
                .Should()
                .Be(datasetMetadataViewModel.AuthorName);

            cloudBlob.Metadata["name"]
                .Should()
                .Be(datasetMetadataViewModel.Name);

            cloudBlob.Metadata["description"]
                .Should()
                .Be(datasetMetadataViewModel.Description); 
            
            cloudBlob
                 .Received()
                 .SetMetadata();
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

            Reference author = new Reference(authorId, authorName);

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
            IActionResult result = await datasetService.DatasetVersionUpdate(model, author);

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

            // Act
            IActionResult result = await datasetService.DatasetVersionUpdate(model, null);

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

            // Act
            IActionResult result = await datasetService.DatasetVersionUpdate(model, null);

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

            datasetRepository
               .GetDatasetByDatasetId(Arg.Any<string>())
               .Returns((Dataset)null);

            // Act
            IActionResult result = await datasetService.DatasetVersionUpdate(model, null);

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

            DatasetVersion datasetVersion = new DatasetVersion()
            {
                Version = 1,
                Author = new Reference("authorId", "Author Name"),
                BlobName = "file/name.xlsx",
                Comment = "My update comment",
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
            IActionResult result = await datasetService.GetCurrentDatasetVersionByDatasetId(datasetId);

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

            DatasetVersion datasetVersion1 = new DatasetVersion()
            {
                Version = 1,
                Author = new Reference("authorId", "Author Name"),
                BlobName = "file/name.xlsx",
                Comment = "My update comment",
                Date = new DateTime(2018, 12, 1, 3, 4, 5),
                PublishStatus = Models.Versioning.PublishStatus.Draft,
            };

            DatasetVersion datasetVersion2 = new DatasetVersion()
            {
                Version = 2,
                Author = new Reference("authorId2", "Author Name Two"),
                BlobName = "file/name2.xlsx",
                Comment = "My update comment for second",
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
            IActionResult result = await datasetService.GetCurrentDatasetVersionByDatasetId(datasetId);

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

            // Act
            IActionResult result = await datasetService.GetCurrentDatasetVersionByDatasetId(datasetId);

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

            datasetRepository
                .GetDatasetDocumentByDatasetId(Arg.Is(datasetId))
                .Returns((DocumentEntity<Dataset>)null);

            // Act
            IActionResult result = await datasetService.GetCurrentDatasetVersionByDatasetId(datasetId);

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

            datasetRepository
                .GetDatasetDocumentByDatasetId(Arg.Is(datasetId))
                .Returns(new DocumentEntity<Dataset>(null));

            // Act
            IActionResult result = await datasetService.GetCurrentDatasetVersionByDatasetId(datasetId);

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

            datasetRepository
                .GetDatasetDocumentByDatasetId(Arg.Is(datasetId))
                .Returns(new DocumentEntity<Dataset>(new Dataset()));

            // Act
            IActionResult result = await datasetService.GetCurrentDatasetVersionByDatasetId(datasetId);

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

        [TestMethod]
        public void UpdateDatasetAndVersionDefinitionName_GivenANullDefinitionRefrenceSupplied_LogsAndThrowsException()
        {
            //Arrange
            Reference reference = null;

            ILogger logger = CreateLogger();

            DatasetService datasetService = CreateDatasetService(logger: logger);

            //Act
            Func<Task> test = () => datasetService.UpdateDatasetAndVersionDefinitionName(reference);

            //Assert
            test
               .Should()
               .ThrowExactly<NonRetriableException>();

            logger
                .Received(1)
                .Error("Null dataset definition reference supplied");
        }

        [TestMethod]
        public async Task UpdateDatasetAndVersionDefinitionName_GivenDatasetFound_LogsAndDoesNotProcess()
        {
            //Arrange
            const string definitionId = "id-1";
            const string defintionName = "name-1";

            Reference reference = new Reference(definitionId, defintionName);

            ILogger logger = CreateLogger();

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetsByQuery(Arg.Any<Expression<Func<DocumentEntity<Dataset>, bool>>>())
                .Returns(Enumerable.Empty<Dataset>());

            DatasetService datasetService = CreateDatasetService(logger: logger, datasetRepository: datasetRepository);

            //Act
            await datasetService.UpdateDatasetAndVersionDefinitionName(reference);

            //Assert
            logger
                .Received(1)
                .Information(Arg.Is($"No datasets found to update for definition id: {definitionId}"));
        }

        [TestMethod]
        public void UpdateDatasetAndVersionDefinitionName_GivenUpdatingCosmosCausesException_LogsAndThrowsException()
        {
            //Arrange
            const string definitionId = "id-1";
            const string defintionName = "name-1";

            Reference reference = new Reference(definitionId, defintionName);

            IEnumerable<Dataset> datasets = new[]
            {
                new Dataset{ Definition = new Reference() }
            };

            ILogger logger = CreateLogger();

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetsByQuery(Arg.Any<Expression<Func<DocumentEntity<Dataset>, bool>>>())
                .Returns(datasets);

            datasetRepository
                .When(x => x.SaveDatasets(Arg.Any<IEnumerable<Dataset>>()))
                .Do(x => { throw new Exception(); });

            DatasetService datasetService = CreateDatasetService(logger: logger, datasetRepository: datasetRepository);

            //Act
            Func<Task> test = () => datasetService.UpdateDatasetAndVersionDefinitionName(reference);

            //Assert
            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be($"Failed to save datasets to cosmos for definition id: {definitionId}");

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is($"Failed to save datasets to cosmos for definition id: {definitionId}"));
        }

        [TestMethod]
        public void UpdateDatasetAndVersionDefinitionName_GivenUpdatingDatasetInSearchCausesErrors_LogsAndThrowsException()
        {
            //Arrange
            const string definitionId = "id-1";
            const string defintionName = "name-1";

            Reference reference = new Reference(definitionId, defintionName);

            IEnumerable<Dataset> datasets = new[]
            {
                new Dataset{
                    Definition = new Reference(definitionId, defintionName),
                    History = new List<DatasetVersion>
                    {

                    },
                    Current = new DatasetVersion
                    {
                        PublishStatus = Models.Versioning.PublishStatus.Approved
                    }
                }
            };

            ILogger logger = CreateLogger();

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            datasetRepository
                .GetDatasetsByQuery(Arg.Any<Expression<Func<DocumentEntity<Dataset>, bool>>>())
                .Returns(datasets);

            ISearchRepository<DatasetIndex> datasetSearchRepository = CreateSearchRepository();

            IEnumerable<IndexError> indexErrors = new List<IndexError>()
            {
                new IndexError()
                {
                      Key = "datasetId",
                      ErrorMessage = "Error in dataset ID for search",
                }
            };

            datasetSearchRepository
                .Index(Arg.Any<List<DatasetIndex>>())
                .Returns(indexErrors);

            DatasetService datasetService = CreateDatasetService(
                logger: logger, 
                datasetRepository: datasetRepository,
                searchRepository: datasetSearchRepository
                );

            //Act
            Func<Task> test = () => datasetService.UpdateDatasetAndVersionDefinitionName(reference);

            //Assert
            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be($"Failed to save dataset to search for definition id: {definitionId} in search with errors: Error in dataset ID for search");

            logger
                .Received(1)
                .Error(Arg.Is($"Failed to save dataset to search for definition id: {definitionId} in search with errors: Error in dataset ID for search"));
        }

        [TestMethod]
        [DataRow("SpecId1", DeletionType.SoftDelete)]
        [DataRow("SpecId1", DeletionType.PermanentDelete)]
        public async Task DeleteDatasets_Deletes_Dependencies_Using_Correct_SpecificationId_And_DeletionType(string specificationId, DeletionType deletionType)
        {
            const string dataDefinitionRelationshipId = "22";
            Message message = new Message
            {
                UserProperties =
                {
                    new KeyValuePair<string, object>("specification-id", specificationId),
                    new KeyValuePair<string, object>("deletion-type", (int)deletionType)
                }
            };
            var specificationSummary = new SpecificationSummary
            {
                DataDefinitionRelationshipIds = new[] {dataDefinitionRelationshipId}
            };
            ISpecificationsApiClient specificationsApiClient = Substitute.For<ISpecificationsApiClient>();
            var apiResponseWithSpecSummary = new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, specificationSummary);
            specificationsApiClient.GetSpecificationSummaryById(specificationId).Returns(apiResponseWithSpecSummary);
            IProviderSourceDatasetRepository providerSourceDatasetRepository = Substitute.For<IProviderSourceDatasetRepository>();
            IDatasetRepository datasetRepository = Substitute.For<IDatasetRepository>();

            DatasetService service = CreateDatasetService(
                providerSourceDatasetRepository: providerSourceDatasetRepository, 
                datasetRepository:datasetRepository,
                specificationsApiClient:specificationsApiClient);

            IActionResult result = await service.DeleteDatasets(message);

            await providerSourceDatasetRepository.Received(1).DeleteProviderSourceDataset(dataDefinitionRelationshipId, deletionType); 
            await providerSourceDatasetRepository.Received(1).DeleteProviderSourceDatasetVersion(dataDefinitionRelationshipId, deletionType);
            await datasetRepository.Received(1).DeleteDefinitionSpecificationRelationshipBySpecificationId(specificationId, deletionType);
            await datasetRepository.Received(1).DeleteDatasetsBySpecificationId(specificationId, deletionType);
            result.Should().BeOfType<OkResult>();
        }
    }
}
