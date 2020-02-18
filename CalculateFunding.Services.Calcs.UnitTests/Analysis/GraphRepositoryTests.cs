using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Graph;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Calcs.Analysis;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog;
using ApiSpecification = CalculateFunding.Common.ApiClient.Graph.Models.Specification;
using ApiCalculation = CalculateFunding.Common.ApiClient.Graph.Models.Calculation;

namespace CalculateFunding.Services.Calcs.UnitTests.Analysis
{
    [TestClass]
    public class GraphRepositoryTests : GraphTestBase
    {
        private GraphRepository _repository;
        private Mock<IGraphApiClient> _graphApiClient;

        [TestInitialize]
        public void SetUp()
        {
            _graphApiClient = new Mock<IGraphApiClient>();

            _repository = new GraphRepository(_graphApiClient.Object,
                new ResiliencePolicies
                {
                    GraphApiClientPolicy = Policy.NoOpAsync()
                },
                Mapper.Object,
                new Mock<ILogger>().Object);
        }

        [TestMethod]
        public void GuardsAgainstNoSpecificationCalculationRelationshipsBeingSupplied()
        {
            Func<Task> invocation = () => WhenTheGraphIsRecreated(null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be("specificationCalculationRelationships");
        }

        [TestMethod]
        public async Task DeletesThenInsertsGraphForSpecification()
        {
            string specificationId = NewRandomString();

            string calculationIdOne = NewRandomString();
            string calculationIdTwo = NewRandomString();
            string calculationIdThree = NewRandomString();
            string calculationIdFour = NewRandomString();

            Specification specification = NewGraphSpecification(_ => _.WithId(specificationId));
            Calculation[] calculations = new[]
            {
                NewGraphCalculation(_ => _.WithId(calculationIdOne)),
                NewGraphCalculation(_ => _.WithId(calculationIdTwo)),
                NewGraphCalculation(_ => _.WithId(calculationIdThree)),
                NewGraphCalculation(_ => _.WithId(calculationIdFour)),
            };
            CalculationRelationship[] calculationRelationships = new[]
            {
                NewCalculationRelationship(_ => _.WithCalculationOneId(calculationIdTwo)
                    .WithCalculationTwoId(calculationIdOne)),
                NewCalculationRelationship(_ => _.WithCalculationOneId(calculationIdThree)
                    .WithCalculationTwoId(calculationIdTwo)),
                NewCalculationRelationship(_ => _.WithCalculationOneId(calculationIdFour)
                    .WithCalculationTwoId(calculationIdThree)),
                NewCalculationRelationship(_ => _.WithCalculationOneId(calculationIdFour)
                    .WithCalculationTwoId(calculationIdThree))
            };

            SpecificationCalculationRelationships specificationCalculationRelationships = NewSpecificationCalculationRelationships(_ =>
                _.WithSpecification(specification)
                    .WithCalculations(calculations)
                    .WithCalculationRelationships(calculationRelationships));

            ApiSpecification apiSpecification = NewApiSpecification();
            ApiCalculation[] apiCalculations = new[]
            {
                NewApiCalculation(),
                NewApiCalculation(),
                NewApiCalculation()
            };
            
            GivenTheMapping(specification, apiSpecification);
            AndTheCollectionMapping(calculations, apiCalculations);
            AndTheSpecificationIsDeleted(specificationId);
            AndTheSpecificationIsCreated(apiSpecification);
            AndTheCalculationsAreCreated(apiCalculations);
            AndTheSpecificationCalculationRelationshipsWereCreated(specificationId, new []
            {
                calculationIdOne,
                calculationIdTwo,
                calculationIdThree,
                calculationIdFour
            });
            AndTheRelationshipsWereCreated(calculationRelationships);
            
            await WhenTheGraphIsRecreated(specificationCalculationRelationships);

            _graphApiClient.VerifyAll();
        }

        private void AndTheSpecificationIsDeleted(string specificationId)
        {
            _graphApiClient.Setup(_ => _.DeleteAllForSpecification(specificationId))
                .ReturnsAsync(HttpStatusCode.OK)
                .Verifiable();
        }

        private void AndTheSpecificationIsCreated(ApiSpecification specification)
        {
            _graphApiClient.Setup(_ => _.UpsertSpecifications(It.Is<ApiSpecification[]>(specs =>
                    specs.SequenceEqual(new [] { specification }))))
                .ReturnsAsync(HttpStatusCode.OK)
                .Verifiable();
        }

        private void AndTheCalculationsAreCreated(params ApiCalculation[] calculations)
        {
            _graphApiClient.Setup(_ => _.UpsertCalculations(It.Is<ApiCalculation[]>(calcs =>
                    calculations.SequenceEqual(calcs))))
                .ReturnsAsync(HttpStatusCode.OK)
                .Verifiable();
        }

        private void AndTheRelationshipsWereCreated(CalculationRelationship[] calculationRelationships)
        {
            IEnumerable<IGrouping<string, CalculationRelationship>> relationshipsPerCalculation =
                calculationRelationships.GroupBy(_ => _.CalculationOneId);

            foreach (IGrouping<string, CalculationRelationship> relationships in relationshipsPerCalculation)
            {
                _graphApiClient.Setup(_ => _.UpsertCalculationCalculationsRelationships(relationships.Key, It.Is<string[]>(calcs =>
                        calcs.SequenceEqual(relationships.Select(rel => rel.CalculationTwoId).ToArray()))))
                    .ReturnsAsync(HttpStatusCode.OK)
                    .Verifiable();
            }
        }

        private void AndTheSpecificationCalculationRelationshipsWereCreated(string specificationId, string[] calculationIds)
        {
            foreach (string calculationId in calculationIds)
            {
                _graphApiClient.Setup(_ => _.UpsertCalculationSpecificationRelationship(calculationId, specificationId))
                    .ReturnsAsync(HttpStatusCode.OK)
                    .Verifiable();
            }
        }
        
        private async Task WhenTheGraphIsRecreated(SpecificationCalculationRelationships specificationCalculationRelationships)
        {
            await _repository.RecreateGraph(specificationCalculationRelationships);
        }

        private ApiSpecification NewApiSpecification()
        {
            return new GraphApiSpecificationBuilder()
                .Build();
        }

        private ApiCalculation NewApiCalculation()
        {
            return new GraphApiCalculationBuilder()
                .Build();
        }
    }
}