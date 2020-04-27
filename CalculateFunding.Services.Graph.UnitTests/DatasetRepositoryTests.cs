using System;
using System.Threading.Tasks;
using CalculateFunding.Models.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Graph.UnitTests
{
    [TestClass]
    public class DatasetRepositoryTests : GraphRepositoryTestBase
    {
        private const string DatasetId = Dataset.IdField;
        private const string DatasetDefinitionId = DatasetDefinition.IdField;
        private const string DataFieldId = DataField.IdField;

        private const string DatasetDatasetDefinitionRelationship = DatasetRepository.DatasetDatasetDefinitionRelationship;
        private const string DatasetDefinitionDatasetRelationship = DatasetRepository.DatasetDefinitionDatasetRelationship;
        private const string DataFieldDatasetRelationship = DatasetRepository.DataFieldDatasetRelationship;
        private const string DatasetDataFieldRelationship = DatasetRepository.DatasetDataFieldRelationship;
        
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

            await ThenTheNodeWasCreated(dataset, DatasetId);
        }
        
        [TestMethod]
        public async Task DeleteDatasetDelegatesToGraphRepository()
        {
            string datasetId = NewRandomString();

            await _datasetRepository.DeleteDataset(datasetId);

            await ThenTheNodeWasDeleted<Dataset>(DatasetId, datasetId);
        }
        
        [TestMethod]
        public async Task UpsertDatasetDefinitionDelegatesToGraphRepository()
        {
            DatasetDefinition definition = NewDataDefinition();

            await _datasetRepository.UpsertDatasetDefinition(definition);

            await ThenTheNodeWasCreated(definition, DatasetDefinitionId);
        }
        
        [TestMethod]
        public async Task DeleteDatasetDefinitionDelegatesToGraphRepository()
        {
            string definitionId = NewRandomString();

            await _datasetRepository.DeleteDatasetDefinition(definitionId);

            await ThenTheNodeWasDeleted<DatasetDefinition>(DatasetDefinitionId, definitionId);
        }
        
        [TestMethod]
        public async Task UpsertDataFieldDelegatesToGraphRepository()
        {
            DataField field = NewDataField();

            await _datasetRepository.UpsertDataField(field);

            await ThenTheNodeWasCreated(field, DataFieldId);
        }
        
        [TestMethod]
        public async Task DeleteDataFieldDelegatesToGraphRepository()
        {
            string fieldId = NewRandomString();

            await _datasetRepository.DeleteDataField(fieldId);

            await ThenTheNodeWasDeleted<DataField>(DataFieldId, fieldId);
        }
        
        [TestMethod]
        public async Task CreateDataDefinitionDatasetRelationshipDelegatesToGraphRepository()
        {
            string definitionId = NewRandomString();
            string datasetId = NewRandomString();
            
            await _datasetRepository.UpsertDataDefinitionDatasetRelationship(definitionId,
                datasetId);

            await ThenTheRelationshipWasCreated<DatasetDefinition, Dataset>(DatasetDefinitionDatasetRelationship,
                (DatasetDefinitionId, definitionId),
                (DatasetId, datasetId));

            await AndTheRelationshipWasCreated<Dataset, DatasetDefinition>(DatasetDatasetDefinitionRelationship,
                (DatasetId, datasetId),
                (DatasetDefinitionId, definitionId));
        }
        
        [TestMethod]
        public async Task DeleteDataDefinitionDatasetRelationshipDelegatesToGraphRepository()
        {
            string definitionId = NewRandomString();
            string datasetId = NewRandomString();
            
            await _datasetRepository.DeleteDataDefinitionDatasetRelationship(definitionId,
                datasetId);

            await ThenTheRelationshipWasDeleted<DatasetDefinition, Dataset>(DatasetDefinitionDatasetRelationship,
                (DatasetDefinitionId, definitionId),
                (DatasetId, datasetId));

            await AndTheRelationshipWasDeleted<Dataset, DatasetDefinition>(DatasetDatasetDefinitionRelationship,
                (DatasetId, datasetId),
                (DatasetDefinitionId, definitionId));
        }
        
        [TestMethod]
        public async Task CreateDatasetDataFieldRelationshipDelegatesToGraphRepository()
        {
            string datasetId = NewRandomString();
            string dataFieldId = NewRandomString();
            
            await _datasetRepository.UpsertDatasetDataFieldRelationship(datasetId,
                dataFieldId);

            await ThenTheRelationshipWasCreated<Dataset, DataField>(DatasetDataFieldRelationship,
                (DatasetId, datasetId),
                (DataFieldId, dataFieldId));

            await AndTheRelationshipWasCreated<DataField, Dataset>(DataFieldDatasetRelationship,
                (DataFieldId, dataFieldId),
                (DatasetId, datasetId));
        }
        
        [TestMethod]
        public async Task DeleteDatasetDataFieldRelationshipDelegatesToGraphRepository()
        {
            string datasetId = NewRandomString();
            string dataFieldId = NewRandomString();
            
            await _datasetRepository.DeleteDatasetDataFieldRelationship(datasetId,
                dataFieldId);

            await ThenTheRelationshipWasDeleted<Dataset, DataField>(DatasetDataFieldRelationship,
                (DatasetId, datasetId),
                (DataFieldId, dataFieldId));

            await AndTheRelationshipWasDeleted<DataField, Dataset>(DataFieldDatasetRelationship,
                (DataFieldId, dataFieldId),
                (DatasetId, datasetId));
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