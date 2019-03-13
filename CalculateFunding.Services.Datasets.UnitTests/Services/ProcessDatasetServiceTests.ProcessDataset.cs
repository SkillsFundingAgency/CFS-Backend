using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Models;
using CalculateFunding.Models;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.DataImporter;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Datasets.Services
{
    [TestClass]
    public class ProcessDatasetsServiceProcessDatasetTests : ProcessDatasetServiceTestsBase
    {
        [TestMethod]
        public void ProcessDataset_GivenNullMessage_ThrowsArgumentNullException()
        {
            //Arrange
            Message message = null;

            ProcessDatasetService service = CreateProcessDatasetService();

            // Act
            Func<Task> test = () => service.ProcessDataset(message);

            // Assert
            test
                .Should().ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public async Task ProcessDataset_GivenNullPayload_DoesNoProcessing()
        {
            //Arrange
            Message message = new Message(new byte[0]);
            message.UserProperties.Add("jobId", "job1");

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            ILogger logger = CreateLogger();

            ProcessDatasetService service = CreateProcessDatasetService(datasetRepository: datasetRepository, logger: logger);

            // Act
            await service.ProcessDataset(message);

            // Assert
            await datasetRepository
                .DidNotReceive()
                .GetDefinitionSpecificationRelationshipById(Arg.Any<string>());

            logger
                .Received(1)
                .Error(Arg.Is("A null dataset was provided to ProcessData"));
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadButNoSpecificationIdKeyinProperties_DoesNoProcessing()
        {
            //Arrange
            Dataset dataset = new Dataset();

            string json = JsonConvert.SerializeObject(dataset);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("jobId", "job1");

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            ILogger logger = CreateLogger();

            ProcessDatasetService service = CreateProcessDatasetService(datasetRepository: datasetRepository, logger: logger);

            // Act
            await service.ProcessDataset(message);

            // Assert
            await datasetRepository
                .DidNotReceive()
                .GetDefinitionSpecificationRelationshipById(Arg.Any<string>());

            logger
                .Received(1)
                .Error("Specification Id key is missing in ProcessDataset message properties");
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadButNoSpecificationIdValueinProperties_DoesNoProcessing()
        {
            //Arrange
            Dataset dataset = new Dataset();

            string json = JsonConvert.SerializeObject(dataset);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("specification-id", "");
            message.UserProperties.Add("jobId", "job1");

            IDatasetRepository datasetRepository = CreateDatasetsRepository();
            ILogger logger = CreateLogger();

            ProcessDatasetService service = CreateProcessDatasetService(datasetRepository: datasetRepository, logger: logger);

            // Act
            await service.ProcessDataset(message);

            // Assert
            await datasetRepository
                .DidNotReceive()
                .GetDefinitionSpecificationRelationshipById(Arg.Any<string>());

            logger
                .Received(1)
                .Error("A null or empty specification id was provided to ProcessData");
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadButDatasetDefinitionCouldNotBeFound_DoesNotProcess()
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


            string json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("specification-id", SpecificationId);
            message.UserProperties.Add("relationship-id", relationshipId);
            message.UserProperties.Add("jobId", "job1");

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

            ProcessDatasetService service = CreateProcessDatasetService(datasetRepository: datasetRepository, logger: logger);

            // Act
            await service.ProcessDataset(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is($"Unable to find a data definition for id: {DataDefintionId}, for blob: {blobPath}"));

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Any<string>());
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadButBuildProjectCouldNotBeFound_DoesNotProcess()
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

            string json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("specification-id", SpecificationId);
            message.UserProperties.Add("relationship-id", relationshipId);
            message.UserProperties.Add("jobId", "job1");

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

            ProcessDatasetService service = CreateProcessDatasetService(datasetRepository: datasetRepository, logger: logger, calcsRepository: calcsRepository);

            // Act
            await service.ProcessDataset(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is($"Unable to find a build project for specification id: {SpecificationId}"));

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Any<string>());
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadButBlobNotFound_DoesNotProcess()
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

            string json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("specification-id", SpecificationId);
            message.UserProperties.Add("relationship-id", relationshipId);
            message.UserProperties.Add("jobId", "job1");

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

            ProcessDatasetService service = CreateProcessDatasetService(
                datasetRepository: datasetRepository, logger: logger,
                calcsRepository: calcsRepository, blobClient: blobClient);

            // Act
            await service.ProcessDataset(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is($"Failed to find blob with path: {blobPath}"));

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Any<string>());
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadAndBlobFoundButEmptyFile_DoesNotProcess()
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

            string json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("specification-id", SpecificationId);
            message.UserProperties.Add("relationship-id", relationshipId);
            message.UserProperties.Add("jobId", "job1");

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

            ProcessDatasetService service = CreateProcessDatasetService(
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
            await service.ProcessDataset(message);

            // Assert
            logger
                .Received(1)
                .Error(Arg.Is($"Invalid blob returned: {blobPath}"));

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Any<string>());
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadAndBlobFoundButNoTableResultsReturned_DoesNotProcess()
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

            string json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("specification-id", SpecificationId);
            message.UserProperties.Add("relationship-id", relationshipId);
            message.UserProperties.Add("jobId", "job1");

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

            ProcessDatasetService service = CreateProcessDatasetService(
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

            await service.ProcessDataset(message);

            // Assert
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

            string dataset_cache_key = $"ds-table-rows:{ProcessDatasetService.GetBlobNameCacheKey(blobPath)}:{DataDefintionId}";

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

            string json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("specification-id", SpecificationId);
            message.UserProperties.Add("relationship-id", relationshipId);
            message.UserProperties.Add("jobId", "job1");

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

            ProcessDatasetService service = CreateProcessDatasetService(
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

            string dataset_cache_key = $"ds-table-rows:{ProcessDatasetService.GetBlobNameCacheKey(blobPath)}:{DataDefintionId}";

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

            string json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("specification-id", SpecificationId);
            message.UserProperties.Add("relationship-id", relationshipId);
            message.UserProperties.Add("jobId", "job1");
            message.UserProperties.Add("user-id", UserId);
            message.UserProperties.Add("user-name", Username);

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

            ProcessDatasetService service = CreateProcessDatasetService(
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

            string json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("specification-id", SpecificationId);
            message.UserProperties.Add("relationship-id", relationshipId);
            message.UserProperties.Add("jobId", "job1");

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

            ProcessDatasetService service = CreateProcessDatasetService(
                datasetRepository: datasetRepository, logger: logger,
                calcsRepository: calcsRepository, blobClient: blobClient, cacheProvider: cacheProvider,
                providerResultsRepository: resultsRepository);

            // Act
            await service.ProcessDataset(message);

            // Assert
            await
                resultsRepository
                    .DidNotReceive()
                    .UpdateCurrentProviderSourceDatasets(Arg.Any<IEnumerable<ProviderSourceDataset>>());

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

            string json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("specification-id", SpecificationId);
            message.UserProperties.Add("relationship-id", relationshipId);
            message.UserProperties.Add("jobId", "job1");

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

            ProcessDatasetService service = CreateProcessDatasetService(
                datasetRepository: datasetRepository, logger: logger,
                calcsRepository: calcsRepository, blobClient: blobClient, cacheProvider: cacheProvider,
                providerResultsRepository: resultsRepository);

            // Act
            await service.ProcessDataset(message);

            // Assert
            await
                resultsRepository
                    .DidNotReceive()
                    .UpdateCurrentProviderSourceDatasets(Arg.Any<IEnumerable<ProviderSourceDataset>>());

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

            string json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("specification-id", SpecificationId);
            message.UserProperties.Add("relationship-id", relationshipId);
            message.UserProperties.Add("jobId", "job1");

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

            ProcessDatasetService service = CreateProcessDatasetService(
                datasetRepository: datasetRepository, logger: logger,
                calcsRepository: calcsRepository, blobClient: blobClient, cacheProvider: cacheProvider,
                providerResultsRepository: resultsRepository);

            // Act
            await service.ProcessDataset(message);

            // Assert
            await
                resultsRepository
                    .DidNotReceive()
                    .UpdateCurrentProviderSourceDatasets(Arg.Any<IEnumerable<ProviderSourceDataset>>());

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

            string dataset_aggregations_cache_key = $"{CacheKeys.DatasetAggregationsForSpecification}{SpecificationId}";

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

            string json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";
            string relationshipName = "Relationship Name";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("specification-id", SpecificationId);
            message.UserProperties.Add("relationship-id", relationshipId);
            message.UserProperties.Add("jobId", "job1");

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

            ProcessDatasetService service = CreateProcessDatasetService(
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
                    .UpdateCurrentProviderSourceDatasets(Arg.Is<IEnumerable<ProviderSourceDataset>>(
                        m => m.First().DataDefinition.Id == DataDefintionId &&
                             m.First().DataGranularity == DataGranularity.SingleRowPerProvider &&
                             m.First().DefinesScope == false &&
                             !string.IsNullOrWhiteSpace(m.First().Id) &&
                             m.First().SpecificationId == SpecificationId &&
                             m.First().ProviderId == "123"
                        ));
        }

        [TestMethod]
        async public Task ProcessDataset_GivenPayloadAndTableResultsWithProviderIdsAndLaCodesAsIdentifiers_SavesDataset()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            string dataset_cache_key = $"ds-table-rows:{blobPath}:{DataDefintionId}";

            string dataset_aggregations_cache_key = $"{CacheKeys.DatasetAggregationsForSpecification}{SpecificationId}";

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult
                {
                    Rows = new List<RowLoadResult>
                    {
                        new RowLoadResult { Identifier = "111", IdentifierFieldType = IdentifierFieldType.LACode, Fields = new Dictionary<string, object>{ { "UPIN", "123456" }, { "LACode", "111" } } }
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

            string json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";
            string relationshipName = "Relationship Name";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("specification-id", SpecificationId);
            message.UserProperties.Add("relationship-id", relationshipId);
            message.UserProperties.Add("jobId", "job1");

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
                                    Name = "UPIN",
                                },
                                new FieldDefinition
                                {
                                    IdentifierFieldType = IdentifierFieldType.LACode,
                                    Name = "LACode",
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
                new ProviderSummary { Id = "123",  UPIN = "123456", LACode = "111" },
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

            ProcessDatasetService service = CreateProcessDatasetService(
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
                    .UpdateCurrentProviderSourceDatasets(Arg.Is<IEnumerable<ProviderSourceDataset>>(
                        m => m.First().DataDefinition.Id == DataDefintionId &&
                             m.First().DataGranularity == DataGranularity.SingleRowPerProvider &&
                             m.First().DefinesScope == false &&
                             !string.IsNullOrWhiteSpace(m.First().Id) &&
                             m.First().SpecificationId == SpecificationId &&
                             m.First().ProviderId == "123" &&
                             m.First().Current.Rows[0]["LACode"].ToString() == "111"
                        ));
        }

        [TestMethod]
        async public Task ProcessDataset_GivenPayloadAndTableResultsWithProviderIdsAndIsAggregatesFeatureToggleEnabledButNoAggretableFields_SavesDataset()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            string dataset_cache_key = $"ds-table-rows:{blobPath}:{DataDefintionId}";

            string dataset_aggregations_cache_key = $"{CacheKeys.DatasetAggregationsForSpecification}{SpecificationId}";

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

            string json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";
            string relationshipName = "Relationship Name";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("specification-id", SpecificationId);
            message.UserProperties.Add("relationship-id", relationshipId);
            message.UserProperties.Add("jobId", "job1");

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

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsAggregateSupportInCalculationsEnabled()
                .Returns(true);

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

            IDatasetsAggregationsRepository datasetsAggregationsRepository = CreateDatasetsAggregationsRepository();

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

            ProcessDatasetService service = CreateProcessDatasetService(
                datasetRepository: datasetRepository,
                logger: logger,
                calcsRepository: calcsRepository,
                blobClient: blobClient,
                cacheProvider: cacheProvider,
                providerRepository: resultsRepository,
                providerResultsRepository: providerResultsRepository,
                excelDatasetReader: excelDatasetReader,
                datasetsAggregationsRepository: datasetsAggregationsRepository,
                featureToggle: featureToggle);

            // Act
            await service.ProcessDataset(message);

            // Assert
            await
                providerResultsRepository
                    .Received(1)
                    .UpdateCurrentProviderSourceDatasets(Arg.Is<IEnumerable<ProviderSourceDataset>>(
                        m => m.First().DataDefinition.Id == DataDefintionId &&
                             m.First().DataGranularity == DataGranularity.SingleRowPerProvider &&
                             m.First().DefinesScope == false &&
                             !string.IsNullOrWhiteSpace(m.First().Id) &&
                             m.First().SpecificationId == SpecificationId &&
                             m.First().ProviderId == "123"
                        ));

            await
                datasetsAggregationsRepository
                    .DidNotReceive()
                    .CreateDatasetAggregations(Arg.Any<DatasetAggregations>());

            await
                cacheProvider
                    .Received(1)
                    .RemoveAsync<List<CalculationAggregation>>(Arg.Is(dataset_aggregations_cache_key));
        }

        [TestMethod]
        async public Task ProcessDataset_GivenPayloadAndTableResultsWithProviderIdsAndIsAggregatesFeatureToggleEnabledAnHasAggretableField_SavesDataset()
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
                        new RowLoadResult {
                            Identifier = "123456", IdentifierFieldType = IdentifierFieldType.UPIN, Fields = new Dictionary<string, object>{ { "UPIN", "123456"}, { "TestField", 3000 } }
                        }
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

            string json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";
            string relationshipName = "Relationship Name";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("specification-id", SpecificationId);
            message.UserProperties.Add("relationship-id", relationshipId);
            message.UserProperties.Add("jobId", "job1");

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
                                },
                                new FieldDefinition
                                {
                                    Name = "TestField",
                                    IsAggregable = true,
                                    Type = FieldType.Decimal
                                }
                            }
                        }
                    }
                }
            };

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsAggregateSupportInCalculationsEnabled()
                .Returns(true);

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

            IDatasetsAggregationsRepository datasetsAggregationsRepository = CreateDatasetsAggregationsRepository();

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

            ProcessDatasetService service = CreateProcessDatasetService(
                datasetRepository: datasetRepository,
                logger: logger,
                calcsRepository: calcsRepository,
                blobClient: blobClient,
                cacheProvider: cacheProvider,
                providerRepository: resultsRepository,
                providerResultsRepository: providerResultsRepository,
                excelDatasetReader: excelDatasetReader,
                datasetsAggregationsRepository: datasetsAggregationsRepository,
                featureToggle: featureToggle);

            // Act
            await service.ProcessDataset(message);

            // Assert
            await
                datasetsAggregationsRepository
                    .Received(1)
                    .CreateDatasetAggregations(Arg.Is<DatasetAggregations>(
                        m => m.DatasetRelationshipId == relationshipId &&
                             m.SpecificationId == SpecificationId &&
                             m.Fields.Count() == 4 &&
                             m.Fields.ElementAt(0).Value == 3000 &&
                             m.Fields.ElementAt(0).FieldType == AggregatedType.Sum &&
                             m.Fields.ElementAt(0).FieldReference == "Datasets.RelationshipName.TestField_Sum" &&
                             m.Fields.ElementAt(1).Value == 3000 &&
                             m.Fields.ElementAt(1).FieldType == AggregatedType.Average &&
                             m.Fields.ElementAt(1).FieldReference == "Datasets.RelationshipName.TestField_Average" &&
                             m.Fields.ElementAt(2).Value == 3000 &&
                             m.Fields.ElementAt(2).FieldType == AggregatedType.Min &&
                             m.Fields.ElementAt(2).FieldReference == "Datasets.RelationshipName.TestField_Min" &&
                             m.Fields.ElementAt(3).Value == 3000 &&
                             m.Fields.ElementAt(3).FieldType == AggregatedType.Max &&
                             m.Fields.ElementAt(3).FieldReference == "Datasets.RelationshipName.TestField_Max"));
        }

        [TestMethod]
        async public Task ProcessDataset_GivenPayloadAndTableResultsWithMultipleRowsWithProviderIdsAndIsAggregatesFeatureToggleEnabledAnHasAggretableField_SavesDataset()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            string dataset_cache_key = $"ds-table-rows:{blobPath}:{DataDefintionId}";

            string dataset_aggregations_cache_key = $"{CacheKeys.DatasetAggregationsForSpecification}{SpecificationId}";

            IEnumerable<TableLoadResult> tableLoadResults = new[]
            {
                new TableLoadResult
                {
                    Rows = new List<RowLoadResult>
                    {
                        new RowLoadResult {
                            Identifier = "123456", IdentifierFieldType = IdentifierFieldType.UPIN, Fields = new Dictionary<string, object>{ { "UPIN", "123456"}, { "TestField", 3000 } }
                        },
                        new RowLoadResult {
                            Identifier = "123457", IdentifierFieldType = IdentifierFieldType.UPIN, Fields = new Dictionary<string, object>{ { "UPIN", "123457"}, { "TestField", 120 } }
                        },
                        new RowLoadResult {
                            Identifier = "923457", IdentifierFieldType = IdentifierFieldType.UPIN, Fields = new Dictionary<string, object>{ { "UPIN", "923457"}, { "TestField", 10 } }
                        },
                         new RowLoadResult {
                            Identifier = "955457", IdentifierFieldType = IdentifierFieldType.UPIN, Fields = new Dictionary<string, object>{ { "UPIN", "955457"}, { "TestField", 567 } }
                        }
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

            string json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";
            string relationshipName = "Relationship Name";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("specification-id", SpecificationId);
            message.UserProperties.Add("relationship-id", relationshipId);
            message.UserProperties.Add("jobId", "job1");

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
                                },
                                new FieldDefinition
                                {
                                    Name = "TestField",
                                    IsAggregable = true,
                                    Type = FieldType.Decimal
                                }
                            }
                        }
                    }
                }
            };

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsAggregateSupportInCalculationsEnabled()
                .Returns(true);

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

            IDatasetsAggregationsRepository datasetsAggregationsRepository = CreateDatasetsAggregationsRepository();

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

            ProcessDatasetService service = CreateProcessDatasetService(
                datasetRepository: datasetRepository,
                logger: logger,
                calcsRepository: calcsRepository,
                blobClient: blobClient,
                cacheProvider: cacheProvider,
                providerRepository: resultsRepository,
                providerResultsRepository: providerResultsRepository,
                excelDatasetReader: excelDatasetReader,
                datasetsAggregationsRepository: datasetsAggregationsRepository,
                featureToggle: featureToggle);

            // Act
            await service.ProcessDataset(message);

            // Assert
            await
                datasetsAggregationsRepository
                    .Received(1)
                    .CreateDatasetAggregations(Arg.Is<DatasetAggregations>(
                        m => m.DatasetRelationshipId == relationshipId &&
                             m.SpecificationId == SpecificationId &&
                             m.Fields.Count() == 4 &&
                             m.Fields.ElementAt(0).Value == 3697 &&
                             m.Fields.ElementAt(0).FieldType == AggregatedType.Sum &&
                             m.Fields.ElementAt(0).FieldReference == "Datasets.RelationshipName.TestField_Sum" &&
                             m.Fields.ElementAt(1).Value == (decimal)924.25 &&
                             m.Fields.ElementAt(1).FieldType == AggregatedType.Average &&
                             m.Fields.ElementAt(1).FieldReference == "Datasets.RelationshipName.TestField_Average" &&
                             m.Fields.ElementAt(2).Value == 10 &&
                             m.Fields.ElementAt(2).FieldType == AggregatedType.Min &&
                             m.Fields.ElementAt(2).FieldReference == "Datasets.RelationshipName.TestField_Min" &&
                             m.Fields.ElementAt(3).Value == 3000 &&
                             m.Fields.ElementAt(3).FieldType == AggregatedType.Max &&
                             m.Fields.ElementAt(3).FieldReference == "Datasets.RelationshipName.TestField_Max"));

            await
                cacheProvider
                    .Received(1)
                    .RemoveAsync<List<CalculationAggregation>>(Arg.Is(dataset_aggregations_cache_key));
        }

        [TestMethod]
        async public Task ProcessDataset_GivenPayloadAndTableResultsWithMultipleProviderIds_DoesNotSaveDataset()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            string dataset_cache_key = $"ds-table-rows:{ProcessDatasetService.GetBlobNameCacheKey(blobPath)}:{DataDefintionId}";

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

            string json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";
            string relationshipName = "Relationship Name";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("specification-id", SpecificationId);
            message.UserProperties.Add("relationship-id", relationshipId);
            message.UserProperties.Add("jobId", "job1");

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

            ProcessDatasetService service = CreateProcessDatasetService(
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
                    .UpdateCurrentProviderSourceDatasets(Arg.Any<IEnumerable<ProviderSourceDataset>>());
        }

        [TestMethod]
        async public Task ProcessDataset_GivenPayloadAndTableResultsWithProviderIdsButNoExistingToCompare_SavesDatasetDoesntCallCreateVersionSavesVersion()
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

            string json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";
            string relationshipName = "Relationship Name";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("specification-id", SpecificationId);
            message.UserProperties.Add("relationship-id", relationshipId);
            message.UserProperties.Add("jobId", "job1");

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

            IVersionRepository<ProviderSourceDatasetVersion> versionRepository = CreateVersionRepository();

            ProcessDatasetService service = CreateProcessDatasetService(
                datasetRepository: datasetRepository,
                logger: logger,
                calcsRepository: calcsRepository,
                blobClient: blobClient,
                cacheProvider: cacheProvider,
                providerRepository: resultsRepository,
                providerResultsRepository: providerResultsRepository,
                excelDatasetReader: excelDatasetReader,
                versionRepository: versionRepository);

            // Act
            await service.ProcessDataset(message);

            // Assert
            await
                providerResultsRepository
                    .Received(1)
                    .UpdateCurrentProviderSourceDatasets(Arg.Is<IEnumerable<ProviderSourceDataset>>(
                        m => m.First().DataDefinition.Id == DataDefintionId &&
                             m.First().DataGranularity == DataGranularity.SingleRowPerProvider &&
                             m.First().DefinesScope == false &&
                             !string.IsNullOrWhiteSpace(m.First().Id) &&
                             m.First().SpecificationId == SpecificationId &&
                             m.First().ProviderId == "123"
                        ));

            await
            versionRepository
                .DidNotReceive()
                .CreateVersion(Arg.Any<ProviderSourceDatasetVersion>(), Arg.Any<ProviderSourceDatasetVersion>());

            await
                versionRepository
                    .Received(1)
                    .SaveVersions(Arg.Is<IEnumerable<ProviderSourceDatasetVersion>>(m =>
                        m.Count() == 1 &&
                        m.First().Author != null &&
                        m.First().Date.Date == DateTime.Now.Date &&
                        m.First().EntityId == $"{SpecificationId}_relId_123" &&
                        m.First().ProviderSourceDatasetId == $"{SpecificationId}_relId_123" &&
                        m.First().Id == $"{SpecificationId}_relId_123_version_1" &&
                        m.First().Rows.Count() == 1 &&
                        m.First().Version == 1
                    ));
        }

        [TestMethod]
        async public Task ProcessDataset_GivenPayloadAndTableResultsWithProviderIdsAndGetsExistingButNoChanges_DoesNotUpdate()
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
                },
                Id = DatasetId,
                Name = "ds-1"
            };

            string json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";
            string relationshipName = "Relationship Name";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("specification-id", SpecificationId);
            message.UserProperties.Add("relationship-id", relationshipId);
            message.UserProperties.Add("jobId", "job1");

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
                    Version = 1
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

            ProviderSourceDatasetVersion existingVersion = new ProviderSourceDatasetVersion
            {
                Rows = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>{ { "UPIN", "123456"} }
                },
                ProviderId = "123",
                Dataset = new VersionReference(DatasetId, "ds-1", 1)
            };

            IEnumerable<ProviderSourceDataset> existingCurrentDatasets = new[]
            {
                new ProviderSourceDataset
                {
                    ProviderId = "123",
                    Current = existingVersion
                }
            };
            ;
            providerResultsRepository
                .GetCurrentProviderSourceDatasets(Arg.Is(SpecificationId), Arg.Is("relId"))
                .Returns(existingCurrentDatasets);

            IVersionRepository<ProviderSourceDatasetVersion> versionRepository = CreateVersionRepository();

            ProcessDatasetService service = CreateProcessDatasetService(
                datasetRepository: datasetRepository,
                logger: logger,
                calcsRepository: calcsRepository,
                blobClient: blobClient,
                cacheProvider: cacheProvider,
                providerRepository: resultsRepository,
                providerResultsRepository: providerResultsRepository,
                excelDatasetReader: excelDatasetReader,
                versionRepository: versionRepository);

            // Act
            await service.ProcessDataset(message);

            // Assert
            await versionRepository
                .DidNotReceive()
                .CreateVersion(Arg.Any<ProviderSourceDatasetVersion>(), Arg.Any<ProviderSourceDatasetVersion>());

            logger
                .DidNotReceive()
                .Information(Arg.Is<string>(m => m.StartsWith("Saving")));
        }

        [TestMethod]
        async public Task ProcessDataset_GivenPayloadAndTableResultsWithProviderIdsAndChangesInData_CalsCreateNewVersionAndSaves()
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
                },
                Id = DatasetId,
                Name = "ds-1"
            };

            string json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";
            string relationshipName = "Relationship Name";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("specification-id", SpecificationId);
            message.UserProperties.Add("relationship-id", relationshipId);
            message.UserProperties.Add("jobId", "job1");
            message.UserProperties.Add("user-id", UserId);
            message.UserProperties.Add("user-name", Username);

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
                    Version = 1
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

            ProviderSourceDatasetVersion existingVersion = new ProviderSourceDatasetVersion
            {
                Rows = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>{ { "UPIN", "1234567"} }
                },
                ProviderId = "123",
                Dataset = new VersionReference(DatasetId, "ds-1", 1)
            };

            ProviderSourceDatasetVersion newVersion = new ProviderSourceDatasetVersion
            {
                Rows = new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>{ { "UPIN", "123456"} }
                },
                ProviderId = "123",
                Dataset = new VersionReference(DatasetId, "ds-1", 1),
                Author = new Reference(UserId, Username),
                Version = 2
            };

            IEnumerable<ProviderSourceDataset> existingCurrentDatasets = new[]
            {
                new ProviderSourceDataset
                {
                    ProviderId = "123",
                    Current = existingVersion
                }
            };
            ;
            providerResultsRepository
                .GetCurrentProviderSourceDatasets(Arg.Is(SpecificationId), Arg.Is("relId"))
                .Returns(existingCurrentDatasets);

            IVersionRepository<ProviderSourceDatasetVersion> versionRepository = CreateVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<ProviderSourceDatasetVersion>(), Arg.Is(existingVersion), Arg.Is("123"))
                .Returns(newVersion);

            ProcessDatasetService service = CreateProcessDatasetService(
                datasetRepository: datasetRepository,
                logger: logger,
                calcsRepository: calcsRepository,
                blobClient: blobClient,
                cacheProvider: cacheProvider,
                providerRepository: resultsRepository,
                providerResultsRepository: providerResultsRepository,
                excelDatasetReader: excelDatasetReader,
                versionRepository: versionRepository);

            // Act
            await service.ProcessDataset(message);

            // Assert
            await versionRepository
                .Received(1)
                .CreateVersion(Arg.Any<ProviderSourceDatasetVersion>(), Arg.Any<ProviderSourceDatasetVersion>(), Arg.Is("123"));

            await
                versionRepository
                    .Received(1)
                    .SaveVersions(Arg.Is<IEnumerable<ProviderSourceDatasetVersion>>(m => m.First() == newVersion));

            logger
                .Received(1)
                .Information(Arg.Is("Saving 1 updated source datasets"));

            logger
                .Received(1)
                .Information(Arg.Is("Saving 1 items to history"));
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadAndTableResultsWithProviderIds_EnsuresCreatesNewJob()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            string dataset_cache_key = $"ds-table-rows:{blobPath}:{DataDefintionId}";

            string dataset_aggregations_cache_key = $"{CacheKeys.DatasetAggregationsForSpecification}{SpecificationId}";

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

            string json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";
            string relationshipName = "Relationship Name";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("specification-id", SpecificationId);
            message.UserProperties.Add("relationship-id", relationshipId);
            message.UserProperties.Add("jobId", "job1");
            message.UserProperties.Add("user-id", UserId);
            message.UserProperties.Add("user-name", Username);

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
                        DefinesScope = true
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

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1", JobDefinitionId = JobConstants.DefinitionNames.CreateInstructAllocationJob });

            ProcessDatasetService service = CreateProcessDatasetService(
                datasetRepository: datasetRepository,
                logger: logger,
                calcsRepository: calcsRepository,
                blobClient: blobClient,
                cacheProvider: cacheProvider,
                providerRepository: resultsRepository,
                providerResultsRepository: providerResultsRepository,
                excelDatasetReader: excelDatasetReader,
                jobsApiClient: jobsApiClient);

            // Act
            await service.ProcessDataset(message);

            // Assert
            await
                 jobsApiClient
                     .Received(1)
                     .CreateJob(Arg.Is<JobCreateModel>(
                         m =>
                             m.InvokerUserDisplayName == Username &&
                             m.InvokerUserId == UserId &&
                             m.JobDefinitionId == JobConstants.DefinitionNames.CreateInstructAllocationJob &&
                             m.Properties["specification-id"] == SpecificationId &&
                             m.Properties["provider-cache-key"] == $"{CacheKeys.ScopedProviderSummariesPrefix}{SpecificationId}" &&
                             m.Trigger.EntityId == relationshipId &&
                             m.Trigger.EntityType == nameof(DefinitionSpecificationRelationship) &&
                             m.Trigger.Message == $"Processed dataset relationship: '{relationshipId}' for specification: '{SpecificationId}'"
                         ));

            logger
                .Received(1)
                .Information(Arg.Is($"New job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' created with id: 'job-id-1'"));
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadAndTableResultsWithProviderIdsAndJobServiceFeatureIsOnAndCalcsIncludeAggregatedCals_EnsuresCreatesNewGenerateAggregationsJob()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            string dataset_cache_key = $"ds-table-rows:{blobPath}:{DataDefintionId}";

            string dataset_aggregations_cache_key = $"{CacheKeys.DatasetAggregationsForSpecification}{SpecificationId}";

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

            string json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";
            string relationshipName = "Relationship Name";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("specification-id", SpecificationId);
            message.UserProperties.Add("relationship-id", relationshipId);
            message.UserProperties.Add("jobId", "job1");
            message.UserProperties.Add("user-id", UserId);
            message.UserProperties.Add("user-name", Username);

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
                        DefinesScope = true
                    }
                },
                SpecificationId = SpecificationId,
            };

            IEnumerable<CalculationCurrentVersion> calculations = new[]
            {
                new CalculationCurrentVersion
                {
                     SourceCode = "return Sum(Calc1)"
                }
            };

            ICalcsRepository calcsRepository = CreateCalcsRepository();
            calcsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns(buildProject);

            calcsRepository
                .GetCurrentCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

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

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-1", JobDefinitionId = JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob });

            ProcessDatasetService service = CreateProcessDatasetService(
                datasetRepository: datasetRepository,
                logger: logger,
                calcsRepository: calcsRepository,
                blobClient: blobClient,
                cacheProvider: cacheProvider,
                providerRepository: resultsRepository,
                providerResultsRepository: providerResultsRepository,
                excelDatasetReader: excelDatasetReader,
                jobsApiClient: jobsApiClient);

            // Act
            await service.ProcessDataset(message);

            // Assert
            await
                 jobsApiClient
                     .Received(1)
                     .CreateJob(Arg.Is<JobCreateModel>(
                         m =>
                             m.InvokerUserDisplayName == Username &&
                             m.InvokerUserId == UserId &&
                             m.JobDefinitionId == JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob &&
                             m.Properties["specification-id"] == SpecificationId &&
                             m.Properties["provider-cache-key"] == $"{CacheKeys.ScopedProviderSummariesPrefix}{SpecificationId}" &&
                             m.Trigger.EntityId == relationshipId &&
                             m.Trigger.EntityType == nameof(DefinitionSpecificationRelationship) &&
                             m.Trigger.Message == $"Processed dataset relationship: '{relationshipId}' for specification: '{SpecificationId}'"
                         ));

            logger
                .Received(1)
                .Information(Arg.Is($"New job of type '{JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob}' created with id: 'job-id-1'"));
        }

        [TestMethod]
        public async Task ProcessDataset_GivenPayloadAndTableResultsWithProviderIdsButCreatingJobReturnsNull_LogsErrorAndThrowsException()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            string dataset_cache_key = $"ds-table-rows:{blobPath}:{DataDefintionId}";

            string dataset_aggregations_cache_key = $"{CacheKeys.DatasetAggregationsForSpecification}{SpecificationId}";

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

            string json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";
            string relationshipName = "Relationship Name";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("specification-id", SpecificationId);
            message.UserProperties.Add("relationship-id", relationshipId);
            message.UserProperties.Add("jobId", "job1");
            message.UserProperties.Add("user-id", UserId);
            message.UserProperties.Add("user-name", Username);

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
                        DefinesScope = true
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

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns((Job)null);

            ProcessDatasetService service = CreateProcessDatasetService(
                datasetRepository: datasetRepository,
                logger: logger,
                calcsRepository: calcsRepository,
                blobClient: blobClient,
                cacheProvider: cacheProvider,
                providerRepository: resultsRepository,
                providerResultsRepository: providerResultsRepository,
                excelDatasetReader: excelDatasetReader,
                jobsApiClient: jobsApiClient);

            // Act
            Func<Task> test = async () => await service.ProcessDataset(message);

            // Assert
            test
                .Should().ThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be($"Failed to create job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' on specification '{SpecificationId}'");

            await
                 jobsApiClient
                     .Received(1)
                     .CreateJob(Arg.Is<JobCreateModel>(
                         m =>
                             m.InvokerUserDisplayName == Username &&
                             m.InvokerUserId == UserId &&
                             m.JobDefinitionId == JobConstants.DefinitionNames.CreateInstructAllocationJob &&
                             m.Properties["specification-id"] == SpecificationId &&
                             m.Properties["provider-cache-key"] == $"{CacheKeys.ScopedProviderSummariesPrefix}{SpecificationId}" &&
                             m.Trigger.EntityId == relationshipId &&
                             m.Trigger.EntityType == nameof(DefinitionSpecificationRelationship) &&
                             m.Trigger.Message == $"Processed dataset relationship: '{relationshipId}' for specification: '{SpecificationId}'"
                         ));

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is($"Failed to create job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' on specification '{SpecificationId}'"));

        }

        [TestMethod]
        public async Task ProcessDataset_GivenRunningAsAJob_ThenUpdateJobStatus()
        {
            //Arrange
            const string blobPath = "dataset-id/v1/ds.xlsx";

            string dataset_cache_key = $"ds-table-rows:{blobPath}:{DataDefintionId}";

            string dataset_aggregations_cache_key = $"{CacheKeys.DatasetAggregationsForSpecification}{SpecificationId}";

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

            string json = JsonConvert.SerializeObject(dataset);

            string relationshipId = "relId";
            string relationshipName = "Relationship Name";
            string jobId = "jobId1";

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message.UserProperties.Add("specification-id", SpecificationId);
            message.UserProperties.Add("relationship-id", relationshipId);
            message.UserProperties.Add("jobId", jobId);
            message.UserProperties.Add("user-id", UserId);
            message.UserProperties.Add("user-name", Username);

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
                        DefinesScope = true
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

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .CreateJob(Arg.Any<JobCreateModel>())
                .Returns(new Job { Id = "job-id-2", JobDefinitionId = JobConstants.DefinitionNames.CreateInstructAllocationJob });

            ProcessDatasetService service = CreateProcessDatasetService(
                datasetRepository: datasetRepository,
                logger: logger,
                calcsRepository: calcsRepository,
                blobClient: blobClient,
                cacheProvider: cacheProvider,
                providerRepository: resultsRepository,
                providerResultsRepository: providerResultsRepository,
                excelDatasetReader: excelDatasetReader,
                jobsApiClient: jobsApiClient);

            // Act
            await service.ProcessDataset(message);

            // Assert
            await
                jobsApiClient
                    .Received(1)
                    .AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(l => l.CompletedSuccessfully == null && l.ItemsProcessed == 0 && string.IsNullOrEmpty(l.Outcome)));

            await
                 jobsApiClient
                     .Received(1)
                     .AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(l => l.CompletedSuccessfully == true && l.ItemsProcessed == 100 && l.Outcome == "Processed Dataset"));
        }
    }
}
