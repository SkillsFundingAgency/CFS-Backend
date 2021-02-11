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
    public class SpecificationRepositoryTests : GraphRepositoryTestBase
    {
        private SpecificationRepository _specificationRepository;

        [TestInitialize]
        public void Setup()
        {
            _specificationRepository = new SpecificationRepository(GraphRepository);
        }

        [TestMethod]
        public async Task UpsertSpecifications_GivenValidSpecifications_ExpectedMethodsCalled()
        {
            Specification[] specifications = new[] { NewSpecification(), NewSpecification() };

            await _specificationRepository.UpsertSpecifications(specifications);

            await ThenTheNodesWereCreated(specifications, AttributeConstants.SpecificationId);
        }


        [TestMethod]
        public async Task GetAllEntities_GivenEntities_ExpectedMethodsCalled()
        {
            string specificationId = NewRandomString();

            Specification specification1 = NewSpecification();
            Calculation calculation2 = NewCalculation();

            Entity<Specification> entity = new Entity<Specification> { Node = specification1, Relationships = new[] { new Relationship { One = calculation2, Two = specification1, Type = AttributeConstants.CalculationSpecificationRelationshipId } } };

            GivenTheEntities(new[] { AttributeConstants.CalculationACalculationBRelationship, AttributeConstants.CalculationSpecificationRelationshipId }, AttributeConstants.SpecificationId, specificationId, entity);

            IEnumerable<Entity<Specification, IRelationship>> entities = await _specificationRepository.GetAllEntities(specificationId);

            entities
                .Should()
                .HaveCount(1);

            entities
                .FirstOrDefault()
                .Node
                .Should()
                .BeEquivalentTo(specification1);

            entities
                .FirstOrDefault()
                .Relationships
                .Should()
                .HaveCount(1);
        }

        [TestMethod]
        public async Task DeleteCalculation_GivenValidCalculation_ExpectedMethodsCalled()
        {
            string specificationId = NewRandomString();
            
            await _specificationRepository.DeleteSpecification(specificationId);

            await ThenTheNodeWasDeleted<Specification>(AttributeConstants.SpecificationId, specificationId);
        }
        
        [TestMethod]
        public async Task DeleteSpecificationDatasetRelationshipDelegatesToGraphRepository()
        {
            string specificationId = NewRandomString();
            string datasetId = NewRandomString();
            
            await _specificationRepository.DeleteSpecificationDatasetRelationship(specificationId,
                datasetId);

            await ThenTheRelationshipWasDeleted<Specification, Dataset>(AttributeConstants.SpecificationDatasetRelationship,
                (AttributeConstants.SpecificationId, specificationId),
                (AttributeConstants.DatasetId, datasetId));

            await AndTheRelationshipWasDeleted<Dataset, Specification>(AttributeConstants.DatasetSpecificationRelationship,
                (AttributeConstants.DatasetId, datasetId),
                (AttributeConstants.SpecificationId, specificationId));
        }
        
        [TestMethod]
        public async Task CreateSpecificationDatasetRelationshipDelegatesToGraphRepository()
        {
            string specificationId = NewRandomString();
            string datasetId = NewRandomString();
            
            await _specificationRepository.CreateSpecificationDatasetRelationship(specificationId,
                datasetId);

            await ThenTheRelationshipWasCreated<Specification, Dataset>(AttributeConstants.SpecificationDatasetRelationship,
                (AttributeConstants.SpecificationId, specificationId),
                (AttributeConstants.DatasetId, datasetId));

            await AndTheRelationshipWasCreated<Dataset, Specification>(AttributeConstants.DatasetSpecificationRelationship,
                (AttributeConstants.DatasetId, datasetId),
                (AttributeConstants.SpecificationId, specificationId));
        }
        
        [TestMethod]
        public async Task DeleteSpecifications()
        {
            string[] ids = AsArray(NewRandomString(), NewRandomString());

            await _specificationRepository.DeleteSpecifications(ids);

            await ThenTheNodesWereDeleted<Specification>(ids.Select(_ => (AttributeConstants.SpecificationId, _)).ToArray());
        }
        
        [TestMethod]
        public async Task CreateSpecificationDatasetRelationships()
        {
            string specificationIdOne = NewRandomString();
            string specificationIdTwo = NewRandomString();
            string datasetIdOne = NewRandomString();
            string datasetIdTwo = NewRandomString();

            await _specificationRepository.CreateSpecificationDatasetRelationships((specificationIdOne, datasetIdOne), (specificationIdTwo, datasetIdTwo));

            await ThenTheRelationshipsWereCreated<Specification, Dataset>(AttributeConstants.SpecificationDatasetRelationship,
                ((AttributeConstants.SpecificationId, specificationIdOne),
                    (AttributeConstants.DatasetId, datasetIdOne)),
                ((AttributeConstants.SpecificationId, specificationIdTwo),
                    (AttributeConstants.DatasetId, datasetIdTwo)));

            await AndTheRelationshipsWereCreated<Dataset, Specification>(AttributeConstants.DatasetSpecificationRelationship,
                ((AttributeConstants.DatasetId, datasetIdOne),
                    (AttributeConstants.SpecificationId, specificationIdOne)),
                ((AttributeConstants.DatasetId, datasetIdTwo),
                    (AttributeConstants.SpecificationId, specificationIdTwo)));
        }
        
        [TestMethod]
        public async Task DeleteSpecificationDatasetRelationships()
        {
            string specificationIdOne = NewRandomString();
            string specificationIdTwo = NewRandomString();
            string datasetIdOne = NewRandomString();
            string datasetIdTwo = NewRandomString();

            await _specificationRepository.DeleteSpecificationDatasetRelationships((specificationIdOne, datasetIdOne), (specificationIdTwo, datasetIdTwo));

            await ThenTheRelationshipsWereDeleted<Specification, Dataset>(AttributeConstants.SpecificationDatasetRelationship,
                ((AttributeConstants.SpecificationId, specificationIdOne),
                    (AttributeConstants.DatasetId, datasetIdOne)),
                ((AttributeConstants.SpecificationId, specificationIdTwo),
                    (AttributeConstants.DatasetId, datasetIdTwo)));

            await AndTheRelationshipsWereDeleted<Dataset, Specification>(AttributeConstants.DatasetSpecificationRelationship,
                ((AttributeConstants.DatasetId, datasetIdOne),
                    (AttributeConstants.SpecificationId, specificationIdOne)),
                ((AttributeConstants.DatasetId, datasetIdTwo),
                    (AttributeConstants.SpecificationId, specificationIdTwo)));
        }
    }
}
