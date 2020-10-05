using CalculateFunding.Common.Graph;
using CalculateFunding.Common.Graph.Interfaces;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Graph.Constants;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Graph.UnitTests
{
    [TestClass]
    public class CalculationRepositoryTests : GraphRepositoryTestBase
    {
        private CalculationRepository _calculationRepository;

        [TestInitialize]
        public void Setup()
        {
            _calculationRepository = new CalculationRepository(GraphRepository);
        }

        [TestMethod]
        public async Task UpsertCalculations_GivenValidCalculations_ExpectedMethodsCalled()
        {
            Calculation[] calculations = new[] { NewCalculation(), NewCalculation() };

            await _calculationRepository.UpsertCalculations(calculations);

            await ThenTheNodesWereCreated(calculations, AttributeConstants.CalculationId);
        }

        [TestMethod]
        public async Task DeleteCalculation_GivenValidCalculation_ExpectedMethodsCalled()
        {
            string calculationId = NewRandomString();

            await _calculationRepository.DeleteCalculation(calculationId);

            await ThenTheNodeWasDeleted<Calculation>(AttributeConstants.CalculationId, calculationId);
        }

        [TestMethod]
        public async Task UpsertCalculationSpecificationRelationship_GivenValidSpecificationAndCalculation_ExpectedMethodsCalled()
        {
            string calculationId = NewRandomString();
            string specificationId = NewRandomString();
            
            await _calculationRepository.UpsertCalculationSpecificationRelationship(calculationId,
                specificationId);

            await ThenTheRelationshipWasCreated<Calculation, Specification>(AttributeConstants.CalculationSpecificationRelationshipId,
                    (AttributeConstants.CalculationId, calculationId),
                    (AttributeConstants.SpecificationId, specificationId));

            await AndTheRelationshipWasCreated<Specification, Calculation>(AttributeConstants.SpecificationCalculationRelationshipId,
                    (AttributeConstants.SpecificationId, specificationId),
                    (AttributeConstants.CalculationId, calculationId));
        }

        [TestMethod]
        public async Task GetCalculationCircularDependencies_GivenCircularDependencies_ExpectedMethodsCalled()
        { 
            string specificationId = NewRandomString();

            Calculation calculation1 = NewCalculation();
            Calculation calculation2 = NewCalculation();

            Entity<Calculation> entity = new Entity<Calculation> { Node = calculation1, Relationships = new[] { new Relationship { One = calculation2, Two = calculation1, Type = AttributeConstants.CalculationACalculationBRelationship } } };

            GivenCircularDependencies(AttributeConstants.CalculationACalculationBRelationship, AttributeConstants.CalculationId, specificationId, entity);

            IEnumerable<Entity<Calculation, IRelationship>> entities = await _calculationRepository.GetCalculationCircularDependencies(specificationId);

            entities
                .Should()
                .HaveCount(1);

            entities
                .FirstOrDefault()
                .Node
                .Should()
                .BeEquivalentTo(calculation1);

            entities
                .FirstOrDefault()
                .Relationships
                .Should()
                .HaveCount(1);
        }

        [TestMethod]
        public async Task UpsertCalculationCalculationRelationship_GivenValidCalculationAndCalculation_ExpectedMethodsCalled()
        {
            string calculationAId = NewRandomString();
            string calculationBId = NewRandomString();
            
            await _calculationRepository.UpsertCalculationCalculationRelationship(calculationAId,
                calculationBId);

            await ThenTheRelationshipWasCreated<Calculation, Calculation>(AttributeConstants.CalculationACalculationBRelationship,
                (AttributeConstants.CalculationId, calculationAId),
                (AttributeConstants.CalculationId, calculationBId));

            await AndTheRelationshipWasCreated<Calculation, Calculation>(AttributeConstants.CalculationBCalculationARelationship,
                (AttributeConstants.CalculationId, calculationBId),
                (AttributeConstants.CalculationId, calculationAId));
        }

        [TestMethod]
        public async Task DeleteCalculationSpecificationRelationship_GivenValidCalculationAndSpecification_ExpectedMethodsCalled()
        {
            string calculationId = NewRandomString();
            string specificationId = NewRandomString();
            
            await _calculationRepository.DeleteCalculationSpecificationRelationship(calculationId,
                specificationId);

            await ThenTheRelationshipWasDeleted<Calculation, Specification>(AttributeConstants.CalculationSpecificationRelationshipId,
                    (AttributeConstants.CalculationId, calculationId),
                    (AttributeConstants.SpecificationId, specificationId));

            await AndTheRelationshipWasDeleted<Specification, Calculation>(AttributeConstants.SpecificationCalculationRelationshipId,
                    (AttributeConstants.SpecificationId, specificationId),
                    (AttributeConstants.CalculationId, calculationId));
        }

        [TestMethod]
        public async Task DeleteCalculationCalculationRelationship_GivenValidCalculationAndCalculation_ExpectedMethodsCalled()
        {
            string calculationAId = NewRandomString();
            string calculationBId = NewRandomString();
            
            await _calculationRepository.DeleteCalculationCalculationRelationship(calculationAId,
                calculationBId);

            await ThenTheRelationshipWasDeleted<Calculation, Calculation>(AttributeConstants.CalculationACalculationBRelationship,
                (AttributeConstants.CalculationId, calculationAId),
                (AttributeConstants.CalculationId, calculationBId));

            await AndTheRelationshipWasDeleted<Calculation, Calculation>(AttributeConstants.CalculationBCalculationARelationship,
                (AttributeConstants.CalculationId, calculationBId),
                (AttributeConstants.CalculationId, calculationAId));
        }

        [TestMethod]
        public async Task DeleteCalculationDataFieldRelationshipDelegatesToGraphRepository()
        {
            string calculationAId = NewRandomString();
            string dataFieldId = NewRandomString();
            
            await _calculationRepository.DeleteCalculationDataFieldRelationship(calculationAId,
                dataFieldId);

            await ThenTheRelationshipWasDeleted<Calculation, DataField>(AttributeConstants.CalculationDataFieldRelationshipId,
                (AttributeConstants.CalculationId, calculationAId),
                (AttributeConstants.DataFieldId, dataFieldId));

            await AndTheRelationshipWasDeleted<DataField, Calculation>(AttributeConstants.DataFieldCalculationRelationship,
                (AttributeConstants.DataFieldId, dataFieldId),
                (AttributeConstants.CalculationId, calculationAId));
        }
        
        [TestMethod]
        public async Task UpsertCalculationDataFieldRelationshipDelegatesToGraphRepository()
        {
            string calculationAId = NewRandomString();
            string dataFieldId = NewRandomString();
            
            await _calculationRepository.UpsertCalculationDataFieldRelationship(calculationAId,
                dataFieldId);

            await ThenTheRelationshipWasCreated<Calculation, DataField>(AttributeConstants.CalculationDataFieldRelationshipId,
                (AttributeConstants.CalculationId, calculationAId),
                (AttributeConstants.DataFieldId, dataFieldId));

            await AndTheRelationshipWasCreated<DataField, Calculation>(AttributeConstants.DataFieldCalculationRelationship,
                (AttributeConstants.DataFieldId, dataFieldId),
                (AttributeConstants.CalculationId, calculationAId));
        }
    }
}
