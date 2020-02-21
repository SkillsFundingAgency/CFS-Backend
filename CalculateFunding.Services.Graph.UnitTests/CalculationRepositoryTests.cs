using CalculateFunding.Common.Graph.Interfaces;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Graph.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Graph.UnitTests
{
    [TestClass]
    public class CalculationRepositoryTests : GraphRepositoryTestBase
    {
        private const string CalculationId = "calculationid";
        private const string SpecificationId = "specificationid";
        private const string CalculationSpecificationRelationship = "BelongsToSpecification";
        private const string SpecificationCalculationRelationship = "HasCalculation";
        private const string CalculationACalculationBRelationship = "CallsCalculation";
        private const string CalculationBCalculationARelationship = "CalledByCalculation";
        private IGraphRepository _graphRepository;
        private ICalculationRepository _calculationRepository;

        [TestInitialize]
        public void Setup()
        {
            _graphRepository = Substitute.For<IGraphRepository>();
            _calculationRepository = new CalculationRepository(_graphRepository);
        }

        [TestMethod]
        public async Task UpsertCalculations_GivenValidCalculations_ExpectedMethodsCalled()
        {
            Calculation[] calcs = new Calculation[] { NewCalculation(), NewCalculation() };

            await _calculationRepository.UpsertCalculations(calcs);

            await _graphRepository
                .Received(1)
                .UpsertNodes(Arg.Is<IEnumerable<Calculation>>(_ => _.ToArray().All(calc => calcs.Contains(calc))),
                    Arg.Is<IEnumerable<string>>(_ => _.All(rel => rel == CalculationId) && _.Count()==1));
        }

        [TestMethod]
        public async Task DeleteCalculation_GivenValidCalculation_ExpectedMethodsCalled()
        {
            Calculation calc = NewCalculation();

            await _calculationRepository.DeleteCalculation(calc.CalculationId);

            await _graphRepository
                .Received(1)
                .DeleteNode<Calculation>(CalculationId, Arg.Is(calc.CalculationId));
        }

        [TestMethod]
        public async Task UpsertCalculationSpecificationRelationship_GivenValidSpecificationAndCalculation_ExpectedMethodsCalled()
        {
            Calculation calculation = NewCalculation();
            Specification specification = NewSpecification();

            await _calculationRepository.UpsertCalculationSpecificationRelationship(calculation.CalculationId,
                specification.SpecificationId);

            await _graphRepository
                .Received(1)
                .UpsertRelationship<Calculation, Specification>(CalculationSpecificationRelationship,
                    Arg.Is((CalculationId, calculation.CalculationId)),
                    Arg.Is((SpecificationId, specification.SpecificationId)));

            await _graphRepository
                .Received(1)
                .UpsertRelationship<Specification, Calculation>(SpecificationCalculationRelationship,
                    Arg.Is((SpecificationId, specification.SpecificationId)),
                    Arg.Is((CalculationId, calculation.CalculationId)));
        }

        [TestMethod]
        public async Task UpsertCalculationCalculationRelationship_GivenValidCalculationAndCalculation_ExpectedMethodsCalled()
        {
            Calculation calculationA = NewCalculation();
            Calculation calculationB = NewCalculation();

            await _calculationRepository.UpsertCalculationCalculationRelationship(calculationA.CalculationId,
                calculationB.CalculationId);

            await _graphRepository
                .Received(1)
                .UpsertRelationship<Calculation, Calculation>(CalculationACalculationBRelationship,
                    Arg.Is((CalculationId, calculationA.CalculationId)),
                    Arg.Is((CalculationId, calculationB.CalculationId)));

            await _graphRepository
                .Received(1)
                .UpsertRelationship<Calculation, Calculation>(CalculationBCalculationARelationship,
                    Arg.Is((CalculationId, calculationB.CalculationId)),
                    Arg.Is((CalculationId, calculationA.CalculationId)));
        }

        [TestMethod]
        public async Task DeleteCalculationSpecificationRelationship_GivenValidCalculationAndSpecification_ExpectedMethodsCalled()
        {
            Calculation calculation = NewCalculation();
            Specification specification = NewSpecification();

            await _calculationRepository.DeleteCalculationSpecificationRelationship(calculation.CalculationId,
                specification.SpecificationId);

            await _graphRepository
                .Received(1)
                .DeleteRelationship<Calculation, Specification>(CalculationSpecificationRelationship,
                    Arg.Is((CalculationId, calculation.CalculationId)),
                    Arg.Is((SpecificationId, specification.SpecificationId)));

            await _graphRepository
                .Received(1)
                .DeleteRelationship<Specification, Calculation>(SpecificationCalculationRelationship,
                    Arg.Is((SpecificationId, specification.SpecificationId)),
                    Arg.Is((CalculationId, calculation.CalculationId)));
        }

        [TestMethod]
        public async Task DeleteCalculationCalculationRelationship_GivenValidCalculationAndCalculation_ExpectedMethodsCalled()
        {
            Calculation calculationA = NewCalculation();
            Calculation calculationB = NewCalculation();

            await _calculationRepository.DeleteCalculationCalculationRelationship(calculationA.CalculationId,
                calculationB.CalculationId);

            await _graphRepository
                .Received(1)
                .DeleteRelationship<Calculation, Calculation>(CalculationACalculationBRelationship,
                    Arg.Is((CalculationId, calculationA.CalculationId)),
                    Arg.Is((CalculationId, calculationB.CalculationId)));

            await _graphRepository
                .Received(1)
                .DeleteRelationship<Calculation, Calculation>(CalculationBCalculationARelationship,
                    Arg.Is((CalculationId, calculationB.CalculationId)),
                    Arg.Is((CalculationId, calculationA.CalculationId)));
        }
    }
}
