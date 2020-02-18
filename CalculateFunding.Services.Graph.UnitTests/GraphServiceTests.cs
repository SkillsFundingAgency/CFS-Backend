using CalculateFunding.Common.Graph.Interfaces;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Graph.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.Threading.Tasks;
using Serilog;
using Neo4jDriver = Neo4j.Driver;

namespace CalculateFunding.Services.Graph.UnitTests
{
    [TestClass]
    public class GraphServiceTests
    {
        private ICalculationRepository _calculationRepository;
        private ISpecificationRepository _specificationRepository;
        private IGraphService _graphService;
        private ILogger _logger;

        [TestInitialize]
        public void SetupTest()
        {
            _logger = Substitute.For<ILogger>();
            _calculationRepository = Substitute.For<ICalculationRepository>();
            _specificationRepository = Substitute.For<ISpecificationRepository>();
            _graphService = new GraphService(_logger, _calculationRepository, _specificationRepository);
        }

        [TestMethod]
        public async Task SaveCalculations_GivenValidCalulations_OkStatusCodeReturned()
        {
            Calculation[] calcs = new Calculation[] { NewCalculation(), NewCalculation() };

            IActionResult result = await _graphService.UpsertCalculations(calcs);

            await _calculationRepository
                .Received(1)
                .UpsertCalculations(calcs);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task SaveCalculations_FailsToAddCalculations_InternalServerErrorReturned()
        {
            Calculation[] calcs = new Calculation[] { NewCalculation(), NewCalculation() };

            _calculationRepository
                .UpsertCalculations(calcs)
                .Throws(new Neo4jDriver.Neo4jException());

            IActionResult result = await _graphService.UpsertCalculations(calcs);

            result
                .Should()
                .BeAssignableTo<InternalServerErrorResult>();
        }

        [TestMethod]
        public async Task SaveSpecifications_GivenValidSpecifications_OkStatusCodeReturned()
        {
            Specification[] specifications = new Specification[] { NewSpecification(), NewSpecification() };

            IActionResult result = await _graphService.UpsertSpecifications(specifications);

            await _specificationRepository
                .Received(1)
                .UpsertSpecifications(specifications);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task SaveSpecifications_FailsToAddSpecifications_InternalServerErrorReturned()
        {
            Specification[] specifications = new Specification[] { NewSpecification(), NewSpecification() };

            _specificationRepository
                .UpsertSpecifications(specifications)
                .Throws(new Neo4jDriver.Neo4jException());

            IActionResult result = await _graphService.UpsertSpecifications(specifications);

            _logger
                .Received(1)
                .Error($"Save specifications failed for specifications:'{specifications.AsJson()}'");

            result
                .Should()
                .BeAssignableTo<InternalServerErrorResult>();
        }

        [TestMethod]
        public async Task DeleteCalculation_GivenExistingCalculation_OkStatusCodeReturned()
        {
            Calculation calc = NewCalculation();

            IActionResult result = await _graphService.DeleteCalculation(calc.CalculationId);

            await _calculationRepository
                .Received(1)
                .DeleteCalculation(calc.CalculationId);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task DeleteCalculation_FailsToDeleteCalculation_InternalServerErrorReturned()
        {
            Calculation calc = NewCalculation();

            _calculationRepository
                .DeleteCalculation(calc.CalculationId)
                .Throws(new Neo4jDriver.Neo4jException());

            IActionResult result = await _graphService.DeleteCalculation(calc.CalculationId);

            _logger
                .Received(1)
                .Error($"Delete calculation failed for calculation:'{calc.CalculationId}'");

            result
                .Should()
                .BeAssignableTo<InternalServerErrorResult>();
        }

        [TestMethod]
        public async Task DeleteSpecification_GivenExistingSpecification_OkStatusCodeReturned()
        {
            Specification specification = NewSpecification();

            IActionResult result = await _graphService.DeleteSpecification(specification.SpecificationId);

            await _specificationRepository
                .Received(1)
                .DeleteSpecification(specification.SpecificationId);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task DeleteSpecification_FailsToDeleteSpecification_InternalServerErrorReturned()
        {
            Specification specification = NewSpecification();

            _specificationRepository
                .DeleteSpecification(specification.SpecificationId)
                .Throws(new Neo4jDriver.Neo4jException());

            IActionResult result = await _graphService.DeleteSpecification(specification.SpecificationId);

            _logger
                .Received(1)
                .Error($"Delete specification failed for specification:'{specification.SpecificationId}'");

            result
                .Should()
                .BeAssignableTo<InternalServerErrorResult>();
        }

        [TestMethod]
        public async Task CreateCalculationSpecificationRelationship_GivenValidRelationship_OkStatusCodeReturned()
        {
            Calculation calc = NewCalculation();
            Specification specification = NewSpecification();

            IActionResult result = await _graphService.UpsertCalculationSpecificationRelationship(calc.CalculationId,
                specification.SpecificationId);

            await _calculationRepository
                .Received(1)
                .UpsertCalculationSpecificationRelationship(calc.CalculationId, specification.SpecificationId);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task CreateCalculationSpecificationRelationship_FailedToCreateRelationship_InternalServerErrorReturned()
        {
            Calculation calc = NewCalculation();
            Specification specification = NewSpecification();

            _calculationRepository
                .UpsertCalculationSpecificationRelationship(calc.CalculationId, specification.SpecificationId)
                .Throws(new Neo4jDriver.Neo4jException());

            IActionResult result = await _graphService.UpsertCalculationSpecificationRelationship(calc.CalculationId,
                specification.SpecificationId);

            _logger
                .Received(1)
                .Error($"Upsert calculation relationship between specification failed for calculation:'{calc.CalculationId}'" +
                    $" and specification:'{specification.SpecificationId}'");

            result
                .Should()
                .BeAssignableTo<InternalServerErrorResult>();
        }

        [TestMethod]
        public async Task CreateCalculationCalculationRelationship_GivenValidRelationship_OkStatusCodeReturned()
        {
            Calculation calcA = NewCalculation();
            Calculation calcB = NewCalculation();

            IActionResult result = await _graphService.UpsertCalculationCalculationRelationship(calcA.CalculationId,
                calcB.CalculationId);

            await _calculationRepository
                .Received(1)
                .UpsertCalculationCalculationRelationship(calcA.CalculationId, calcB.CalculationId);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task CreateCalculationCalculationRelationship_FailedToCreateRelationship_InternalServerErrorReturned()
        {
            Calculation calcA = NewCalculation();
            Calculation calcB = NewCalculation();

            _calculationRepository
                .UpsertCalculationCalculationRelationship(calcA.CalculationId, calcB.CalculationId)
                .Throws(new Neo4jDriver.Neo4jException());

            IActionResult result = await _graphService.UpsertCalculationCalculationRelationship(calcA.CalculationId,
                calcB.CalculationId);

            _logger
                .Received(1)
                .Error($"Upsert calculation relationship call to calculation failed for calculation:'{calcA.CalculationId}'" +
                    $" calling calculation:'{calcB.CalculationId}'");

            result
                .Should()
                .BeAssignableTo<InternalServerErrorResult>();
        }

        [TestMethod]
        public async Task CreateCalculationCalculationsRelationships_FailedToCreateRelationships_InternalServerErrorReturned()
        {
            Calculation calcA = NewCalculation();
            Calculation calcB = NewCalculation();
            Calculation calcC = NewCalculation();

            _calculationRepository
                .UpsertCalculationCalculationRelationship(calcA.CalculationId, calcB.CalculationId)
                .Throws(new Neo4jDriver.Neo4jException());

            IActionResult result = await _graphService.UpsertCalculationCalculationsRelationships(calcA.CalculationId,
                new[] {calcB.CalculationId,
                calcC.CalculationId});

            _logger
                .Received(1)
                .Error($"Upsert calculation relationship call to calculation failed for calculation:'{calcA.CalculationId}'" +
                    $" calling calculations:'{new[] { calcB.CalculationId, calcC.CalculationId}.AsJson()}'");

            result
                .Should()
                .BeAssignableTo<InternalServerErrorResult>();
        }

        [TestMethod]
        public async Task DeleteCalculationSpecificationRelationship_GivenValidRelationship_OkStatusCodeReturned()
        {
            Calculation calc = NewCalculation();
            Specification specification = NewSpecification();

            IActionResult result = await _graphService.DeleteCalculationSpecificationRelationship(calc.CalculationId, specification.SpecificationId);

            await _calculationRepository
                .Received(1)
                .DeleteCalculationSpecificationRelationship(calc.CalculationId, specification.SpecificationId);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task DeleteCalculationSpecificationRelationship_FailedToDeleteRelationship_InternalServerErrorReturned()
        {
            Calculation calc = NewCalculation();
            Specification specification = NewSpecification();

            _calculationRepository
                .DeleteCalculationSpecificationRelationship(calc.CalculationId, specification.SpecificationId)
                .Throws(new Neo4jDriver.Neo4jException());

            IActionResult result = await _graphService.DeleteCalculationSpecificationRelationship(calc.CalculationId, specification.SpecificationId);

            _logger
                .Received(1)
                .Error($"Delete calculation relationship between specification failed for calculation:'{calc.CalculationId}'" +
                    $" and specification:'{specification.SpecificationId}'");

            result
                .Should()
                .BeAssignableTo<InternalServerErrorResult>();
        }

        [TestMethod]
        public async Task DeleteCalculationCalculationRelationship_GivenValidRelationship_OkStatusCodeReturned()
        {
            Calculation calcA = NewCalculation();
            Calculation calcB = NewCalculation();

            IActionResult result = await _graphService.DeleteCalculationCalculationRelationship(calcA.CalculationId, calcB.CalculationId);

            await _calculationRepository
                .Received(1)
                .DeleteCalculationCalculationRelationship(calcA.CalculationId, calcB.CalculationId);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task DeleteCalculationCalculationRelationship_FailedToDeleteRelationship_InternalServerErrorReturned()
        {
            Calculation calcA = NewCalculation();
            Calculation calcB = NewCalculation();

            _calculationRepository
                .DeleteCalculationCalculationRelationship(calcA.CalculationId, calcB.CalculationId)
                .Throws(new Neo4jDriver.Neo4jException());

            IActionResult result = await _graphService.DeleteCalculationCalculationRelationship(calcA.CalculationId, calcB.CalculationId);

            _logger
                .Received(1)
                .Error($"Delete calculation relationship call to calculation failed for calculation:'{calcA.CalculationId}'" +
                    $" calling calculation:'{calcB.CalculationId}'");
            result
                .Should()
                .BeAssignableTo<InternalServerErrorResult>();
        }

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
    }
}
