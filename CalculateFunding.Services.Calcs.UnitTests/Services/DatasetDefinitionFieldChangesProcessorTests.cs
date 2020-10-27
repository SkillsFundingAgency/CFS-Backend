using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core;
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
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Common.ApiClient.DataSets;
using CalculateFunding.Common.ApiClient.Models;
using System.Net;
using AutoMapper;
using CalculateFunding.Services.Calcs.MappingProfiles;

namespace CalculateFunding.Services.Calcs.Services
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
            await processor.Run(message);

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
            Func<Task> test = async () => await processor.Run(message);

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
            await processor.Run(message);

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
            await processor.Run(message);

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

            IDatasetsApiClient datasetRepository = CreateDatasetRepository();
            datasetRepository
                 .GetSpecificationIdsForRelationshipDefinitionId(Arg.Is(definitionId))               
                 .Returns(new ApiResponse<IEnumerable<string>>(HttpStatusCode.OK, Enumerable.Empty<string>()));

            DatasetDefinitionFieldChangesProcessor processor = CreateProcessor(logger: logger, datasetRepository: datasetRepository);

            //Act
            await processor.Run(message);

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

            IDatasetsApiClient datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetSpecificationIdsForRelationshipDefinitionId(Arg.Is(definitionId))
                .Returns(new ApiResponse<IEnumerable<string>>(HttpStatusCode.OK, relationshipSpecificationIds));
            datasetRepository
                 .GetCurrentRelationshipsBySpecificationIdAndDatasetDefinitionId(Arg.Is(specificationId), Arg.Is(definitionId))
                 .Returns(new ApiResponse<IEnumerable<Common.ApiClient.DataSets.Models.DatasetSpecificationRelationshipViewModel>>(HttpStatusCode.OK, Enumerable.Empty<Common.ApiClient.DataSets.Models.DatasetSpecificationRelationshipViewModel>()));


            DatasetDefinitionFieldChangesProcessor processor = CreateProcessor(logger: logger, datasetRepository: datasetRepository);

            //Act
            Func<Task> test = async () => await processor.Run(message);

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

            IEnumerable<Common.ApiClient.DataSets.Models.DatasetSpecificationRelationshipViewModel> relationshipViewModels = new[]
              {
                new Common.ApiClient.DataSets.Models.DatasetSpecificationRelationshipViewModel()
            };

            IEnumerable<DatasetSpecificationRelationshipViewModel> relationshipViewModel = new[]
           {
                new DatasetSpecificationRelationshipViewModel()
            };

            IDatasetsApiClient datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetSpecificationIdsForRelationshipDefinitionId(Arg.Is(definitionId))
                .Returns(new ApiResponse<IEnumerable<string>>(HttpStatusCode.OK, relationshipSpecificationIds));
            datasetRepository
                .GetCurrentRelationshipsBySpecificationIdAndDatasetDefinitionId(Arg.Is(specificationId), Arg.Is(definitionId))
                .Returns(new ApiResponse<IEnumerable<Common.ApiClient.DataSets.Models.DatasetSpecificationRelationshipViewModel>>(HttpStatusCode.OK, relationshipViewModels));


            ICalculationService calculationService = CreateCalculationService();

            DatasetDefinitionFieldChangesProcessor processor = CreateProcessor(
                logger: logger,
                datasetRepository: datasetRepository,
                calculationService: calculationService);

            //Act
            await processor.Run(message);

            //Assert
            await
                calculationService
                .Received(1)
                .ResetCalculationForFieldDefinitionChanges(Arg.Is<IEnumerable<Models.Datasets.ViewModels.DatasetSpecificationRelationshipViewModel>>(_ => _.First().Id == relationshipViewModel.First().Id), Arg.Is(specificationId), Arg.Is<IEnumerable<string>>(m => m.First() == "test field 1"));
        }

        [TestMethod]
        [DataRow(FieldDefinitionChangeType.IsAggregable)]
        [DataRow(FieldDefinitionChangeType.IsNotAggregable)]
        public async Task ProcessChanges_GivenChangeModelWithAggregableFieldChanges_CallsResetCalculationForFieldDefinitionChanges(FieldDefinitionChangeType fieldDefinitionChangeType)
        {
            //Arrange
            string definitionId = "df-id-1";
            string specificationId = "spec-1";

            IEnumerable<Calculation> calculations = new[]
            {
                new Calculation
                {
                    Current = new CalculationVersion
                    {
                        SourceCode = "return Sum(Datasets.TestRelationship.TestField1)"
                    }
                }
            };

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

            IEnumerable<Common.ApiClient.DataSets.Models.DatasetSpecificationRelationshipViewModel> relationshipViewModels = new[]
             {
                new Common.ApiClient.DataSets.Models.DatasetSpecificationRelationshipViewModel
                {
                    Name = "Test Relationship"
                }
            };

            IEnumerable<DatasetSpecificationRelationshipViewModel> relationshipViewModel = new[]
           {
                new DatasetSpecificationRelationshipViewModel
                {
                    Name = "Test Relationship"
                }
            };

            IDatasetsApiClient datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetSpecificationIdsForRelationshipDefinitionId(Arg.Is(definitionId))
                .Returns(new ApiResponse<IEnumerable<string>>(HttpStatusCode.OK, relationshipSpecificationIds));
            datasetRepository
                 .GetCurrentRelationshipsBySpecificationIdAndDatasetDefinitionId(Arg.Is(specificationId), Arg.Is(definitionId))
                 .Returns(new ApiResponse<IEnumerable<Common.ApiClient.DataSets.Models.DatasetSpecificationRelationshipViewModel>>(HttpStatusCode.OK, relationshipViewModels));


            ICalculationService calculationService = CreateCalculationService();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calculations);

            IMapper mapper = CreateMapper();

            DatasetDefinitionFieldChangesProcessor processor = CreateProcessor(
                logger: logger,
                datasetRepository: datasetRepository,
                calculationService: calculationService,
                calculationsRepository: calculationsRepository,
                mapper: mapper);

            //Act
            await processor.Run(message);

            //Assert
            await
                calculationService
                .Received(1)
                .ResetCalculationForFieldDefinitionChanges(Arg.Is<IEnumerable<Models.Datasets.ViewModels.DatasetSpecificationRelationshipViewModel>>(_ => _.First().Id == relationshipViewModel.First().Id), Arg.Is(specificationId), Arg.Is<IEnumerable<string>>(m => m.First() == "test field 1"));
        }

        [TestMethod]
        [DataRow(FieldDefinitionChangeType.IsAggregable)]
        [DataRow(FieldDefinitionChangeType.IsNotAggregable)]
        public async Task ProcessChanges_GivenChangeModelWithAggregableFieldChangesButNoAggregateParanetersFound_DoesNotResetCalculationForFieldDefinitionChanges(FieldDefinitionChangeType fieldDefinitionChangeType)
        {
            //Arrange
            string definitionId = "df-id-1";
            string specificationId = "spec-1";

            IEnumerable<Calculation> calculations = new[]
            {
                new Calculation
                {
                    Current = new CalculationVersion
                    {
                        SourceCode = "return 2 + Datasets.TestRelationship.TestField1"
                    }
                }
            };

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

            IEnumerable<Common.ApiClient.DataSets.Models.DatasetSpecificationRelationshipViewModel> relationshipViewModels = new[]
             {
                new Common.ApiClient.DataSets.Models.DatasetSpecificationRelationshipViewModel
                {
                    Name = "Test Relationship"
                }
            };
          

            IDatasetsApiClient datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetSpecificationIdsForRelationshipDefinitionId(Arg.Is(definitionId))
                .Returns(new ApiResponse<IEnumerable<string>>(HttpStatusCode.OK, relationshipSpecificationIds));
            datasetRepository
                 .GetCurrentRelationshipsBySpecificationIdAndDatasetDefinitionId(Arg.Is(specificationId), Arg.Is(definitionId))
                 .Returns(new ApiResponse<IEnumerable<Common.ApiClient.DataSets.Models.DatasetSpecificationRelationshipViewModel>>(HttpStatusCode.OK, relationshipViewModels));

            ICalculationService calculationService = CreateCalculationService();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(calculations);

            DatasetDefinitionFieldChangesProcessor processor = CreateProcessor(
                logger: logger,
                datasetRepository: datasetRepository,
                calculationService: calculationService,
                calculationsRepository: calculationsRepository);

            //Act
            await processor.Run(message);

            //Assert
            await
                calculationService
                .DidNotReceive()
                .ResetCalculationForFieldDefinitionChanges(Arg.Any<IEnumerable<DatasetSpecificationRelationshipViewModel>>(), Arg.Any<string>(), Arg.Any<IEnumerable<string>>());
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

            IEnumerable<Common.ApiClient.DataSets.Models.DatasetSpecificationRelationshipViewModel> relationshipViewModels = new[]
              {
                new Common.ApiClient.DataSets.Models.DatasetSpecificationRelationshipViewModel()
            };

            IEnumerable<DatasetSpecificationRelationshipViewModel> relationshipViewModel = new[]
            {
                new DatasetSpecificationRelationshipViewModel()
            };

            IDatasetsApiClient datasetRepository = CreateDatasetRepository();
            
            datasetRepository
                .GetSpecificationIdsForRelationshipDefinitionId(Arg.Is(definitionId))
                .Returns(new ApiResponse<IEnumerable<string>>(HttpStatusCode.OK, relationshipSpecificationIds));

            datasetRepository
                 .GetCurrentRelationshipsBySpecificationIdAndDatasetDefinitionId(Arg.Is(specificationId), Arg.Is(definitionId))
                 .Returns(new ApiResponse<IEnumerable<Common.ApiClient.DataSets.Models.DatasetSpecificationRelationshipViewModel>>(HttpStatusCode.OK, relationshipViewModels));


            ICalculationService calculationService = CreateCalculationService();

            DatasetDefinitionFieldChangesProcessor processor = CreateProcessor(
                logger: logger,
                datasetRepository: datasetRepository,
                calculationService: calculationService);

            //Act
            await processor.Run(message);

            //Assert
            await
                calculationService
                .Received(1)
                .ResetCalculationForFieldDefinitionChanges(Arg.Is<IEnumerable<Models.Datasets.ViewModels.DatasetSpecificationRelationshipViewModel>>(_ => _.First().Id == relationshipViewModel.First().Id),
                Arg.Is(specificationId), 
                    Arg.Is<IEnumerable<string>>(
                        m => 
                        m.Count() == 2 && 
                        m.ElementAt(0) == "test field 1" && 
                        m.ElementAt(1) == "test field 2"));
        }

        private static DatasetDefinitionFieldChangesProcessor CreateProcessor(
            IFeatureToggle featureToggle = null,
            IDatasetsApiClient datasetRepository = null,
            ILogger logger = null,
            ICalculationService calculationService = null,
            ICalculationsRepository calculationsRepository = null,
            IMapper mapper = null)
        {
            return new DatasetDefinitionFieldChangesProcessor(
                featureToggle ?? CreateFeatureToggle(),
                datasetRepository ?? CreateDatasetRepository(),
                CalcsResilienceTestHelper.GenerateTestPolicies(),
                logger ?? CreateLogger(),
                calculationService ?? CreateCalculationService(),
                calculationsRepository ?? CreateCalculationsRepository(),
                mapper ?? CreateMapper());
        }

        private static IFeatureToggle CreateFeatureToggle(bool featureToggleOn = true)
        {
            IFeatureToggle featureToggle = Substitute.For<IFeatureToggle>();
            featureToggle
                .IsProcessDatasetDefinitionFieldChangesEnabled()
                .Returns(featureToggleOn);

            return featureToggle;
        }

        private static IDatasetsApiClient CreateDatasetRepository()
        {
            return Substitute.For<IDatasetsApiClient>();
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static ICalculationService CreateCalculationService()
        {
            return Substitute.For<ICalculationService>();
        }

        public static ICalculationsRepository CreateCalculationsRepository()
        {
            return Substitute.For<ICalculationsRepository>();
        }

        private static IMapper CreateMapper()
        {
            MapperConfiguration mapperConfig = new MapperConfiguration(c =>
            {
                c.AddProfile<CalculationsMappingProfile>();
            });

            return mapperConfig.CreateMapper();
        }
    }
}
