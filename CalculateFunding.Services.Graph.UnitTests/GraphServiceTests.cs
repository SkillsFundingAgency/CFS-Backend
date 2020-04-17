using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Graph.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Threading.Tasks;
using CalculateFunding.Tests.Common.Helpers;
using Serilog;
using Neo4jDriver = Neo4j.Driver;

namespace CalculateFunding.Services.Graph.UnitTests
{
    [TestClass]
    public class GraphServiceTests
    {
        private ICalculationRepository _calculationRepository;
        private ISpecificationRepository _specificationRepository;
        private IDatasetRepository _datasetRepository;
        private GraphService _graphService;
        private ILogger _logger;

        [TestInitialize]
        public void SetupTest()
        {
            _logger = Substitute.For<ILogger>();
            _calculationRepository = Substitute.For<ICalculationRepository>();
            _specificationRepository = Substitute.For<ISpecificationRepository>();
            _datasetRepository = Substitute.For<IDatasetRepository>();
            
            _graphService = new GraphService(_logger, 
                _calculationRepository, 
                _specificationRepository,
                _datasetRepository);
        }

        [TestMethod]
        public async Task UpsertDatasetDelegatesToTheRepository()
        {
            Dataset dataset = new Dataset();

            IActionResult result = await _graphService.UpsertDataset(dataset);

            await _datasetRepository
                .Received(1)
                .UpsertDataset(dataset);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task DeleteDatasetDelegatesToTheRepository()
        {
            string datasetId = NewRandomString();
            
            IActionResult result = await _graphService.DeleteDataset(datasetId);

            await _datasetRepository
                .Received(1)
                .DeleteDataset(datasetId);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task UpsertDatasetDefinitionDelegatesToTheRepository()
        {
            DatasetDefinition definition = new DatasetDefinition();

            IActionResult result = await _graphService.UpsertDatasetDefinition(definition);

            await _datasetRepository
                .Received(1)
                .UpsertDatasetDefinition(definition);

            result
                .Should()
                .BeOfType<OkResult>();
        }
        
        [TestMethod]
        public async Task DeleteDatasetDefinitionDelegatesToTheRepository()
        {
            string definitionId = NewRandomString();
            
            IActionResult result = await _graphService.DeleteDatasetDefinition(definitionId);

            await _datasetRepository
                .Received(1)
                .DeleteDatasetDefinition(definitionId);

            result
                .Should()
                .BeOfType<OkResult>();
        }
        
        [TestMethod]
        public async Task UpsertDataFieldDelegatesToTheRepository()
        {
            DataField field = new DataField();

            IActionResult result = await _graphService.UpsertDataField(field);

            await _datasetRepository
                .Received(1)
                .UpsertDataField(field);

            result
                .Should()
                .BeOfType<OkResult>();
        }
        
        [TestMethod]
        public async Task DeleteDataFieldDelegatesToTheRepository()
        {
            string fieldId = NewRandomString();
            
            IActionResult result = await _graphService.DeleteDataField(fieldId);

            await _datasetRepository
                .Received(1)
                .DeleteDataField(fieldId);

            result
                .Should()
                .BeOfType<OkResult>();
        }
        
        [TestMethod]
        public async Task UpsertDataDefinitionDatasetRelationshipDelegatesToRepository()
        {
            string definitionId = NewRandomString();
            string datasetId = NewRandomString();
            
            IActionResult result = await _graphService.UpsertDataDefinitionDatasetRelationship(definitionId,
                datasetId);

            await _datasetRepository
                .Received(1)
                .CreateDataDefinitionDatasetRelationship(definitionId, datasetId);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task DeleteDataDefinitionDatasetRelationshipDelegatesToRepository()
        {
            string definitionId = NewRandomString();
            string datasetId = NewRandomString();
            
            IActionResult result = await _graphService.DeleteDataDefinitionDatasetRelationship(definitionId,
                datasetId);

            await _datasetRepository
                .Received(1)
                .DeleteDataDefinitionDatasetRelationship(definitionId, datasetId);

            result
                .Should()
                .BeOfType<OkResult>();
        }
        
        [TestMethod]
        public async Task UpsertDatasetDataFieldRelationshipDelegatesToRepository()
        {
            string datasetId = NewRandomString();
            string fieldId = NewRandomString();
            
            IActionResult result = await _graphService.UpsertDatasetDataFieldRelationship(datasetId,
                fieldId);

            await _datasetRepository
                .Received(1)
                .CreateDatasetDataFieldRelationship(datasetId, fieldId);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task DeleteDatasetDataFieldRelationshipDelegatesToRepository()
        {
            string datasetId = NewRandomString();
            string fieldId = NewRandomString();
            
            IActionResult result = await _graphService.DeleteDatasetDataFieldRelationship(datasetId,
                fieldId);

            await _datasetRepository
                .Received(1)
                .DeleteDatasetDataFieldRelationship(datasetId, fieldId);

            result
                .Should()
                .BeOfType<OkResult>();
        }
        
        [TestMethod]
        public async Task CreateSpecificationDatasetRelationshipDelegatesToRepository()
        {
            string specificationId = NewRandomString();
            string datasetId = NewRandomString();
            
            IActionResult result = await _graphService.CreateSpecificationDatasetRelationship(specificationId,
                datasetId);

            await _specificationRepository
                .Received(1)
                .CreateSpecificationDatasetRelationship(specificationId, datasetId);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task DeleteSpecificationDatasetRelationshipDelegatesToRepository()
        {
            string specificationId = NewRandomString();
            string datasetId = NewRandomString();
            
            IActionResult result = await _graphService.DeleteSpecificationDatasetRelationship(specificationId,
                datasetId);

            await _specificationRepository
                .Received(1)
                .DeleteSpecificationDatasetRelationship(specificationId, datasetId);

            result
                .Should()
                .BeOfType<OkResult>();
        }
        
        [TestMethod]
        public async Task CreateCalculationDataFieldRelationshipDelegatesToRepository()
        {
            string calculationId = NewRandomString();
            string fieldId = NewRandomString();
            
            IActionResult result = await _graphService.CreateCalculationDataFieldRelationship(calculationId,
                fieldId);

            await _calculationRepository
                .Received(1)
                .CreateCalculationDataFieldRelationship(calculationId, fieldId);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task DeleteCalculationDataFieldRelationshipDelegatesToRepository()
        {
            string calculationId = NewRandomString();
            string fieldId = NewRandomString();
            
            IActionResult result = await _graphService.DeleteCalculationDataFieldRelationship(calculationId,
                fieldId);

            await _calculationRepository
                .Received(1)
                .DeleteCalculationDataFieldRelationship(calculationId, fieldId);

            result
                .Should()
                .BeOfType<OkResult>();
        }
        
        [TestMethod]
        public async Task SaveCalculations_GivenValidCalculations_OkStatusCodeReturned()
        {
            Calculation[] calculations = new[] { NewCalculation(), NewCalculation() };

            IActionResult result = await _graphService.UpsertCalculations(calculations);

            await _calculationRepository
                .Received(1)
                .UpsertCalculations(calculations);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task SaveSpecifications_GivenValidSpecifications_OkStatusCodeReturned()
        {
            Specification[] specifications = new[] { NewSpecification(), NewSpecification() };

            IActionResult result = await _graphService.UpsertSpecifications(specifications);

            await _specificationRepository
                .Received(1)
                .UpsertSpecifications(specifications);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task DeleteCalculation_GivenExistingCalculation_OkStatusCodeReturned()
        {
            string calculationId = NewRandomString();
            
            IActionResult result = await _graphService.DeleteCalculation(calculationId);

            await _calculationRepository
                .Received(1)
                .DeleteCalculation(calculationId);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task DeleteSpecification_GivenExistingSpecification_OkStatusCodeReturned()
        {
            string specificationId = NewRandomString();
            
            IActionResult result = await _graphService.DeleteSpecification(specificationId);

            await _specificationRepository
                .Received(1)
                .DeleteSpecification(specificationId);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task CreateCalculationSpecificationRelationship_GivenValidRelationship_OkStatusCodeReturned()
        {
            string calculationId = NewRandomString();
            string specificationId = NewRandomString();
            
            IActionResult result = await _graphService.UpsertCalculationSpecificationRelationship(calculationId,
                specificationId);

            await _calculationRepository
                .Received(1)
                .UpsertCalculationSpecificationRelationship(calculationId, specificationId);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task CreateCalculationCalculationRelationship_GivenValidRelationship_OkStatusCodeReturned()
        {
            string calculationAId = NewRandomString();
            string calculationBId = NewRandomString();
            
            IActionResult result = await _graphService.UpsertCalculationCalculationRelationship(calculationAId,
                calculationBId);

            await _calculationRepository
                .Received(1)
                .UpsertCalculationCalculationRelationship(calculationAId, calculationBId);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task DeleteCalculationSpecificationRelationship_GivenValidRelationship_OkStatusCodeReturned()
        {
            string calculationAId = NewRandomString();
            string specificationId = NewRandomString();

            IActionResult result = await _graphService.DeleteCalculationSpecificationRelationship(calculationAId, specificationId);

            await _calculationRepository
                .Received(1)
                .DeleteCalculationSpecificationRelationship(calculationAId, specificationId);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task DeleteCalculationCalculationRelationship_GivenValidRelationship_OkStatusCodeReturned()
        {
            string calculationAId = NewRandomString();
            string calculationBId = NewRandomString();

            IActionResult result = await _graphService.DeleteCalculationCalculationRelationship(calculationAId, calculationBId);

            await _calculationRepository
                .Received(1)
                .DeleteCalculationCalculationRelationship(calculationAId, calculationBId);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task DeleteAllForSpecificationDelegatesToSpecificationRepository()
        {
            string specificationId = NewRandomString();

            IActionResult result = await _graphService.DeleteAllForSpecification(specificationId);
            
            result
                .Should()
                .BeOfType<OkResult>();

            await _specificationRepository
                .Received(1)
                .DeleteAllForSpecification(specificationId);
        }

        [TestMethod]
        public async Task SaveDatasetFields_GivenValidDatasetFields_OkStatusCodeReturned()
        {
            DatasetField[] datasetFields = new[] { NewDatasetField(), NewDatasetField() };

            IActionResult result = await _graphService.UpsertDatasetField(datasetFields);

            await _datasetRepository
                .Received(1)
                .UpsertDatasetField(datasetFields);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task CreateCalculationDatasetFieldsRelationship_GivenValidRelationship_OkStatusCodeReturned()
        {
            string calculationId = NewRandomString();
            string datasetFieldid = NewRandomString();

            IActionResult result = await _graphService.UpsertCalculationDatasetFieldRelationship(calculationId,
                datasetFieldid);

            await _datasetRepository
                .Received(1)
                .UpsertCalculationDatasetFieldRelationship(calculationId, datasetFieldid);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task DeleteCalculationDatasetFieldRelationship_GivenValidRelationship_OkStatusCodeReturned()
        {
            string calculationId = NewRandomString();
            string datasetFieldId = NewRandomString();

            IActionResult result = await _graphService.DeleteCalculationDatasetFieldRelationship(calculationId, datasetFieldId);

            await _datasetRepository
                .Received(1)
                .DeleteCalculationDatasetFieldRelationship(calculationId, datasetFieldId);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        private string NewRandomString() => new RandomString();

        private Calculation NewCalculation(Action<CalculationBuilder> setUp = null)
        {
            CalculationBuilder calculationBuilder = new CalculationBuilder();

            setUp?.Invoke(calculationBuilder);

            return calculationBuilder.Build();
        }

        private Specification NewSpecification(Action<SpecificationBuilder> setUp = null)
        {
            SpecificationBuilder specificationBuilder = new SpecificationBuilder();

            setUp?.Invoke(specificationBuilder);

            return specificationBuilder.Build();
        }

        private DatasetField NewDatasetField(Action<DatasetFieldBuilder> setUp = null)
        {
            DatasetFieldBuilder datasetFieldBuilder = new DatasetFieldBuilder();

            setUp?.Invoke(datasetFieldBuilder);

            return datasetFieldBuilder.Build();
        }
    }
}
