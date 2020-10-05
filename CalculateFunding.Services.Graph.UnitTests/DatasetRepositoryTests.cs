using System;
using System.Threading.Tasks;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Graph.Constants;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Graph.UnitTests
{
    [TestClass]
    public class DatasetRepositoryTests : GraphRepositoryTestBase
    {
        private DatasetRepository _datasetRepository;

        [TestInitialize]
        public void SetUp()
        {
            _datasetRepository = new DatasetRepository(GraphRepository);
        }

        [TestMethod]
        public async Task UpsertDatasetDelegatesToGraphRepository()
        {
            Dataset dataset = NewDataset();

            await _datasetRepository.UpsertDataset(dataset);

            await ThenTheNodeWasCreated(dataset, AttributeConstants.DatasetId);
        }
        
        [TestMethod]
        public async Task DeleteDatasetDelegatesToGraphRepository()
        {
            string datasetId = NewRandomString();

            await _datasetRepository.DeleteDataset(datasetId);

            await ThenTheNodeWasDeleted<Dataset>(AttributeConstants.DatasetId, datasetId);
        }
        
        [TestMethod]
        public async Task UpsertDatasetDefinitionDelegatesToGraphRepository()
        {
            DatasetDefinition definition = NewDataDefinition();

            await _datasetRepository.UpsertDatasetDefinition(definition);

            await ThenTheNodeWasCreated(definition, AttributeConstants.DatasetDefinitionId);
        }
        
        [TestMethod]
        public async Task DeleteDatasetDefinitionDelegatesToGraphRepository()
        {
            string definitionId = NewRandomString();

            await _datasetRepository.DeleteDatasetDefinition(definitionId);

            await ThenTheNodeWasDeleted<DatasetDefinition>(AttributeConstants.DatasetDefinitionId, definitionId);
        }
        
        [TestMethod]
        public async Task UpsertDataFieldDelegatesToGraphRepository()
        {
            DataField field = NewDataField();

            await _datasetRepository.UpsertDataField(field);

            await ThenTheNodeWasCreated(field, AttributeConstants.DataFieldId);
        }
        
        [TestMethod]
        public async Task DeleteDataFieldDelegatesToGraphRepository()
        {
            string fieldId = NewRandomString();

            await _datasetRepository.DeleteDataField(fieldId);

            await ThenTheNodeWasDeleted<DataField>(AttributeConstants.DataFieldId, fieldId);
        }
        
        [TestMethod]
        public async Task CreateDataDefinitionDatasetRelationshipDelegatesToGraphRepository()
        {
            string definitionId = NewRandomString();
            string datasetId = NewRandomString();
            
            await _datasetRepository.UpsertDataDefinitionDatasetRelationship(definitionId,
                datasetId);

            await ThenTheRelationshipWasCreated<DatasetDefinition, Dataset>(AttributeConstants.DatasetDefinitionDatasetRelationshipId,
                (AttributeConstants.DatasetDefinitionId, definitionId),
                (AttributeConstants.DatasetId, datasetId));

            await AndTheRelationshipWasCreated<Dataset, DatasetDefinition>(AttributeConstants.DatasetDatasetDefinitionRelationshipId,
                (AttributeConstants.DatasetId, datasetId),
                (AttributeConstants.DatasetDefinitionId, definitionId));
        }
        
        [TestMethod]
        public async Task DeleteDataDefinitionDatasetRelationshipDelegatesToGraphRepository()
        {
            string definitionId = NewRandomString();
            string datasetId = NewRandomString();
            
            await _datasetRepository.DeleteDataDefinitionDatasetRelationship(definitionId,
                datasetId);

            await ThenTheRelationshipWasDeleted<DatasetDefinition, Dataset>(AttributeConstants.DatasetDefinitionDatasetRelationshipId,
                (AttributeConstants.DatasetDefinitionId, definitionId),
                (AttributeConstants.DatasetId, datasetId));

            await AndTheRelationshipWasDeleted<Dataset, DatasetDefinition>(AttributeConstants.DatasetDatasetDefinitionRelationshipId,
                (AttributeConstants.DatasetId, datasetId),
                (AttributeConstants.DatasetDefinitionId, definitionId));
        }
        
        [TestMethod]
        public async Task CreateDatasetDataFieldRelationshipDelegatesToGraphRepository()
        {
            string datasetId = NewRandomString();
            string dataFieldId = NewRandomString();
            
            await _datasetRepository.UpsertDatasetDataFieldRelationship(datasetId,
                dataFieldId);

            await ThenTheRelationshipWasCreated<Dataset, DataField>(AttributeConstants.DatasetDataFieldRelationshipId,
                (AttributeConstants.DatasetId, datasetId),
                (AttributeConstants.DataFieldId, dataFieldId));

            await AndTheRelationshipWasCreated<DataField, Dataset>(AttributeConstants.DataFieldDatasetRelationshipId,
                (AttributeConstants.DataFieldId, dataFieldId),
                (AttributeConstants.DatasetId, datasetId));
        }
        
        [TestMethod]
        public async Task DeleteDatasetDataFieldRelationshipDelegatesToGraphRepository()
        {
            string datasetId = NewRandomString();
            string dataFieldId = NewRandomString();
            
            await _datasetRepository.DeleteDatasetDataFieldRelationship(datasetId,
                dataFieldId);

            await ThenTheRelationshipWasDeleted<Dataset, DataField>(AttributeConstants.DatasetDataFieldRelationshipId,
                (AttributeConstants.DatasetId, datasetId),
                (AttributeConstants.DataFieldId, dataFieldId));

            await AndTheRelationshipWasDeleted<DataField, Dataset>(AttributeConstants.DataFieldDatasetRelationshipId,
                (AttributeConstants.DataFieldId, dataFieldId),
                (AttributeConstants.DatasetId, datasetId));
        }

        private Dataset NewDataset(Action<DatasetBuilder> setUp = null)
        {
            DatasetBuilder datasetBuilder = new DatasetBuilder();
            
            setUp?.Invoke(datasetBuilder);

            return datasetBuilder.Build();
        }
        
        private DataField NewDataField(Action<DataFieldBuilder> setUp = null)
        {
            DataFieldBuilder datasetBuilder = new DataFieldBuilder();
            
            setUp?.Invoke(datasetBuilder);

            return datasetBuilder.Build();
        }

        private DatasetDefinition NewDataDefinition(Action<DatasetDefinitionBuilder> setUp = null)
        {
            DatasetDefinitionBuilder datasetBuilder = new DatasetDefinitionBuilder();
            
            setUp?.Invoke(datasetBuilder);

            return datasetBuilder.Build();
        }
    }
}