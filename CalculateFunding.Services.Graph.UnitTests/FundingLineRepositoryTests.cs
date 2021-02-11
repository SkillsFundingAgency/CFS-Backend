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
    public class FundingLineRepositoryTests : GraphRepositoryTestBase
    {
        private FundingLineRepository _FundingLineRepository;

        [TestInitialize]
        public void Setup()
        {
            _FundingLineRepository = new FundingLineRepository(GraphRepository);
        }

        [TestMethod]
        public async Task UpsertFundingLines_GivenValidFundingLines_ExpectedMethodsCalled()
        {
            FundingLine[] FundingLines = new[] { NewFundingLine(), NewFundingLine() };

            await _FundingLineRepository.UpsertFundingLines(FundingLines);

            await ThenTheNodesWereCreated(FundingLines, AttributeConstants.FundingLineId);
        }


        [TestMethod]
        public async Task GetAllEntities_GivenEntities_ExpectedMethodsCalled()
        {
            string FundingLineId = NewRandomString();

            FundingLine FundingLine1 = NewFundingLine();
            Calculation calculation2 = NewCalculation();

            Entity<FundingLine> entity = new Entity<FundingLine> { Node = FundingLine1, Relationships = new[] { new Relationship { One = calculation2, Two = FundingLine1, Type = AttributeConstants.CalculationFundingLineRelationshipId } } };

            GivenTheEntities(new[] { AttributeConstants.FundingLineCalculationRelationshipId, AttributeConstants.CalculationFundingLineRelationshipId }, AttributeConstants.FundingLineId, FundingLineId, entity);

            IEnumerable<Entity<FundingLine, IRelationship>> entities = await _FundingLineRepository.GetAllEntities(FundingLineId);

            entities
                .Should()
                .HaveCount(1);

            entities
                .FirstOrDefault()
                .Node
                .Should()
                .BeEquivalentTo(FundingLine1);

            entities
                .FirstOrDefault()
                .Relationships
                .Should()
                .HaveCount(1);
        }

        [TestMethod]
        public async Task DeleteCalculation_GivenValidCalculation_ExpectedMethodsCalled()
        {
            string FundingLineId = NewRandomString();

            await _FundingLineRepository.DeleteFundingLine(FundingLineId);

            await ThenTheNodeWasDeleted<FundingLine>(AttributeConstants.FundingLineId, FundingLineId);
        }

        [TestMethod]
        public async Task DeleteFundingLineCalculationRelationshipDelegatesToGraphRepository()
        {
            string FundingLineId = NewRandomString();
            string CalculationId = NewRandomString();

            await _FundingLineRepository.DeleteFundingLineCalculationRelationship(FundingLineId,
                CalculationId);

            await ThenTheRelationshipWasDeleted<FundingLine, Calculation>(AttributeConstants.FundingLineCalculationRelationshipId,
                (AttributeConstants.FundingLineId, FundingLineId),
                (AttributeConstants.CalculationId, CalculationId));
        }

        [TestMethod]
        public async Task DeleteCalculationFundingLineRelationshipDelegatesToGraphRepository()
        {
            string FundingLineId = NewRandomString();
            string CalculationId = NewRandomString();

            await _FundingLineRepository.DeleteCalculationFundingLineRelationship(CalculationId,
                FundingLineId);

            await ThenTheRelationshipWasDeleted<Calculation, FundingLine>(AttributeConstants.CalculationFundingLineRelationshipId,
                (AttributeConstants.CalculationId, CalculationId),
                (AttributeConstants.FundingLineId, FundingLineId));
        }

        [TestMethod]
        public async Task CreateFundingLineCalculationRelationshipDelegatesToGraphRepository()
        {
            string FundingLineId = NewRandomString();
            string CalculationId = NewRandomString();

            await _FundingLineRepository.UpsertFundingLineCalculationRelationship(FundingLineId,
                CalculationId);

            await ThenTheRelationshipWasCreated<FundingLine, Calculation>(AttributeConstants.FundingLineCalculationRelationshipId,
                (AttributeConstants.FundingLineId, FundingLineId),
                (AttributeConstants.CalculationId, CalculationId));
        }

        [TestMethod]
        public async Task CreateCalculationFundingLineRelationshipDelegatesToGraphRepository()
        {
            string FundingLineId = NewRandomString();
            string CalculationId = NewRandomString();

            await _FundingLineRepository.UpsertCalculationFundingLineRelationship(CalculationId,
                FundingLineId);

            await ThenTheRelationshipWasCreated<Calculation, FundingLine>(AttributeConstants.CalculationFundingLineRelationshipId,
                (AttributeConstants.CalculationId, CalculationId),
                (AttributeConstants.FundingLineId, FundingLineId));
        }
        
        [TestMethod]
        public async Task GetAllEntitiesForAll()
        {
            string[] ids = AsArray(NewRandomString(), NewRandomString());

            FundingLine[] entities = AsArray(NewFundingLine(), NewFundingLine());

            Entity<FundingLine>[] expected = entities.Select(fl => new Entity<FundingLine>
            {
                Node = fl
            }).ToArray();
            
            GivenTheEntitiesForAll( new[]
                {
                    AttributeConstants.FundingLineCalculationRelationshipId,
                    AttributeConstants.CalculationFundingLineRelationshipId
                }, ids.Select(id => (AttributeConstants.FundingLineId, id))
                    .ToArray(),
                expected);

            IEnumerable<Entity<FundingLine, IRelationship>> actual = await _FundingLineRepository.GetAllEntitiesForAll(ids);

            actual
                .Should()
                .BeEquivalentTo(expected.Select(_ => new Entity<FundingLine, IRelationship>
                {
                    Node = _.Node,
                    Relationships = _.Relationships
                }));
        }
        
        [TestMethod]
        public async Task DeleteCalculationFundingLineRelationships()
        {
            string calculationIdOne = NewRandomString();
            string calculationIdTwo = NewRandomString();
            string fundingLineIdOne = NewRandomString();
            string fundingLineIdTwo = NewRandomString();

            await _FundingLineRepository.DeleteCalculationFundingLineRelationships((calculationIdOne, fundingLineIdOne), (calculationIdTwo, fundingLineIdTwo));

            await ThenTheRelationshipsWereDeleted<Calculation, FundingLine>(AttributeConstants.CalculationFundingLineRelationshipId,
                ((AttributeConstants.CalculationId, calculationIdOne),
                    (AttributeConstants.FundingLineId, fundingLineIdOne)),
                ((AttributeConstants.CalculationId, calculationIdTwo),
                    (AttributeConstants.FundingLineId, fundingLineIdTwo)));
        }
        
        [TestMethod]
        public async Task DeleteFundingLineCalculationRelationships()
        {
            string calculationIdOne = NewRandomString();
            string calculationIdTwo = NewRandomString();
            string fundingLineIdOne = NewRandomString();
            string fundingLineIdTwo = NewRandomString();

            await _FundingLineRepository.DeleteFundingLineCalculationRelationships((fundingLineIdOne, calculationIdOne), (fundingLineIdTwo, calculationIdTwo));

            await ThenTheRelationshipsWereDeleted<FundingLine, Calculation>(AttributeConstants.FundingLineCalculationRelationshipId,
                ((AttributeConstants.FundingLineId, fundingLineIdOne),
                    (AttributeConstants.CalculationId, calculationIdOne)),
                ((AttributeConstants.FundingLineId, fundingLineIdTwo),
                    (AttributeConstants.CalculationId, calculationIdTwo)));
        }
        
        [TestMethod]
        public async Task UpsertCalculationFundingLineRelationships()
        {
            string calculationIdOne = NewRandomString();
            string calculationIdTwo = NewRandomString();
            string fundingLineIdOne = NewRandomString();
            string fundingLineIdTwo = NewRandomString();

            await _FundingLineRepository.UpsertCalculationFundingLineRelationships((calculationIdOne, fundingLineIdOne), (calculationIdTwo, fundingLineIdTwo));

            await ThenTheRelationshipsWereCreated<Calculation, FundingLine>(AttributeConstants.CalculationFundingLineRelationshipId,
                ((AttributeConstants.CalculationId, calculationIdOne),
                    (AttributeConstants.FundingLineId, fundingLineIdOne)),
                ((AttributeConstants.CalculationId, calculationIdTwo),
                    (AttributeConstants.FundingLineId, fundingLineIdTwo)));
        }
        
        [TestMethod]
        public async Task UpsertFundingLineCalculationRelationships()
        {
            string calculationIdOne = NewRandomString();
            string calculationIdTwo = NewRandomString();
            string fundingLineIdOne = NewRandomString();
            string fundingLineIdTwo = NewRandomString();

            await _FundingLineRepository.UpsertFundingLineCalculationRelationships((fundingLineIdOne, calculationIdOne), (fundingLineIdTwo, calculationIdTwo));

            await ThenTheRelationshipsWereCreated<FundingLine, Calculation>(AttributeConstants.FundingLineCalculationRelationshipId,
                ((AttributeConstants.FundingLineId, fundingLineIdOne),
                    (AttributeConstants.CalculationId, calculationIdOne)),
                ((AttributeConstants.FundingLineId, fundingLineIdTwo),
                    (AttributeConstants.CalculationId, calculationIdTwo)));
        }
        
        [TestMethod]
        public async Task DeleteFundingLines()
        {
            string[] ids = AsArray(NewRandomString(), NewRandomString());

            await _FundingLineRepository.DeleteFundingLines(ids);

            await ThenTheNodesWereDeleted<FundingLine>(ids.Select(_ => (AttributeConstants.FundingLineId, _)).ToArray());
        }
    }
}
