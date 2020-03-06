using CalculateFunding.Models.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Graph.UnitTests
{
    [TestClass]
    public class SpecificationRepositoryTests : GraphRepositoryTestBase
    {
        private const string SpecificationId = "specificationid";
        private const string DatasetId = Dataset.IdField;
        private const string SpecificationDatasetRelationship = SpecificationRepository.SpecificationDatasetRelationship;
        private const string DatasetSpecificationRelationship =  SpecificationRepository.DatasetSpecificationRelationship;
        
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

            await ThenTheNodesWereCreated(specifications, SpecificationId);
        }

        [TestMethod]
        public async Task DeleteCalculation_GivenValidCalculation_ExpectedMethodsCalled()
        {
            string specificationId = NewRandomString();
            
            await _specificationRepository.DeleteSpecification(specificationId);

            await ThenTheNodeWasDeleted<Specification>(SpecificationId, specificationId);
        }

        [TestMethod]
        public async Task DeleteAllForSpecification_GivenValidCalculation_ExpectedMethodsCalled()
        {
            string specificationId = NewRandomString();

            await _specificationRepository.DeleteAllForSpecification(specificationId);

            await ThenTheNodeAndAllItsChildrenWereDeleted<Specification>(SpecificationId, specificationId);
        }
        
        [TestMethod]
        public async Task DeleteSpecificationDatasetRelationshipDelegatesToGraphRepository()
        {
            string specificationId = NewRandomString();
            string datasetId = NewRandomString();
            
            await _specificationRepository.DeleteSpecificationDatasetRelationship(specificationId,
                datasetId);

            await ThenTheRelationshipWasDeleted<Specification, Dataset>(SpecificationDatasetRelationship,
                (SpecificationId, specificationId),
                (DatasetId, datasetId));

            await AndTheRelationshipWasDeleted<Dataset, Specification>(DatasetSpecificationRelationship,
                (DatasetId, datasetId),
                (SpecificationId, specificationId));
        }
        
        [TestMethod]
        public async Task CreateSpecificationDatasetRelationshipDelegatesToGraphRepository()
        {
            string specificationId = NewRandomString();
            string datasetId = NewRandomString();
            
            await _specificationRepository.CreateSpecificationDatasetRelationship(specificationId,
                datasetId);

            await ThenTheRelationshipWasCreated<Specification, Dataset>(SpecificationDatasetRelationship,
                (SpecificationId, specificationId),
                (DatasetId, datasetId));

            await AndTheRelationshipWasCreated<Dataset, Specification>(DatasetSpecificationRelationship,
                (DatasetId, datasetId),
                (SpecificationId, specificationId));
        }
    }
}
