using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Core.Interfaces.Caching;
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
    public class DatasetsServiceProcessDatasetTests : DatasetServiceTestsBase
    {
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

            IVersionRepository<ProviderSourceDatasetVersion> versionRepository = CreateVersionRepository();

            DatasetService service = CreateDatasetService(
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
        async public Task ProcessDataset_GivenPayloadAndTableResultsWithProviderIdsAndGetsExistingButNoChanges_DoesnotUpdate()
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

            DatasetService service = CreateDatasetService(
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

            DatasetService service = CreateDatasetService(
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
    }
}
