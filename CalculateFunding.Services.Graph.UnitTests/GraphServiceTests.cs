using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Graph.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CalculateFunding.Common.Graph;
using CalculateFunding.Common.Graph.Interfaces;
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
                .UpsertDataDefinitionDatasetRelationship(definitionId, datasetId);

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
                .UpsertDatasetDataFieldRelationship(datasetId, fieldId);

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
            
            IActionResult result = await _graphService.UpsertSpecificationDatasetRelationship(specificationId,
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
            
            IActionResult result = await _graphService.UpsertCalculationDataFieldRelationship(calculationId,
                fieldId);

            await _calculationRepository
                .Received(1)
                .UpsertCalculationDataFieldRelationship(calculationId, fieldId);

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
        public async Task SaveDatasetFields_GivenValidDatasetFields_OkStatusCodeReturned()
        {
            DataField[] datasetFields = new[] { NewDataField(), NewDataField() };

            IActionResult result = await _graphService.UpsertDataFields(datasetFields);

            await _datasetRepository
                .Received(1)
                .UpsertDataFields(datasetFields);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task CreateCalculationDatasetFieldsRelationship_GivenValidRelationship_OkStatusCodeReturned()
        {
            string calculationId = NewRandomString();
            string datasetFieldid = NewRandomString();

            IActionResult result = await _graphService.UpsertCalculationDataFieldRelationship(calculationId,
                datasetFieldid);

            await _calculationRepository
                .Received(1)
                .UpsertCalculationDataFieldRelationship(calculationId, datasetFieldid);

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

            await _calculationRepository
                .Received(1)
                .DeleteCalculationDataFieldRelationship(calculationId, datasetFieldId);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task GetCircularDependencies_FetchesCalculationsInSpecificationWithSuppliedId_QueriesCircularDependenciesPerCalculation()
        {
            const string calculationid = nameof(calculationid);
            
            string specificationId = NewRandomString();
            string calculationOneId = NewRandomString();
            string calculationTwoId = NewRandomString();

            GivenTheSpecificationContents(specificationId,
                NewSpecificationEntity(_ => _.WithRelationships(
                    NewRelationship(rel => rel.WithOne((calculationid, calculationOneId),
                        (NewRandomString(), NewRandomString()))),
                    NewRelationship(rel => rel.WithOne((NewRandomString(), NewRandomString()))),
                    NewRelationship(rel => rel.WithOne((NewRandomString(), NewRandomString()), 
                        (calculationid, calculationTwoId))))));
            
            Entity<Calculation, IRelationship> expectedCalculationOne = NewCalculationEntity(calculationOneId);
            Entity<Calculation, IRelationship> expectedCalculationTwo = NewCalculationEntity(calculationTwoId);
            
            AndTheCalculationCircularDependencies(calculationOneId, expectedCalculationOne, expectedCalculationTwo, expectedCalculationTwo);
            AndTheCalculationCircularDependencies(calculationTwoId, expectedCalculationTwo, expectedCalculationOne);
            
            OkObjectResult result = await _graphService.GetCalculationCircularDependencies(specificationId) as OkObjectResult;
            
            //gives distinct results by calc id
            result?.Value
                .Should()
                .BeEquivalentTo(new []
                {
                    expectedCalculationOne,
                    expectedCalculationTwo
                });
        }

        private static Entity<Calculation, IRelationship> NewCalculationEntity(string calculationId) =>
            new Entity<Calculation, IRelationship>
            {
                Node = new Calculation
                {
                    CalculationId = calculationId
                }
            };

        private void GivenTheSpecificationContents(string specificationId,
            params Entity<Specification, IRelationship>[] contents)
        {
            _specificationRepository
                .GetAllEntities(specificationId)
                .Returns(contents);
        }

        private void AndTheCalculationCircularDependencies(string calculationId,
            params Entity<Calculation, IRelationship>[] calculations)
        {
            _calculationRepository.GetCalculationCircularDependencies(calculationId)
                .Returns(calculations);
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

        private DataField NewDataField(Action<DataFieldBuilder> setUp = null)
        {
            DataFieldBuilder datasetFieldBuilder = new DataFieldBuilder();

            setUp?.Invoke(datasetFieldBuilder);

            return datasetFieldBuilder.Build();
        }

        private Entity<Specification, IRelationship> NewSpecificationEntity(Action<EntityBuilder<Specification>> setUp = null)
        {
            EntityBuilder<Specification> entityBuilder = new EntityBuilder<Specification>();

            setUp?.Invoke(entityBuilder);
            
            return entityBuilder.Build();
        }

        private Relationship NewRelationship(Action<RelationshipBuilder> setUp = null)
        {
            RelationshipBuilder relationshipBuilder = new RelationshipBuilder();

            setUp?.Invoke(relationshipBuilder);
            
            return relationshipBuilder.Build();
        }
    }

    public class EntityBuilder<TEntity> : TestEntityBuilder
    where TEntity : class
    {
        private IEnumerable<IRelationship> _relationships;

        public EntityBuilder<TEntity> WithRelationships(params IRelationship[] relationships)
        {
            _relationships = relationships;

            return this;
        }
        
        public Entity<TEntity, IRelationship> Build()
        {
            return new Entity<TEntity, IRelationship>
            {
                Relationships = _relationships
            };
        }
    }

    public class RelationshipBuilder : TestEntityBuilder
    {
        private IDictionary<string, object> _one;

        public RelationshipBuilder WithOne(params (string key, object value)[] values)
        {
            _one = values.ToDictionary(_ => _.key, _ => _.value);

            return this;
        }
        
        public Relationship Build()
        {
            return new Relationship
            {
                One = _one
            };
        }
    }
}
