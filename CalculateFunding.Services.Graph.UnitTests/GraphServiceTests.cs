using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Graph.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Graph;
using CalculateFunding.Common.Graph.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using Serilog;
using System;
using Enum = CalculateFunding.Models.Graph.Enum;
using CalculateFunding.Common.Caching;
using Polly;
using CalculateFunding.Services.Core.Caching;

namespace CalculateFunding.Services.Graph.UnitTests
{
    [TestClass]
    public class GraphServiceTests
    {
        private ICalculationRepository _calculationRepository;
        private ISpecificationRepository _specificationRepository;
        private IDatasetRepository _datasetRepository;
        private IFundingLineRepository _fundingLineRepository;
        private IEnumRepository _enumRepository;
        private GraphService _graphService;
        private ICacheProvider _cacheProvider;
        private ILogger _logger;

        [TestInitialize]
        public void SetupTest()
        {
            _logger = Substitute.For<ILogger>();
            _calculationRepository = Substitute.For<ICalculationRepository>();
            _specificationRepository = Substitute.For<ISpecificationRepository>();
            _datasetRepository = Substitute.For<IDatasetRepository>();
            _fundingLineRepository = Substitute.For<IFundingLineRepository>();
            _enumRepository = Substitute.For<IEnumRepository>();
            _cacheProvider = Substitute.For<ICacheProvider>();

            _graphService = new GraphService(_logger, 
                _calculationRepository, 
                _specificationRepository,
                _datasetRepository,
                _fundingLineRepository,
                _enumRepository,
                new GraphResiliencePolicies {CacheProviderPolicy = Policy.NoOpAsync() },
                _cacheProvider);
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
        public async Task DeleteFundingLine()
        {
            string fieldId = NewRandomString();

            IActionResult result = await _graphService.DeleteFundingLine(fieldId);

            await _fundingLineRepository
                .Received(1)
                .DeleteFundingLine(fieldId);

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
        public async Task DeleteFundingLineCalculationRelationshipDelegatesToRepository()
        {
            string fundingLineId = NewRandomString();
            string calculationId = NewRandomString();

            IActionResult result = await _graphService.DeleteFundingLineCalculationRelationship(fundingLineId,
                calculationId);

            await _fundingLineRepository
                .Received(1)
                .DeleteFundingLineCalculationRelationship(fundingLineId, calculationId);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task DeleteCalculationFundingLineRelationshipDelegatesToRepository()
        {
            string calculationId = NewRandomString();
            string fundingLineId = NewRandomString();

            IActionResult result = await _graphService.DeleteCalculationFundingLineRelationship(calculationId, 
                fundingLineId);

            await _fundingLineRepository
                .Received(1)
                .DeleteCalculationFundingLineRelationship(calculationId, fundingLineId);

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
        public async Task SaveEnums_GivenValidEnums_OkStatusCodeReturned()
        {
            Enum[] enums = new[] { NewEnum(), NewEnum() };

            IActionResult result = await _graphService.UpsertEnums(enums);

            await _enumRepository
                .Received(1)
                .UpsertEnums(enums);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task SaveFundingLine_GivenValidEnums_OkStatusCodeReturned()
        {
            FundingLine[] fundingLines = new[] { NewFundingLine(), NewFundingLine() };

            IActionResult result = await _graphService.UpsertFundingLines(fundingLines);

            await _fundingLineRepository
                .Received(1)
                .UpsertFundingLines(fundingLines);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task SaveDatasetDefinitions_GivenValidEnums_OkStatusCodeReturned()
        {
            DatasetDefinition[] datasetDefinitions = new[] { NewDataDefinition(), NewDataDefinition() };

            IActionResult result = await _graphService.UpsertDatasetDefinitions(datasetDefinitions);

            await _datasetRepository
                .Received(1)
                .UpsertDatasetDefinitions(datasetDefinitions);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task SaveUpsertDatasets_GivenValidEnums_OkStatusCodeReturned()
        {
            Dataset[] datasets = new[] { NewDataset(), NewDataset() };

            IActionResult result = await _graphService.UpsertDatasets(datasets);

            await _datasetRepository
                .Received(1)
                .UpsertDatasets(datasets);

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
        public async Task GetAllEntitiesRelatedToSpecification()
        {
            string specificationId = NewRandomString();
            IActionResult result = await _graphService.GetAllEntities<Specification>(specificationId);

            await _specificationRepository
                .Received(1)
                .GetAllEntities(specificationId);

            result
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        public async Task GetAllEntitiesRelatedToCalculation()
        {
            string calculationId = NewRandomString();
            IActionResult result = await _graphService.GetAllEntities<Calculation>(calculationId);

            await _calculationRepository
                .Received(1)
                .GetAllEntities(calculationId);

            result
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        public async Task GetAllEntitiesRelatedToFundingLine()
        {
            string fundingLineId = NewRandomString();
            IActionResult result = await _graphService.GetAllEntities<FundingLine>(fundingLineId);

            await _fundingLineRepository
                .Received(1)
                .GetAllEntities(fundingLineId);

            result
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        public async Task GetAllEntitiesRelatedToDataField()
        {
            string dataFieldId = NewRandomString();
            IActionResult result = await _graphService.GetAllEntities<DataField>(dataFieldId);

            await _datasetRepository
                .Received(1)
                .GetAllEntities(dataFieldId);

            result
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        public async Task GetAllEntitiesRelatedToEnum()
        {
            string enumId = NewRandomString();
            IActionResult result = await _graphService.GetAllEntities<Enum>(enumId);

            await _enumRepository
                .Received(1)
                .GetAllEntities(enumId);

            result
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        public async Task DeleteEnum_GivenExistingEnum_OkStatusCodeReturned()
        {
            string enumId = NewRandomString();

            IActionResult result = await _graphService.DeleteEnum(enumId);

            await _enumRepository
                .Received(1)
                .DeleteEnum(enumId);

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
        public async Task UpsertCalculationEnumRelationship_GivenValidRelationship_OkStatusCodeReturned()
        {
            string calculationId = NewRandomString();
            string enumId = NewRandomString();

            IActionResult result = await _graphService.UpsertCalculationEnumRelationship(calculationId,
                enumId);

            await _enumRepository
                .Received(1)
                .UpsertCalculationEnumRelationship(calculationId, enumId);

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
            string datasetFieldId = NewRandomString();

            IActionResult result = await _graphService.UpsertCalculationDataFieldRelationship(calculationId,
                datasetFieldId);

            await _calculationRepository
                .Received(1)
                .UpsertCalculationDataFieldRelationship(calculationId, datasetFieldId);

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
            string cacheKey = $"{CacheKeys.CircularDependencies}{specificationId}";
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
            
            AndTheCalculationCircularDependenciesInSpecification(specificationId, expectedCalculationOne, expectedCalculationTwo);
            
            OkObjectResult result = await _graphService.GetCalculationCircularDependencies(specificationId) as OkObjectResult;

            await _cacheProvider
                .Received(1)
                .SetAsync(cacheKey, Arg.Any<List<Entity<Calculation, IRelationship>>>(), Arg.Any<TimeSpan>(), true);

            result?.Value
                .Should()
                .BeEquivalentTo(new []
                {
                    expectedCalculationOne,
                    expectedCalculationTwo
                });
        }

        [TestMethod]
        public async Task DeleteFundingLines()
        {
            string[] fieldIds = AsArray(NewRandomString(), NewRandomString());
            
            IActionResult result = await _graphService.DeleteFundingLines(fieldIds);

            await _fundingLineRepository
                .Received(1)
                .DeleteFundingLines(fieldIds);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task UpsertFundingLineCalculationRelationships()
        {
            (string, string)[] relationships = AsArray((NewRandomString(), NewRandomString()));
            
            IActionResult result = await _graphService.UpsertFundingLineCalculationRelationships(relationships);

            await _fundingLineRepository
                .Received(1)
                .UpsertFundingLineCalculationRelationships(relationships);

            result
                .Should()
                .BeOfType<OkResult>();
        }
        
        [TestMethod]
        public async Task UpsertCalculationFundingLineRelationships()
        {
            (string, string)[] relationships = AsArray((NewRandomString(), NewRandomString()));
            
            IActionResult result = await _graphService.UpsertCalculationFundingLineRelationships(relationships);

            await _fundingLineRepository
                .Received(1)
                .UpsertCalculationFundingLineRelationships(relationships);

            result
                .Should()
                .BeOfType<OkResult>();
        }
        
        [TestMethod]
        public async Task DeleteFundingLineCalculationRelationships()
        {
            (string, string)[] relationships = AsArray((NewRandomString(), NewRandomString()));
            
            IActionResult result = await _graphService.DeleteFundingLineCalculationRelationships(relationships);

            await _fundingLineRepository
                .Received(1)
                .DeleteFundingLineCalculationRelationships(relationships);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task DeleteCalculationFundingLineRelationships()
        {
            (string, string)[] relationships = AsArray((NewRandomString(), NewRandomString()));

            IActionResult result = await _graphService.DeleteCalculationFundingLineRelationships(relationships);

            await _fundingLineRepository
                .Received(1)
                .DeleteCalculationFundingLineRelationships(relationships);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task UpsertDataDefinitionDatasetRelationships()
        {
            (string, string)[] relationships = AsArray((NewRandomString(), NewRandomString()));
            
            IActionResult result = await _graphService.UpsertDataDefinitionDatasetRelationships(relationships);

            await _datasetRepository
                .Received(1)
                .UpsertDataDefinitionDatasetRelationships(relationships);

            result
                .Should()
                .BeOfType<OkResult>();
        }
        
        [TestMethod]
        public async Task UpsertDatasetDataFieldRelationships()
        {
            (string, string)[] relationships = AsArray((NewRandomString(), NewRandomString()));
            
            IActionResult result = await _graphService.UpsertDatasetDataFieldRelationships(relationships);

            await _datasetRepository
                .Received(1)
                .UpsertDatasetDataFieldRelationships(relationships);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task UpsertSpecificationDatasetRelationship()
        {
            string specificationId = NewRandomString();
            string datasetId = NewRandomString();

            IActionResult result = await _graphService.UpsertSpecificationDatasetRelationship(specificationId, datasetId);

            await _specificationRepository
                .Received(1)
                .CreateSpecificationDatasetRelationship(specificationId, datasetId);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task UpsertFundingLineCalculationRelationship()
        {
            string fundingLineId = NewRandomString();
            string calculationId = NewRandomString();

            IActionResult result = await _graphService.UpsertFundingLineCalculationRelationship(fundingLineId, calculationId);

            await _fundingLineRepository
                .Received(1)
                .UpsertFundingLineCalculationRelationship(fundingLineId, calculationId);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task UpsertCalculationFundingLineRelationship()
        {
            string calculationId = NewRandomString();
            string fundingLineId = NewRandomString();

            IActionResult result = await _graphService.UpsertCalculationFundingLineRelationship(calculationId, fundingLineId);

            await _fundingLineRepository
                .Received(1)
                .UpsertCalculationFundingLineRelationship(calculationId, fundingLineId);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task UpsertSpecificationDatasetRelationships()
        {
            (string, string)[] relationships = AsArray((NewRandomString(), NewRandomString()));
            
            IActionResult result = await _graphService.UpsertSpecificationDatasetRelationships(relationships);

            await _specificationRepository
                .Received(1)
                .CreateSpecificationDatasetRelationships(relationships);

            result
                .Should()
                .BeOfType<OkResult>();
        }
        
        [TestMethod]
        public async Task DeleteSpecificationDatasetRelationships()
        {
            (string, string)[] relationships = AsArray((NewRandomString(), NewRandomString()));
            
            IActionResult result = await _graphService.DeleteSpecificationDatasetRelationships(relationships);

            await _specificationRepository
                .Received(1)
                .DeleteSpecificationDatasetRelationships(relationships);

            result
                .Should()
                .BeOfType<OkResult>();
        }
        
        [TestMethod]
        public async Task UpsertCalculationDataFieldRelationships()
        {
            (string, string)[] relationships = AsArray((NewRandomString(), NewRandomString()));
            
            IActionResult result = await _graphService.UpsertCalculationDataFieldRelationships(relationships);

            await _calculationRepository
                .Received(1)
                .UpsertCalculationDataFieldRelationships(relationships);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task UpsertCalculationEnumRelationships()
        {
            (string, string)[] relationships = AsArray((NewRandomString(), NewRandomString()));

            IActionResult result = await _graphService.UpsertCalculationEnumRelationships(relationships);

            await _enumRepository
                .Received(1)
                .UpsertCalculationEnumRelationships(relationships);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task DeleteCalculationDataFieldRelationships()
        {
            (string, string)[] relationships = AsArray((NewRandomString(), NewRandomString()));
            
            IActionResult result = await _graphService.DeleteCalculationDataFieldRelationships(relationships);

            await _calculationRepository
                .Received(1)
                .DeleteCalculationDataFieldRelationships(relationships);

            result
                .Should()
                .BeOfType<OkResult>();
        }
        
        [TestMethod]
        public async Task DeleteCalculations()
        {
            string[] ids = AsArray(NewRandomString(), NewRandomString());
            
            IActionResult result = await _graphService.DeleteCalculations(ids);

            await _calculationRepository
                .Received(1)
                .DeleteCalculations(ids);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task DeleteEnums()
        {
            string[] ids = AsArray(NewRandomString(), NewRandomString());

            IActionResult result = await _graphService.DeleteEnums(ids);

            await _enumRepository
                .Received(1)
                .DeleteEnums(ids);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task UpsertCalculationSpecificationRelationships()
        {
            (string, string)[] relationships = AsArray((NewRandomString(), NewRandomString()));
            
            IActionResult result = await _graphService.UpsertCalculationSpecificationRelationships(relationships);

            await _calculationRepository
                .Received(1)
                .UpsertCalculationSpecificationRelationships(relationships);

            result
                .Should()
                .BeOfType<OkResult>();
        }
        
        [TestMethod]
        public async Task GetAllCalculationsForAll()
        {
            string[] ids = AsArray(NewRandomString(), NewRandomString());

            Entity<Calculation,IRelationship>[] entities = AsArray(NewCalculationEntity(NewRandomString()));
            
            _calculationRepository.GetAllEntitiesForAll(ids)
                .Returns(entities);
            
            IEnumerable<Entity<Calculation, IRelationship>> result = 
                (await _graphService.GetAllCalculationsForAll(ids) as OkObjectResult)?.Value as IEnumerable<Entity<Calculation, IRelationship>>;

            result
                .Should()
                .BeEquivalentTo<Entity<Calculation,IRelationship>>(entities);
        }
        
        [TestMethod]
        public async Task GetAllFundingLinesForAll()
        {
            string[] ids = AsArray(NewRandomString(), NewRandomString());

            Entity<FundingLine,IRelationship>[] entities = AsArray(NewFundingLineEntity(NewRandomString()));
            
            _fundingLineRepository.GetAllEntitiesForAll(ids)
                .Returns(entities);
            
            IEnumerable<Entity<FundingLine, IRelationship>> result = 
                (await _graphService.GetAllFundingLinesForAll(ids) as OkObjectResult)?.Value as IEnumerable<Entity<FundingLine, IRelationship>>;

            result
                .Should()
                .BeEquivalentTo<Entity<FundingLine,IRelationship>>(entities);
        }
        
        [TestMethod]
        public async Task DeleteCalculationSpecificationRelationships()
        {
            (string, string)[] relationships = AsArray((NewRandomString(), NewRandomString()));
            
            IActionResult result = await _graphService.DeleteCalculationSpecificationRelationships(relationships);

            await _calculationRepository
                .Received(1)
                .DeleteCalculationSpecificationRelationships(relationships);

            result
                .Should()
                .BeOfType<OkResult>();
        }
        
        [TestMethod]
        public async Task DeleteCalculationCalculationRelationships()
        {
            (string, string)[] relationships = AsArray((NewRandomString(), NewRandomString()));
            
            IActionResult result = await _graphService.DeleteCalculationCalculationRelationships(relationships);

            await _calculationRepository
                .Received(1)
                .DeleteCalculationCalculationRelationships(relationships);

            result
                .Should()
                .BeOfType<OkResult>();
        }
        
        [TestMethod]
        public async Task UpsertCalculationDataFieldsRelationships()
        {
            string idA = NewRandomString();
            string idBOne = NewRandomString();
            string idBTwo = NewRandomString();
            
            (string, string)[] relationships = AsArray((idA, idBOne), (idA, idBTwo));
            
            IActionResult result = await _graphService.UpsertCalculationDataFieldsRelationships(idA, AsArray(idBOne, idBTwo));

            await _calculationRepository
                .Received(1)
                .UpsertCalculationDataFieldRelationships(Arg.Is<(string, string)[]>(_ => _.SequenceEqual(relationships)) );

            result
                .Should()
                .BeOfType<OkResult>();
        }
        
        [TestMethod]
        public async Task UpsertCalculationCalculationsRelationships()
        {
            string idA = NewRandomString();
            string idBOne = NewRandomString();
            string idBTwo = NewRandomString();
            
            (string, string)[] relationships = AsArray((idA, idBOne), (idA, idBTwo));
            
            IActionResult result = await _graphService.UpsertCalculationCalculationsRelationships(idA, AsArray(idBOne, idBTwo));

            await _calculationRepository
                .Received(1)
                .UpsertCalculationCalculationRelationships(Arg.Is<(string, string)[]>(_ => _.SequenceEqual(relationships)) );

            result
                .Should()
                .BeOfType<OkResult>();
        }
        
        private static Entity<FundingLine, IRelationship> NewFundingLineEntity(string fundingLineId) =>
            new Entity<FundingLine, IRelationship>
            {
                Node = new FundingLine
                {
                    FundingLineId = fundingLineId
                }
            };

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

        private void AndTheCalculationCircularDependenciesInSpecification(string specificationId,
            params Entity<Calculation, IRelationship>[] calculations)
        {
            _calculationRepository.GetAllCalculationsForSpecification(specificationId)
                .Returns(calculations);

            _calculationRepository.GetCalculationCircularDependencies(Arg.Is<string[]>(_ => _.SequenceEqual(calculations.Select(_ => _.Node.CalculationId).ToArray())))
                .Returns(calculations);
        }

        private string NewRandomString() => new RandomString();

        private TItem[] AsArray<TItem>(params TItem[] items) => items;

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

        private Enum NewEnum(Action<EnumBuilder> setUp = null)
        {
            EnumBuilder enumBuilder = new EnumBuilder();

            setUp?.Invoke(enumBuilder);

            return enumBuilder.Build();
        }

        private FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);

            return fundingLineBuilder.Build();
        }

        private DatasetDefinition NewDataDefinition(Action<DatasetDefinitionBuilder> setUp = null)
        {
            DatasetDefinitionBuilder datasetDefinitionBuilder = new DatasetDefinitionBuilder();

            setUp?.Invoke(datasetDefinitionBuilder);

            return datasetDefinitionBuilder.Build();
        }

        private Dataset NewDataset(Action<DatasetBuilder> setUp = null)
        {
            DatasetBuilder datasetBuilder = new DatasetBuilder();

            setUp?.Invoke(datasetBuilder);

            return datasetBuilder.Build();
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
