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

namespace CalculateFunding.Services.Graph.UnitTests
{
    [TestClass]
    public class GraphServiceTests
    {
        private ICalculationRepository _calculationRepository;
        private ISpecificationRepository _specificationRepository;
        private IGraphService _graphService;

        [TestInitialize]
        public void SetupTest()
        {
            _calculationRepository = Substitute.For<ICalculationRepository>();
            _specificationRepository = Substitute.For<ISpecificationRepository>();
            _graphService = new GraphService(_calculationRepository, _specificationRepository);
        }

        [TestMethod]
        public async Task SaveCalculations_GivenValidCalulations_OkStatusCodeReturned()
        {
            Calculation[] calcs = new Calculation[] { NewCalculation(), NewCalculation() };

            IActionResult result = await _graphService.SaveCalculations(calcs);

            await _calculationRepository
                .Received(1)
                .SaveCalculations(calcs);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task SaveCalculations_FailsToAddCalculations_InternalServerErrorReturned()
        {
            Calculation[] calcs = new Calculation[] { NewCalculation(), NewCalculation() };

            _calculationRepository
                .SaveCalculations(calcs)
                .Throws(new Exception());

            IActionResult result = await _graphService.SaveCalculations(calcs);

            result
                .Should()
                .BeAssignableTo<InternalServerErrorResult>();
        }

        [TestMethod]
        public async Task SaveSpecifications_GivenValidSpecifications_OkStatusCodeReturned()
        {
            Specification[] specifications = new Specification[] { NewSpecification(), NewSpecification() };

            IActionResult result = await _graphService.SaveSpecifications(specifications);

            await _specificationRepository
                .Received(1)
                .SaveSpecifications(specifications);

            result
                .Should()
                .BeOfType<OkResult>();
        }



        [TestMethod]
        public async Task SaveSpecifications_FailsToAddSpecifications_InternalServerErrorReturned()
        {
            Specification[] specifications = new Specification[] { NewSpecification(), NewSpecification() };

            _specificationRepository
                .SaveSpecifications(specifications)
                .Throws(new Exception());

            IActionResult result = await _graphService.SaveSpecifications(specifications);

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
                .Throws(new Exception());

            IActionResult result = await _graphService.DeleteCalculation(calc.CalculationId);

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
                .Throws(new Exception());

            IActionResult result = await _graphService.DeleteSpecification(specification.SpecificationId);

            result
                .Should()
                .BeAssignableTo<InternalServerErrorResult>();
        }

        [TestMethod]
        public async Task CreateCalculationRelationship_GivenValidRelationship_OkStatusCodeReturned()
        {
            Calculation calc = NewCalculation();
            Specification specification = NewSpecification();

            IActionResult result = await _graphService.CreateCalculationRelationship(calc.CalculationId, specification.SpecificationId);

            await _calculationRepository
                .Received(1)
                .CreateCalculationRelationship(calc.CalculationId, specification.SpecificationId);

            result
                .Should()
                .BeOfType<OkResult>();
        }

        [TestMethod]
        public async Task CreateCalculationRelationship_FailedToCreateRelationship_InternalServerErrorReturned()
        {
            Calculation calc = NewCalculation();
            Specification specification = NewSpecification();

            _calculationRepository
                .CreateCalculationRelationship(calc.CalculationId, specification.SpecificationId)
                .Throws(new Exception());

            IActionResult result = await _graphService.CreateCalculationRelationship(calc.CalculationId, specification.SpecificationId);

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
