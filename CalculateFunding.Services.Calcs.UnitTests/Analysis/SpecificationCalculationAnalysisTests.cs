using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Calcs.Analysis;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Compiler.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using ApiCalculation = CalculateFunding.Common.ApiClient.Calcs.Models.Calculation;
using Calculation = CalculateFunding.Models.Calcs.Calculation;
using GraphCalculation = CalculateFunding.Models.Graph.Calculation;

namespace CalculateFunding.Services.Calcs.UnitTests.Analysis
{
    [TestClass]
    public class SpecificationCalculationAnalysisTests : GraphTestBase
    {
        private Mock<ICalculationsRepository> _calculations;
        private Mock<ICalculationAnalysis> _calculationsAnalysis;
        private Mock<ISpecificationsApiClient> _specifications;
        private Mock<IDatasetReferenceService> _datasetReferenceService;
        private Mock<IBuildProjectsService> _buildProjectsService;

        private SpecificationCalculationAnalysis _analysis;

        [TestInitialize]
        public void SetUp()
        {
            _calculations = new Mock<ICalculationsRepository>();
            _calculationsAnalysis = new Mock<ICalculationAnalysis>();
            _specifications = new Mock<ISpecificationsApiClient>();
            _buildProjectsService = new Mock<IBuildProjectsService>();
            _datasetReferenceService = new Mock<IDatasetReferenceService>();
            
            _analysis = new SpecificationCalculationAnalysis(new ResiliencePolicies
            {
                CalculationsRepository = Policy.NoOpAsync(),
                SpecificationsApiClient = Policy.NoOpAsync()
            }, 
                _specifications.Object,
                _calculations.Object,
                _calculationsAnalysis.Object,
                _buildProjectsService.Object,
                _datasetReferenceService.Object,
                Mapper.Object);
        }

        [TestMethod]
        [DynamicData(nameof(MissingSpecificationIdExamples), DynamicDataSourceType.Method)]
        public void GuardsAgainstNotSpecificationIdBeingSupplied(string specificationId)
        {
            Func<Task<SpecificationCalculationRelationships>> invocation = () => WhenTheRelationshipsAreDetermined(specificationId);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("specificationId");
        }

        [TestMethod]
        public void GuardsAgainstNoSpecificationForTheSuppliedId()
        {
            string specificationId = NewRandomString();
            
            Func<Task<SpecificationCalculationRelationships>> invocation = () => WhenTheRelationshipsAreDetermined(specificationId);

            invocation
                .Should()
                .Throw<ArgumentOutOfRangeException>()
                .And
                .Message
                .Should()
                .StartWith($"No specification with id {specificationId}. Unable to get calculation relationships");         
        }

        [TestMethod]
        public void GuardsAgainstNoCalculationsForTheSpecificationWithSuppliedId()
        {
            string specificationId = NewRandomString();
            
            GivenTheSpecification(specificationId, new SpecificationSummary());
            
            Func<Task<SpecificationCalculationRelationships>> invocation = () => WhenTheRelationshipsAreDetermined(specificationId);

            invocation
                .Should()
                .Throw<ArgumentOutOfRangeException>()
                .And
                .Message
                .Should()
                .StartWith($"No calculations for specification with id {specificationId}. Unable to get calculation relationships");
        }

        [TestMethod]
        public async Task DeterminesCalculationRelationsForAllCalculationsInTheSpecificationWithTheSuppliedId()
        {
            string specificationId = NewRandomString();
            Calculation calculation = new Calculation();
            SpecificationSummary specificationSummary = new SpecificationSummary();
            Specification graphSpecification = NewGraphSpecification();
            Calculation[] calculations = new[] { calculation, };
            GraphCalculation[] graphCalculations = new GraphCalculation[0];
            CalculationRelationship[] calculationRelationships = new CalculationRelationship[0];

            List<Models.Calcs.DatasetRelationshipSummary> datasetRelationshipSummaries = new List<Models.Calcs.DatasetRelationshipSummary>();
            DatasetReference[] datasetReferences = new[] { new DatasetReference { 
                Calculations = graphCalculations.ToList(), 
                DataField = new DataField(),
                Dataset = new Models.Graph.Dataset(),
                DatasetDefinition = new DatasetDefinition()
            },  };

            GivenTheSpecification(specificationId, specificationSummary);   
            AndTheCalculations(specificationId, calculations);
            AndTheRelationshipsForTheCalculations(calculations, calculationRelationships);
            AndTheBuildProjectForTheSpecification(specificationId, datasetRelationshipSummaries);
            AndTheDatasetReferencesForTheCalculations(calculations, datasetRelationshipSummaries, datasetReferences);
            AndTheMapping(calculations, graphCalculations);
            AndTheMapping(specificationSummary, graphSpecification);

            SpecificationCalculationRelationships specificationCalculationRelationships = await WhenTheRelationshipsAreDetermined(specificationId);
            
            specificationCalculationRelationships
                .Should()
                .BeEquivalentTo(new SpecificationCalculationRelationships
                {
                    Specification = graphSpecification,
                    Calculations = graphCalculations,
                    CalculationRelationships = calculationRelationships,
                    CalculationDataFieldRelationships = datasetReferences.SelectMany(_ => _.Calculations.Select(calculation => new CalculationDataFieldRelationship { Calculation = calculation, DataField = _.DataField })),
                    DatasetDataFieldRelationships = datasetReferences.Select(_ => new DatasetDataFieldRelationship { Dataset = _.Dataset, DataField = _.DataField }),
                    DatasetDatasetDefinitionRelationships = datasetReferences.Select(_ => new DatasetDatasetDefinitionRelationship { Dataset = _.Dataset, DatasetDefinition = _.DatasetDefinition })
                });
        }

        private static IEnumerable<object[]> MissingSpecificationIdExamples()
        {
            yield return new object[] {null};
            yield return new object[] {""};
        }

        private async Task<SpecificationCalculationRelationships> WhenTheRelationshipsAreDetermined(string specificationId)
        {
            return await _analysis.GetSpecificationCalculationRelationships(specificationId);
        }

        private void GivenTheSpecification(string specificationId, SpecificationSummary specification)
        {
            _specifications.Setup(_ => _.GetSpecificationSummaryById(specificationId))
                .ReturnsAsync(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, specification));
        }

        private void AndTheCalculations(string specificationId, params Calculation[] calculations)
        {
            _calculations.Setup(_ => _.GetCalculationsBySpecificationId(specificationId))
                .ReturnsAsync(calculations);
        }

        private void AndTheRelationshipsForTheCalculations(IEnumerable<Calculation> calculations, 
            params CalculationRelationship[] relationships)
        {
            _calculationsAnalysis.Setup(_ => _.DetermineRelationshipsBetweenCalculations(calculations))
                .Returns(relationships);
        }
        private void AndTheBuildProjectForTheSpecification(string specificationId, List<Models.Calcs.DatasetRelationshipSummary> datasetRelationships)
        {
            _buildProjectsService
                .Setup(_ => _.GetBuildProjectForSpecificationId(specificationId))
                .ReturnsAsync(new Models.Calcs.BuildProject { DatasetRelationships = datasetRelationships });
        }

        private void AndTheDatasetReferencesForTheCalculations(IEnumerable<Calculation> calculations,
            List<Models.Calcs.DatasetRelationshipSummary> datasetRelationships,
            params DatasetReference[] references)
        {
            _datasetReferenceService
                .Setup(_ => _.GetDatasetRelationShips(calculations, datasetRelationships))
                .Returns(references);
        }
    }
}