using CalculateFunding.Common.ApiClient.Graph;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using GraphModels = CalculateFunding.Common.ApiClient.Graph.Models;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using Serilog;
using System.Linq;
using FluentAssertions;
using CalculateFunding.Common.ApiClient.Models;
using System.Net;
using Newtonsoft.Json.Linq;
using CalculateFunding.Tests.Common.Helpers;
using GraphCalculation = CalculateFunding.Common.ApiClient.Graph.Models.Calculation;
using GraphEntity = CalculateFunding.Common.ApiClient.Graph.Models.Entity<CalculateFunding.Common.ApiClient.Graph.Models.Calculation>;
using CalculationEntity = CalculateFunding.Models.Graph.Entity<CalculateFunding.Common.ApiClient.Graph.Models.Calculation, CalculateFunding.Common.ApiClient.Graph.Models.Relationship>;
using GraphRelationship = CalculateFunding.Common.ApiClient.Graph.Models.Relationship;
using DatasetReference = CalculateFunding.Models.Graph.DatasetReference;
using AutoMapper;

namespace CalculateFunding.Services.Calcs.UnitTests.Services
{
    [TestClass]
    public class GraphRepositoryTests
    {
        ICalculationsFeatureFlag _calculationsFeatureFlag;
        IGraphApiClient _graphApiClient;
        IGraphRepository _graphRepository;

        [TestInitialize]
        public void SetUp()
        {
            _calculationsFeatureFlag = Substitute.For<ICalculationsFeatureFlag>();
            _graphApiClient = Substitute.For<IGraphApiClient>();
            ResiliencePolicies resiliencePolicies = new ResiliencePolicies { GraphApiClientPolicy = Polly.Policy.NoOpAsync() };
            _graphRepository = new GraphRepository(_graphApiClient, resiliencePolicies, _calculationsFeatureFlag);
        }

        [TestMethod]
        public async Task GetCircularDependencies_WhenFeatureNotEnabledAndSpecificationSuppliedWithCalculation_NoCircularDependenciesReturned()
        {
            SpecificationSummary specificationSummary = NewSpecification();
            
            _calculationsFeatureFlag
                            .IsGraphEnabled()
                .Returns(false);

            IEnumerable<CalculationEntity> circularDependencies = await _graphRepository.GetCircularDependencies(specificationSummary.Id);

            circularDependencies
                .IsNullOrEmpty()
                .Should()
                .Be(true);
        }

        [TestMethod]
        public async Task GetCircularDependencies_WhenFeatureEnabledAndSpecificationSuppliedWithCalculationWithNoCircularDependencies_NoCircularDependenciesReturned()
        {
            SpecificationSummary specificationSummary = NewSpecification();

            _graphApiClient
                .GetCircularDependencies(specificationSummary.Id)
                .Returns(new ApiResponse<IEnumerable<GraphEntity>>(HttpStatusCode.OK));

            _calculationsFeatureFlag
                .IsGraphEnabled()
                .Returns(false);

            IEnumerable<CalculationEntity> circularDependencies = await _graphRepository.GetCircularDependencies(specificationSummary.Id);

            circularDependencies
                .IsNullOrEmpty()
                .Should()
                .Be(true);
        }

        [TestMethod]
        public async Task GetCircularDependencies_WhenFeatureEnabledAndSpecificationSuppliedWithCalculationWithCircularDependencies_CircularDependenciesReturned()
        {
            SpecificationSummary specificationSummary = NewSpecification(_ => _.WithId("ebc5153d-0a3b-44aa-ab4b-aea405cb0df0"));

            GraphCalculation calculation1 = NewGraphCalculation(_ => _.WithSpecificationId(specificationSummary.Id)
            .WithId("41402e30-f8a7-40bc-a4fc-4cd256e2fbeb"));

            GraphCalculation calculation2 = NewGraphCalculation(_ => _.WithSpecificationId(specificationSummary.Id));

            GraphRelationship calculation1Relationship = Substitute.For<GraphRelationship>();
            calculation1Relationship.One = calculation1;
            calculation1Relationship.Two = calculation2;

            GraphRelationship calculation2Relationship = Substitute.For<GraphRelationship>();
            calculation2Relationship.One = calculation2;
            calculation2Relationship.Two = calculation1;


            GraphEntity entity = new GraphEntity { Node = calculation1, Relationships = new[] { calculation1Relationship, calculation2Relationship } };

            _calculationsFeatureFlag
                .IsGraphEnabled()
                .Returns(true);

            _graphApiClient
                .GetCircularDependencies(Arg.Is(specificationSummary.Id))
                .Returns(new ApiResponse<IEnumerable<GraphEntity>>(HttpStatusCode.OK, new[] { entity }));

            IEnumerable<CalculationEntity> circularDependencies = await _graphRepository.GetCircularDependencies(specificationSummary.Id);

            circularDependencies
                .IsNullOrEmpty()
                .Should()
                .Be(false);

            circularDependencies
                .Where(_ => _.Node.CalculationId == calculation1.CalculationId)
                .FirstOrDefault()
                .Relationships
                .Count()
                .Should()
                .Be(2);
        }

        protected static SpecificationSummary NewSpecification(Action<SpecificationSummaryBuilder> setUp = null)
        {
            SpecificationSummaryBuilder specificationSummaryBuilder = new SpecificationSummaryBuilder();

            setUp?.Invoke(specificationSummaryBuilder);

            return specificationSummaryBuilder.Build();
        }

        protected static Calculation NewCalculation(Action<CalculationBuilder> setUp = null)
        {
            CalculationBuilder calculationBuilder = new CalculationBuilder();

            setUp?.Invoke(calculationBuilder);

            return calculationBuilder.Build();
        }

        protected static GraphCalculation NewGraphCalculation(Action<GraphCalculationBuilder> setUp = null)
        {
            GraphCalculationBuilder graphCalculationBuilder = new GraphCalculationBuilder();

            setUp?.Invoke(graphCalculationBuilder);

            return graphCalculationBuilder.Build();
        }

        protected static CalculationVersion NewCalculationVersion(Action<CalculationVersionBuilder> setUp = null)
        {
            CalculationVersionBuilder calculationVersionBuilder = new CalculationVersionBuilder();

            setUp?.Invoke(calculationVersionBuilder);

            return calculationVersionBuilder.Build();
        }

        public string GetResourceString(string resourceName)
        {
            return typeof(GraphRepositoryTests)
                .Assembly
                .GetEmbeddedResourceFileContents(resourceName);
        }

        protected static DatasetReference NewDataSetReference(Action<DatasetReferenceBuilder> setUp = null)
        {
            DatasetReferenceBuilder datasetReferenceBuilder = new DatasetReferenceBuilder();

            setUp?.Invoke(datasetReferenceBuilder);

            return datasetReferenceBuilder.Build();
        }
    }
}
