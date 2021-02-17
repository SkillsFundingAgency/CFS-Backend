using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Repositories;
using CalculateFunding.Services.Profiling.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Profiling.Tests
{
    [TestClass]
    public class ProfilePatternRepositoryTests
    {
        private readonly Mock<ICosmosRepository> _mockCosmoRepository;
        private readonly ProfilePatternRepository _repository;
        private readonly FundingStreamPeriodProfilePatternBuilder _fundingStreamPeriodProfilePatternBuilder;

        public ProfilePatternRepositoryTests()
        {
            _mockCosmoRepository = new Mock<ICosmosRepository>();
            _repository = new ProfilePatternRepository(_mockCosmoRepository.Object);
            _fundingStreamPeriodProfilePatternBuilder = new FundingStreamPeriodProfilePatternBuilder();
        }

        [TestMethod]
        public async Task ProfilePatternQueryRepository_ShouldSaveGivenFundingStreamPeriodProfilePattern()
        {
            // arrange
            FundingStreamPeriodProfilePattern fundingStreamPeriodProfilePattern = _fundingStreamPeriodProfilePatternBuilder.Build();
            var fundingStreamPeriodProfilePatternId = fundingStreamPeriodProfilePattern.Id;
            _mockCosmoRepository.Setup(x => x.UpsertAsync(It.IsAny<FundingStreamPeriodProfilePattern>(), null, false, true, null))
                .ReturnsAsync(HttpStatusCode.OK);

            // act
            var result = await _repository.SaveFundingStreamPeriodProfilePattern(fundingStreamPeriodProfilePattern);

            // assert
            result.Should().Be(HttpStatusCode.OK);
            _mockCosmoRepository.Verify(x => x.UpsertAsync(It.Is<FundingStreamPeriodProfilePattern>(f => f.Id == fundingStreamPeriodProfilePatternId), null, false, true, null), Times.Once);
        }

        [TestMethod]
        public async Task ProfilePatternQueryRepository_ShouldGetFundingStreamPeriodProfilePatternByIdIfExists()
        {
            // arrange
            FundingStreamPeriodProfilePattern fundingStreamPeriodProfilePattern = _fundingStreamPeriodProfilePatternBuilder.Build();
            var fundingStreamPeriodProfilePatternId = fundingStreamPeriodProfilePattern.Id;
            _mockCosmoRepository.Setup(x => x.ReadDocumentByIdAsync<FundingStreamPeriodProfilePattern>(fundingStreamPeriodProfilePatternId))
                .ReturnsAsync(new DocumentEntity<FundingStreamPeriodProfilePattern>(fundingStreamPeriodProfilePattern));

            // act
            var profilePattern = await _repository.GetProfilePattern(fundingStreamPeriodProfilePatternId);

            // assert
            profilePattern.Id.Should().Be(fundingStreamPeriodProfilePatternId);
            _mockCosmoRepository.Verify(x => x.ReadDocumentByIdAsync<FundingStreamPeriodProfilePattern>(It.Is<string>(id => id == fundingStreamPeriodProfilePatternId)), Times.Once);
        }

        [TestMethod]
        public async Task ProfilePatternQueryRepository_ShouldDeleteFundingStreamPeriodProfilePatternById()
        {
            // arrange
            FundingStreamPeriodProfilePattern fundingStreamPeriodProfilePattern = _fundingStreamPeriodProfilePatternBuilder.Build();
            var fundingStreamPeriodProfilePatternId = fundingStreamPeriodProfilePattern.Id;
            _mockCosmoRepository.Setup(x => x.DeleteAsync<FundingStreamPeriodProfilePattern>(fundingStreamPeriodProfilePatternId, null, false, null))
                .ReturnsAsync(HttpStatusCode.OK);

            // act
            var result = await _repository.DeleteProfilePattern(fundingStreamPeriodProfilePatternId);

            // assert
            result.Should().Be(HttpStatusCode.OK);
            _mockCosmoRepository.Verify(x => x.DeleteAsync<FundingStreamPeriodProfilePattern>(It.Is<string>(id => id == fundingStreamPeriodProfilePatternId), null, false, null), Times.Once);
        }

        [TestMethod]
        public async Task ProfilePatternQueryRepository_ShouldGetFundingStreamPeriodProfilePatternByProviderType()
        {
            // arrange
            string fundingPeriodId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingLineCode = NewRandomString();
            string providerType = "Acade";
            string providerSubType = "11ACA";

            string queryText = null;
            IEnumerable<CosmosDbQueryParameter> parameters = Enumerable.Empty<CosmosDbQueryParameter>();

            FundingStreamPeriodProfilePattern fundingStreamPeriodProfilePattern = _fundingStreamPeriodProfilePatternBuilder
                                                                            .WithProviderTypeSubTypes(new[] { new ProviderTypeSubType { ProviderType = providerType, ProviderSubType = providerSubType } })
                                                                            .Build();
            var fundingStreamPeriodProfilePatternId = fundingStreamPeriodProfilePattern.Id;
            _mockCosmoRepository.Setup(x => x.QuerySql<FundingStreamPeriodProfilePattern>(It.IsAny<CosmosDbQuery>(), -1, null))
                .ReturnsAsync(new List<FundingStreamPeriodProfilePattern> { fundingStreamPeriodProfilePattern })
                .Callback<CosmosDbQuery, int, int?>((query, itemsPerPage, maxItemrs) => { queryText = query.QueryText; parameters = query.Parameters; });

            // act
            var profilePattern = await _repository.GetProfilePattern(fundingPeriodId, fundingStreamId, fundingLineCode, providerType, providerSubType);

            // assert
            profilePattern.Id.Should().Be(fundingStreamPeriodProfilePatternId);
            profilePattern.ProviderTypeSubTypes.First().ProviderType.Should().Be(providerType);
            profilePattern.ProviderTypeSubTypes.First().ProviderSubType.Should().Be(providerSubType);
            _mockCosmoRepository.Verify(x => x.QuerySql<FundingStreamPeriodProfilePattern>(It.IsAny<CosmosDbQuery>(), -1, null), Times.Once);
            queryText.Should().Be(@"SELECT 
                                *
                              FROM profilePeriodPattern p
                              WHERE p.documentType = 'FundingStreamPeriodProfilePattern'
                              AND p.deleted = false
                              AND p.content.fundingStreamId = @fundingStreamId
                              AND p.content.fundingPeriodId = @fundingPeriodId
                              AND p.content.fundingLineId = @fundingLineCode");
            parameters.Single(p => p.Name == "@fundingStreamId").Value.Should().Be(fundingStreamId);
            parameters.Single(p => p.Name == "@fundingPeriodId").Value.Should().Be(fundingPeriodId);
            parameters.Single(p => p.Name == "@fundingLineCode").Value.Should().Be(fundingLineCode);;
        }

        private string NewRandomString() => new RandomString();
    }
}
