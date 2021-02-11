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

        [TestMethod]
        public async Task DeleteCalculations()
        {
            string[] ids = AsArray(NewRandomString(), NewRandomString());

            await _calculationRepository.DeleteCalculations(ids);

            await ThenTheNodesWereDeleted<Calculation>(ids.Select(_ => (AttributeConstants.CalculationId, _)).ToArray());
        }

        [TestMethod]
        public async Task UpsertCalculationSpecificationRelationships()
        {
            string calculationIdOne = NewRandomString();
            string calculationIdTwo = NewRandomString();
            string specificationIdOne = NewRandomString();
            string specificationIdTwo = NewRandomString();

            await _calculationRepository.UpsertCalculationSpecificationRelationships((calculationIdOne, specificationIdOne), (calculationIdTwo, specificationIdTwo));

            await ThenTheRelationshipsWereCreated<Calculation, Specification>(AttributeConstants.CalculationSpecificationRelationshipId,
                ((AttributeConstants.CalculationId, calculationIdOne),
                    (AttributeConstants.SpecificationId, specificationIdOne)),
                ((AttributeConstants.CalculationId, calculationIdTwo),
                    (AttributeConstants.SpecificationId, specificationIdTwo)));

            await AndTheRelationshipsWereCreated<Specification, Calculation>(AttributeConstants.SpecificationCalculationRelationshipId,
                ((AttributeConstants.SpecificationId, specificationIdOne),
                    (AttributeConstants.CalculationId, calculationIdOne)),
                ((AttributeConstants.SpecificationId, specificationIdTwo),
                    (AttributeConstants.CalculationId, calculationIdTwo)));
        }
        
        [TestMethod]
        public async Task UpsertCalculationCalculationRelationships()
        {
            string calculationIdOne = NewRandomString();
            string calculationIdTwo = NewRandomString();
            string otherCalculationIdOne = NewRandomString();
            string otherCalculationIdTwo = NewRandomString();

            await _calculationRepository.UpsertCalculationCalculationRelationships((calculationIdOne, otherCalculationIdOne), (calculationIdTwo, otherCalculationIdTwo));

            await ThenTheRelationshipsWereCreated<Calculation, Calculation>(AttributeConstants.CalculationACalculationBRelationship,
                ((AttributeConstants.CalculationId, calculationIdOne),
                    (AttributeConstants.CalculationId, otherCalculationIdOne)),
                ((AttributeConstants.CalculationId, calculationIdTwo),
                    (AttributeConstants.CalculationId, otherCalculationIdTwo)));

            await AndTheRelationshipsWereCreated<Calculation, Calculation>(AttributeConstants.CalculationBCalculationARelationship,
                ((AttributeConstants.CalculationId, otherCalculationIdOne),
                    (AttributeConstants.CalculationId, calculationIdOne)),
                ((AttributeConstants.CalculationId, otherCalculationIdTwo),
                    (AttributeConstants.CalculationId, calculationIdTwo)));
        }
        
        [TestMethod]
        public async Task DeleteCalculationSpecificationRelationships()
        {
            string calculationIdOne = NewRandomString();
            string calculationIdTwo = NewRandomString();
            string specificationIdOne = NewRandomString();
            string specificationIdTwo = NewRandomString();

            await _calculationRepository.DeleteCalculationSpecificationRelationships((calculationIdOne, specificationIdOne), (calculationIdTwo, specificationIdTwo));

            await ThenTheRelationshipsWereDeleted<Calculation, Specification>(AttributeConstants.CalculationSpecificationRelationshipId,
                ((AttributeConstants.CalculationId, calculationIdOne),
                    (AttributeConstants.SpecificationId, specificationIdOne)),
                ((AttributeConstants.CalculationId, calculationIdTwo),
                    (AttributeConstants.SpecificationId, specificationIdTwo)));

            await AndTheRelationshipsWereDeleted<Specification, Calculation>(AttributeConstants.SpecificationCalculationRelationshipId,
                ((AttributeConstants.SpecificationId, specificationIdOne),
                    (AttributeConstants.CalculationId, calculationIdOne)),
                ((AttributeConstants.SpecificationId, specificationIdTwo),
                    (AttributeConstants.CalculationId, calculationIdTwo)));
        }
        
        [TestMethod]
        public async Task DeleteCalculationCalculationRelationships()
        {
            string calculationIdOne = NewRandomString();
            string calculationIdTwo = NewRandomString();
            string otherCalculationIdOne = NewRandomString();
            string otherCalculationIdTwo = NewRandomString();

            await _calculationRepository.DeleteCalculationCalculationRelationships((calculationIdOne, otherCalculationIdOne), (calculationIdTwo, otherCalculationIdTwo));

            await ThenTheRelationshipsWereDeleted<Calculation, Calculation>(AttributeConstants.CalculationACalculationBRelationship,
                ((AttributeConstants.CalculationId, calculationIdOne),
                    (AttributeConstants.CalculationId, otherCalculationIdOne)),
                ((AttributeConstants.CalculationId, calculationIdTwo),
                    (AttributeConstants.CalculationId, otherCalculationIdTwo)));

            await AndTheRelationshipsWereDeleted<Calculation, Calculation>(AttributeConstants.CalculationBCalculationARelationship,
                ((AttributeConstants.CalculationId, otherCalculationIdOne),
                    (AttributeConstants.CalculationId, calculationIdOne)),
                ((AttributeConstants.CalculationId, otherCalculationIdTwo),
                    (AttributeConstants.CalculationId, calculationIdTwo)));
        }
        
        [TestMethod]
        public async Task UpsertCalculationDataFieldRelationships()
        {
            string calculationIdOne = NewRandomString();
            string calculationIdTwo = NewRandomString();
            string datafieldIdOne = NewRandomString();
            string datafieldIdTwo = NewRandomString();

            await _calculationRepository.UpsertCalculationDataFieldRelationships((calculationIdOne, datafieldIdOne), (calculationIdTwo, datafieldIdTwo));

            await ThenTheRelationshipsWereCreated<Calculation, DataField>(AttributeConstants.CalculationDataFieldRelationshipId,
                ((AttributeConstants.CalculationId, calculationIdOne),
                    (AttributeConstants.DataFieldId, datafieldIdOne)),
                ((AttributeConstants.CalculationId, calculationIdTwo),
                    (AttributeConstants.DataFieldId, datafieldIdTwo)));

            await AndTheRelationshipsWereCreated<DataField, Calculation>(AttributeConstants.DataFieldCalculationRelationship,
                ((AttributeConstants.DataFieldId, datafieldIdOne),
                    (AttributeConstants.CalculationId, calculationIdOne)),
                ((AttributeConstants.DataFieldId, datafieldIdTwo),
                    (AttributeConstants.CalculationId, calculationIdTwo)));
        }
        
        [TestMethod]
        public async Task DeleteCalculationDataFieldRelationships()
        {
            string calculationIdOne = NewRandomString();
            string calculationIdTwo = NewRandomString();
            string datafieldIdOne = NewRandomString();
            string datafieldIdTwo = NewRandomString();

            await _calculationRepository.DeleteCalculationDataFieldRelationships((calculationIdOne, datafieldIdOne), (calculationIdTwo, datafieldIdTwo));

            await ThenTheRelationshipsWereDeleted<Calculation, DataField>(AttributeConstants.CalculationDataFieldRelationshipId,
                ((AttributeConstants.CalculationId, calculationIdOne),
                    (AttributeConstants.DataFieldId, datafieldIdOne)),
                ((AttributeConstants.CalculationId, calculationIdTwo),
                    (AttributeConstants.DataFieldId, datafieldIdTwo)));

            await AndTheRelationshipsWereDeleted<DataField, Calculation>(AttributeConstants.DataFieldCalculationRelationship,
                ((AttributeConstants.DataFieldId, datafieldIdOne),
                    (AttributeConstants.CalculationId, calculationIdOne)),
                ((AttributeConstants.DataFieldId, datafieldIdTwo),
                    (AttributeConstants.CalculationId, calculationIdTwo)));
        }

        [TestMethod]
        public async Task GetAllEntitiesForAll()
        {
            string[] ids = AsArray(NewRandomString(), NewRandomString());

            Calculation[] entities = AsArray(NewCalculation(), NewCalculation());

            Entity<Calculation>[] expected = entities.Select(calc => new Entity<Calculation>
            {
                Node = calc
            }).ToArray();
            
            GivenTheEntitiesForAll( new[]
            {
                AttributeConstants.DataFieldCalculationRelationship,
                AttributeConstants.CalculationDataFieldRelationshipId,
                AttributeConstants.CalculationACalculationBRelationship,
                AttributeConstants.CalculationBCalculationARelationship
            }, ids.Select(id => (AttributeConstants.CalculationId, id))
                    .ToArray(),
                expected);

            IEnumerable<Entity<Calculation, IRelationship>> actual = await _calculationRepository.GetAllEntitiesForAll(ids);

            actual
                .Should()
                .BeEquivalentTo(expected.Select(_ => new Entity<Calculation, IRelationship>
                {
                    Node = _.Node,
                    Relationships = _.Relationships
                }));
        }
    }
}
