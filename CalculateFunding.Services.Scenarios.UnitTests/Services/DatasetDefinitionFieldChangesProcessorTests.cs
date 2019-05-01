using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Scenarios.Interfaces;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Scenarios.Services
{
    [TestClass]
    public class DatasetDefinitionFieldChangesProcessorTests
    {
        [TestMethod]
        public async Task ProcessChanges_GivenFeatureToggleIsOff_DoesNotProcess()
        {
            //Arrange
            Message message = new Message();

            IFeatureToggle featureToggle = CreateFeatureToggle(false);

            ILogger logger = CreateLogger();

            DatasetDefinitionFieldChangesProcessor processor = CreateProcessor(featureToggle, logger: logger);

            //Act
            await processor.ProcessChanges(message);

            //Assert
            logger
                .DidNotReceive()
                .Information(Arg.Is("Checking for dataset definition changes before proceeding"));
        }

        [TestMethod]
        public void ProcessChanges_GivenNullDefinitionChanges_ThrowsNonRetriableException()
        {
            //Arrange
            Message message = new Message();

            DatasetDefinitionFieldChangesProcessor processor = CreateProcessor();

            //Act
            Func<Task> test = async () => await processor.ProcessChanges(message);

            //Assert
            test
                .Should()
                .ThrowExactly<NonRetriableException>()
                .Which
                .Message
                .Should()
                .Be("Message does not contain a valid dataset definition change model");
        }

        [TestMethod]
        public async Task ProcessChanges_GivenChangeModelButHasNoChanges_LogsAndReturns()
        {
            //Arrange
            string definitionId = "df-id-1";

            DatasetDefinitionChanges datasetDefinitionChanges = new DatasetDefinitionChanges
            {
                Id = definitionId
            };

            string json = JsonConvert.SerializeObject(datasetDefinitionChanges);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            DatasetDefinitionFieldChangesProcessor processor = CreateProcessor(logger: logger);

            //Act
            await processor.ProcessChanges(message);

            //Assert
            logger
                .Received(1)
                .Information(Arg.Is($"No dataset definition field changes for definition id '{definitionId}'"));
        }

        [TestMethod]
        public async Task ProcessChanges_GivenChangeModelButHasNoFieldChanges_LogsAndReturns()
        {
            //Arrange
            string definitionId = "df-id-1";

            DatasetDefinitionChanges datasetDefinitionChanges = new DatasetDefinitionChanges
            {
                Id = definitionId,
            };

            datasetDefinitionChanges.DefinitionChanges.Add(DefinitionChangeType.DefinitionName);

            string json = JsonConvert.SerializeObject(datasetDefinitionChanges);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            DatasetDefinitionFieldChangesProcessor processor = CreateProcessor(logger: logger);

            //Act
            await processor.ProcessChanges(message);

            //Assert
            logger
                .Received(1)
                .Information(Arg.Is($"No dataset definition field changes for definition id '{definitionId}'"));
        }

        [TestMethod]
        public async Task ProcessChanges_GivenChangeModelWithFieldChangesButNoRelationshipsExist_LogsAndReturns()
        {
            //Arrange
            string definitionId = "df-id-1";

            DatasetDefinitionChanges datasetDefinitionChanges = new DatasetDefinitionChanges
            {
                Id = definitionId,
            };

            FieldDefinitionChanges fieldDefinitionChanges = new FieldDefinitionChanges();
            fieldDefinitionChanges.ChangeTypes.Add(FieldDefinitionChangeType.FieldName);

            TableDefinitionChanges tableDefinitionChanges = new TableDefinitionChanges();
            tableDefinitionChanges.FieldChanges.Add(fieldDefinitionChanges);

            datasetDefinitionChanges.TableDefinitionChanges.Add(tableDefinitionChanges);

            string json = JsonConvert.SerializeObject(datasetDefinitionChanges);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetRelationshipSpecificationIdsByDatasetDefinitionId(Arg.Is(definitionId))
                .Returns(Enumerable.Empty<string>());

            DatasetDefinitionFieldChangesProcessor processor = CreateProcessor(logger: logger, datasetRepository: datasetRepository);

            //Act
            await processor.ProcessChanges(message);

            //Assert
            logger
                .Received(1)
                .Information(Arg.Is($"No dataset definition specification relationships exists for definition id '{definitionId}'"));
        }

        [TestMethod]
        public void ProcessChanges_GivenChangeModelWithFieldNameChangesButNoRelationshipsFound_ThrowsRetriableException()
        {
            //Arrange
            string definitionId = "df-id-1";
            string specificationId = "spec-1";

            DatasetDefinitionChanges datasetDefinitionChanges = new DatasetDefinitionChanges
            {
                Id = definitionId,
            };

            FieldDefinitionChanges fieldDefinitionChanges = new FieldDefinitionChanges
            {
                FieldDefinition = new FieldDefinition
                {
                    Id = "field1"
                }
            };

            fieldDefinitionChanges.ChangeTypes.Add(FieldDefinitionChangeType.FieldName);

            TableDefinitionChanges tableDefinitionChanges = new TableDefinitionChanges();
            tableDefinitionChanges.FieldChanges.Add(fieldDefinitionChanges);

            datasetDefinitionChanges.TableDefinitionChanges.Add(tableDefinitionChanges);

            string json = JsonConvert.SerializeObject(datasetDefinitionChanges);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            IEnumerable<string> relationshipSpecificationIds = new[] { specificationId };

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetRelationshipSpecificationIdsByDatasetDefinitionId(Arg.Is(definitionId))
                .Returns(relationshipSpecificationIds);
            datasetRepository
                .GetCurrentRelationshipsBySpecificationIdAndDatasetDefinitionId(Arg.Is(specificationId), Arg.Is(definitionId))
                .Returns(Enumerable.Empty<DatasetSpecificationRelationshipViewModel>());

            DatasetDefinitionFieldChangesProcessor processor = CreateProcessor(logger: logger, datasetRepository: datasetRepository);

            //Act
            Func<Task> test = async () => await processor.ProcessChanges(message);

            //Assert
            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be($"No relationships found for specificationId '{specificationId}' and dataset definition id '{definitionId}'");
        }

        [TestMethod]
        [DataRow(FieldDefinitionChangeType.IsAggregable)]
        [DataRow(FieldDefinitionChangeType.IsNotAggregable)]
        [DataRow(FieldDefinitionChangeType.FieldType)]
        [DataRow(FieldDefinitionChangeType.FieldName)]
        public async Task ProcessChanges_GivenChangeModelWithFieldChanges_CallsResetCalculationForFieldDefinitionChanges(FieldDefinitionChangeType fieldDefinitionChangeType)
        {
            //Arrange
            string definitionId = "df-id-1";
            string specificationId = "spec-1";

            DatasetDefinitionChanges datasetDefinitionChanges = new DatasetDefinitionChanges
            {
                Id = definitionId,
            };

            FieldDefinitionChanges fieldDefinitionChanges = new FieldDefinitionChanges
            {
                FieldDefinition = new FieldDefinition
                {
                    Id = "field1"
                },
                ExistingFieldDefinition = new FieldDefinition { Name = "test field 1" }
            };

            fieldDefinitionChanges.ChangeTypes.Add(fieldDefinitionChangeType);

            TableDefinitionChanges tableDefinitionChanges = new TableDefinitionChanges();
            tableDefinitionChanges.FieldChanges.Add(fieldDefinitionChanges);

            datasetDefinitionChanges.TableDefinitionChanges.Add(tableDefinitionChanges);

            string json = JsonConvert.SerializeObject(datasetDefinitionChanges);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            IEnumerable<string> relationshipSpecificationIds = new[] { specificationId };

            IEnumerable<DatasetSpecificationRelationshipViewModel> relationshipViewModels = new[]
            {
                new DatasetSpecificationRelationshipViewModel()
            };

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetRelationshipSpecificationIdsByDatasetDefinitionId(Arg.Is(definitionId))
                .Returns(relationshipSpecificationIds);
            datasetRepository
                .GetCurrentRelationshipsBySpecificationIdAndDatasetDefinitionId(Arg.Is(specificationId), Arg.Is(definitionId))
                .Returns(relationshipViewModels);

            IScenariosService scenariosService = CreateScenariosService();

            DatasetDefinitionFieldChangesProcessor processor = CreateProcessor(
                logger: logger,
                datasetRepository: datasetRepository,
                scenariosService: scenariosService);

            //Act
            await processor.ProcessChanges(message);

            //Assert
            await
                scenariosService
                .Received(1)
                .ResetScenarioForFieldDefinitionChanges(Arg.Is(relationshipViewModels), Arg.Is(specificationId), Arg.Is<IEnumerable<string>>(m => m.First() == "test field 1"));
        }

        [TestMethod]
        public async Task ProcessChanges_GivenChangeModelWithMultipleFieldNameChanges_CallsResetCalculationForFieldDefinitionChanges()
        {
            //Arrange
            string definitionId = "df-id-1";
            string specificationId = "spec-1";

            DatasetDefinitionChanges datasetDefinitionChanges = new DatasetDefinitionChanges
            {
                Id = definitionId,
            };

            FieldDefinitionChanges fieldDefinitionChanges1 = new FieldDefinitionChanges
            {
                FieldDefinition = new FieldDefinition
                {
                    Id = "field1"
                },
                ExistingFieldDefinition = new FieldDefinition { Name = "test field 1" }
            };

            FieldDefinitionChanges fieldDefinitionChanges2 = new FieldDefinitionChanges
            {
                FieldDefinition = new FieldDefinition
                {
                    Id = "field2"
                },
                ExistingFieldDefinition = new FieldDefinition { Name = "test field 2" }
            };

            fieldDefinitionChanges1.ChangeTypes.Add(FieldDefinitionChangeType.FieldName);
            fieldDefinitionChanges2.ChangeTypes.Add(FieldDefinitionChangeType.FieldName);

            TableDefinitionChanges tableDefinitionChanges = new TableDefinitionChanges();
            tableDefinitionChanges.FieldChanges.AddRange(new[] { fieldDefinitionChanges1, fieldDefinitionChanges2 });

            datasetDefinitionChanges.TableDefinitionChanges.Add(tableDefinitionChanges);

            string json = JsonConvert.SerializeObject(datasetDefinitionChanges);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            IEnumerable<string> relationshipSpecificationIds = new[] { specificationId };

            IEnumerable<DatasetSpecificationRelationshipViewModel> relationshipViewModels = new[]
            {
                new DatasetSpecificationRelationshipViewModel()
            };

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetRelationshipSpecificationIdsByDatasetDefinitionId(Arg.Is(definitionId))
                .Returns(relationshipSpecificationIds);
            datasetRepository
                .GetCurrentRelationshipsBySpecificationIdAndDatasetDefinitionId(Arg.Is(specificationId), Arg.Is(definitionId))
                .Returns(relationshipViewModels);

            IScenariosService scenariosService = CreateScenariosService();

            DatasetDefinitionFieldChangesProcessor processor = CreateProcessor(
                logger: logger,
                datasetRepository: datasetRepository,
                scenariosService: scenariosService);

            //Act
            await processor.ProcessChanges(message);

            //Assert
            await
                scenariosService
                .Received(1)
                .ResetScenarioForFieldDefinitionChanges(Arg.Is(relationshipViewModels), Arg.Is(specificationId), 
                    Arg.Is<IEnumerable<string>>(
                        m => 
                        m.Count() == 2 && 
                        m.ElementAt(0) == "test field 1" && 
                        m.ElementAt(1) == "test field 2"));
        }

        private static DatasetDefinitionFieldChangesProcessor CreateProcessor(
            IFeatureToggle featureToggle = null,
            IDatasetRepository datasetRepository = null,
            ILogger logger = null,
            IScenariosService scenariosService = null)
        {
            return new DatasetDefinitionFieldChangesProcessor(
                featureToggle ?? CreateFeatureToggle(),
                logger ?? CreateLogger(),
                datasetRepository ?? CreateDatasetRepository(),
                ScenariosResilienceTestHelper.GenerateTestPolicies(),
                scenariosService ?? CreateScenariosService());
        }

        private static IFeatureToggle CreateFeatureToggle(bool featureToggleOn = true)
        {
            IFeatureToggle featureToggle = Substitute.For<IFeatureToggle>();
            featureToggle
                .IsProcessDatasetDefinitionFieldChangesEnabled()
                .Returns(featureToggleOn);

            return featureToggle;
        }

        private static IDatasetRepository CreateDatasetRepository()
        {
            return Substitute.For<IDatasetRepository>();
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static IScenariosService CreateScenariosService()
        {
            return Substitute.For<IScenariosService>();
        }
    }
}
