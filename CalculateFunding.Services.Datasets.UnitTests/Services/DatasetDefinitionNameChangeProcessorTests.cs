using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Datasets.Interfaces;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using System;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.FeatureToggles;

namespace CalculateFunding.Services.Datasets.Services
{
    [TestClass]
    public class DatasetDefinitionNameChangeProcessorTests
    {
        [TestMethod]
        public async Task ProcessChange_GivenFeatureToggleSwitchOff_DoesNotProcess()
        {
            //Arrange
            Message message = new Message();

            ILogger logger = CreateLogger();

            IFeatureToggle featureToggle = CreateFeatureToggle(false);

            DatasetDefinitionNameChangeProcessor processor = CreateProcessor(logger: logger, featureToggle: featureToggle);

            //Act
            await processor.ProcessChanges(message);

            //Assert
            logger
                 .DidNotReceive()
                 .Information(Arg.Is("Checking for changes before proceeding"));
        }

        [TestMethod]
        public void ProcessChange_GivenANullModel_ThrowsException()
        {
            //Arrange
            Message message = new Message();

            DatasetDefinitionNameChangeProcessor processor = CreateProcessor();

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
        public async Task ProcessChange_GivenMessageWithNoDefinitionChanges_LogsAndReturns()
        {
            //Arrange
            const string definitionId = "123456";

            DatasetDefinitionChanges datasetDefinitionChanges = new DatasetDefinitionChanges
            {
                Id = definitionId
            };

            string json = JsonConvert.SerializeObject(datasetDefinitionChanges);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            DatasetDefinitionNameChangeProcessor processor = CreateProcessor(logger: logger);

            //Act
            await processor.ProcessChanges(message);

            //Assert
            logger
                 .Received(1)
                 .Information(Arg.Is($"No dataset definition name change for definition id '{definitionId}'"));
        }

        [TestMethod]
        public async Task ProcessChange_GivenMessageWithDefinitionChanges_CallsServices()
        {
            //Arrange
            const string definitionId = "123456";
            const string definitionName = "New def name";

            DatasetDefinitionChanges datasetDefinitionChanges = new DatasetDefinitionChanges
            {
                Id = definitionId,
                NewName = definitionName
            };

            datasetDefinitionChanges.DefinitionChanges.Add(DefinitionChangeType.DefinitionName);

            string json = JsonConvert.SerializeObject(datasetDefinitionChanges);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            IDefinitionSpecificationRelationshipService definitionSpecificationRelationshipService = CreateDefinitionSpecificationRelationshipService();

            IDatasetService datasetService = CreateDataService();

            DatasetDefinitionNameChangeProcessor processor = CreateProcessor(definitionSpecificationRelationshipService, datasetService, logger);

            //Act
            await processor.ProcessChanges(message);

            //Assert
            await
                definitionSpecificationRelationshipService
                    .Received(1)
                    .UpdateRelationshipDatasetDefinitionName(Arg.Is<Reference>(m => m.Id == definitionId && m.Name == definitionName));

            await
                datasetService
                    .Received(1)
                    .UpdateDatasetAndVersionDefinitionName(Arg.Is<Reference>(m => m.Id == definitionId && m.Name == definitionName));

            logger
                 .Received()
                 .Information(Arg.Is($"Updating relationships for updated definition name with definition id '{definitionId}'"));

            logger
               .Received(1)
               .Information(Arg.Is($"Updating datasets for updated definition name with definition id '{definitionId}'"));
        }

        private DatasetDefinitionNameChangeProcessor CreateProcessor(
            IDefinitionSpecificationRelationshipService definitionSpecificationRelationshipService = null,
            IDatasetService datasetService = null,
            ILogger logger = null,
            IFeatureToggle featureToggle = null)
        {
            return new DatasetDefinitionNameChangeProcessor(
                definitionSpecificationRelationshipService ?? CreateDefinitionSpecificationRelationshipService(),
                datasetService ?? CreateDataService(),
                logger ?? CreateLogger(),
                featureToggle ?? CreateFeatureToggle());
        }

        private static IDefinitionSpecificationRelationshipService CreateDefinitionSpecificationRelationshipService()
        {
            return Substitute.For<IDefinitionSpecificationRelationshipService>();
        }

        private static IDatasetService CreateDataService()
        {
            return Substitute.For<IDatasetService>();
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static IFeatureToggle CreateFeatureToggle(bool toggle = true)
        {
            IFeatureToggle featureToggle = Substitute.For<IFeatureToggle>();
            featureToggle
                .IsProcessDatasetDefinitionNameChangesEnabled()
                .Returns(toggle);

            return featureToggle;
        }
    }
}
