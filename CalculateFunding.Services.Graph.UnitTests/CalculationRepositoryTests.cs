using CalculateFunding.Models.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Graph.UnitTests
{
    [TestClass]
    public class CalculationRepositoryTests : GraphRepositoryTestBase
    {
        private const string CalculationId = CalculationRepository.CalculationId;
        private const string SpecificationId = CalculationRepository.SpecificationId;
        private const string CalculationSpecificationRelationship = CalculationRepository.CalculationSpecificationRelationship;
        private const string SpecificationCalculationRelationship = CalculationRepository.SpecificationCalculationRelationship;
        private const string CalculationACalculationBRelationship = CalculationRepository.CalculationACalculationBRelationship;
        private const string CalculationBCalculationARelationship = CalculationRepository.CalculationBCalculationARelationship;
        private const string CalculationDataFieldRelationship = CalculationRepository.CalculationDataFieldRelationship;
        private const string DataFieldCalculationRelationship = CalculationRepository.DataFieldCalculationRelationship;
        private const string DataFieldId = DataField.IdField;
        
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

            await ThenTheNodesWereCreated(calculations, CalculationId);
        }

        [TestMethod]
        public async Task DeleteCalculation_GivenValidCalculation_ExpectedMethodsCalled()
        {
            string calculationId = NewRandomString();

            await _calculationRepository.DeleteCalculation(calculationId);

            await ThenTheNodeWasDeleted<Calculation>( CalculationId, calculationId);
        }

        [TestMethod]
        public async Task UpsertCalculationSpecificationRelationship_GivenValidSpecificationAndCalculation_ExpectedMethodsCalled()
        {
            string calculationId = NewRandomString();
            string specificationId = NewRandomString();
            
            await _calculationRepository.UpsertCalculationSpecificationRelationship(calculationId,
                specificationId);

            await ThenTheRelationshipWasCreated<Calculation, Specification>(CalculationSpecificationRelationship,
                    (CalculationId, calculationId),
                    (SpecificationId, specificationId));

            await AndTheRelationshipWasCreated<Specification, Calculation>(SpecificationCalculationRelationship,
                    (SpecificationId, specificationId),
                    (CalculationId, calculationId));
        }

        [TestMethod]
        public async Task UpsertCalculationCalculationRelationship_GivenValidCalculationAndCalculation_ExpectedMethodsCalled()
        {
            string calculationAId = NewRandomString();
            string calculationBId = NewRandomString();
            
            await _calculationRepository.UpsertCalculationCalculationRelationship(calculationAId,
                calculationBId);

            await ThenTheRelationshipWasCreated<Calculation, Calculation>(CalculationACalculationBRelationship,
                (CalculationId, calculationAId),
                (CalculationId, calculationBId));

            await AndTheRelationshipWasCreated<Calculation, Calculation>(CalculationBCalculationARelationship,
                (CalculationId, calculationBId),
                (CalculationId, calculationAId));
        }

        [TestMethod]
        public async Task DeleteCalculationSpecificationRelationship_GivenValidCalculationAndSpecification_ExpectedMethodsCalled()
        {
            string calculationId = NewRandomString();
            string specificationId = NewRandomString();
            
            await _calculationRepository.DeleteCalculationSpecificationRelationship(calculationId,
                specificationId);

            await ThenTheRelationshipWasDeleted<Calculation, Specification>(CalculationSpecificationRelationship,
                    (CalculationId, calculationId),
                   (SpecificationId, specificationId));

            await AndTheRelationshipWasDeleted<Specification, Calculation>(SpecificationCalculationRelationship,
                    (SpecificationId, specificationId),
                    (CalculationId, calculationId));
        }

        [TestMethod]
        public async Task DeleteCalculationCalculationRelationship_GivenValidCalculationAndCalculation_ExpectedMethodsCalled()
        {
            string calculationAId = NewRandomString();
            string calculationBId = NewRandomString();
            
            await _calculationRepository.DeleteCalculationCalculationRelationship(calculationAId,
                calculationBId);

            await ThenTheRelationshipWasDeleted<Calculation, Calculation>(CalculationACalculationBRelationship,
                (CalculationId, calculationAId),
                (CalculationId, calculationBId));

            await AndTheRelationshipWasDeleted<Calculation, Calculation>(CalculationBCalculationARelationship,
                (CalculationId, calculationBId),
                (CalculationId, calculationAId));
        }

        [TestMethod]
        public async Task DeleteCalculationDataFieldRelationshipDelegatesToGraphRepository()
        {
            string calculationAId = NewRandomString();
            string dataFieldId = NewRandomString();
            
            await _calculationRepository.DeleteCalculationDataFieldRelationship(calculationAId,
                dataFieldId);

            await ThenTheRelationshipWasDeleted<Calculation, DataField>(CalculationDataFieldRelationship,
                (CalculationId, calculationAId),
                (DataFieldId, dataFieldId));

            await AndTheRelationshipWasDeleted<DataField, Calculation>(DataFieldCalculationRelationship,
                (DataFieldId, dataFieldId),
                (CalculationId, calculationAId));
        }
        
        [TestMethod]
        public async Task UpsertCalculationDataFieldRelationshipDelegatesToGraphRepository()
        {
            string calculationAId = NewRandomString();
            string dataFieldId = NewRandomString();
            
            await _calculationRepository.CreateCalculationDataFieldRelationship(calculationAId,
                dataFieldId);

            await ThenTheRelationshipWasCreated<Calculation, DataField>(CalculationDataFieldRelationship,
                (CalculationId, calculationAId),
                (DataFieldId, dataFieldId));

            await AndTheRelationshipWasCreated<DataField, Calculation>(DataFieldCalculationRelationship,
                (DataFieldId, dataFieldId),
                (CalculationId, calculationAId));
        }
    }
}
