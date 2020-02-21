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
    public class SpecificationRepositoryTests : GraphRepositoryTestBase
    {
        private const string CalculationId = "calculationid";
        private const string SpecificationId = "specificationid";
        private IGraphRepository _graphRepository;
        private ISpecificationRepository _specificationRepository;

        [TestInitialize]
        public void Setup()
        {
            _graphRepository = Substitute.For<IGraphRepository>();
            _specificationRepository = new SpecificationRepository(_graphRepository);
        }

        [TestMethod]
        public async Task UpsertSpecifications_GivenValidSpecifications_ExpectedMethodsCalled()
        {
            Specification[] specs = new Specification[] { NewSpecification(), NewSpecification() };

            await _specificationRepository.UpsertSpecifications(specs);

            await _graphRepository
                .Received(1)
                .UpsertNodes(Arg.Is<IEnumerable<Specification>>(_ => _.ToArray().All(spec => specs.Contains(spec))),
                    Arg.Is<IEnumerable<string>>(_ => _.All(rel => rel == SpecificationId) && _.Count() == 1));
        }

        [TestMethod]
        public async Task DeleteCalculation_GivenValidCalculation_ExpectedMethodsCalled()
        {
            Specification spec = NewSpecification();

            await _specificationRepository.DeleteSpecification(spec.SpecificationId);

            await _graphRepository
                .Received(1)
                .DeleteNode<Specification>(SpecificationId, Arg.Is(spec.SpecificationId));
        }

        [TestMethod]
        public async Task DeleteAllForSpecification_GivenValidCalculation_ExpectedMethodsCalled()
        {
            Specification spec = NewSpecification();

            await _specificationRepository.DeleteAllForSpecification(spec.SpecificationId);

            await _graphRepository
                .Received(1)
                .DeleteNodeAndChildNodes<Specification>(SpecificationId, Arg.Is(spec.SpecificationId));
        }
    }
}
