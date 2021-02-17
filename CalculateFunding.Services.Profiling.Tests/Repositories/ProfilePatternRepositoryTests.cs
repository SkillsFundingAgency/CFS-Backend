using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Repositories;
using CalculateFunding.Services.Profiling.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Profiling.Tests.Repositories
{
    [TestClass]
    public class ProfilePatternRepositoryTests
    {
        private Mock<ICosmosRepository> _cosmos;

        private ProfilePatternRepository _repository;
        
        [TestInitialize]
        public void SetUp()
        {
            _cosmos = new Mock<ICosmosRepository>();
            
            _repository = new ProfilePatternRepository(_cosmos.Object);
        }

        [TestMethod]
        [DynamicData(nameof(MissingIdExamples), DynamicDataSourceType.Method)]
        public void DeleteGuardsAgainstMissingId(string id)
        {
            Func<Task<HttpStatusCode>> invocation = () => WhenThePatternIsDeleted(id);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be(nameof(id));
        }

        [TestMethod]
        public async Task DeleteDelegatesToCosmos()
        {
            string id = NewRandomString();
            HttpStatusCode expectedStatusCode = NewRandomStatusCode();
            
            GivenTheStatusCodeForDeleteId(id, expectedStatusCode);

            HttpStatusCode actualStatusCode = await WhenThePatternIsDeleted(id);

            actualStatusCode
                .Should()
                .Be(expectedStatusCode);
        }

        [TestMethod]
        public async Task GetDelegatesToCosmos()
        {
            FundingStreamPeriodProfilePattern expectedProfilePattern = NewProfilePattern();
            
            GivenTheProfilePattern(expectedProfilePattern);

            FundingStreamPeriodProfilePattern actualProfilePattern = await WhenTheProfilePatternIsQueried(expectedProfilePattern.Id);

            actualProfilePattern
                .Should()
                .BeSameAs(expectedProfilePattern);
        }
        
        [TestMethod]
        public async Task GetByKeyComponentsDelegatesToCosmos()
        {
            FundingStreamPeriodProfilePattern expectedProfilePattern = NewProfilePattern();
            
            GivenTheProfilePattern(expectedProfilePattern);

            FundingStreamPeriodProfilePattern actualProfilePattern = await WhenTheProfilePatternIsQueried(expectedProfilePattern.FundingPeriodId,
                expectedProfilePattern.FundingStreamId,
                expectedProfilePattern.FundingLineId,
                expectedProfilePattern.ProfilePatternKey);

            actualProfilePattern
                .Should()
                .BeSameAs(expectedProfilePattern);
        }

        [TestMethod]
        public async Task GetAllInFundingStreamAndFundingPeriodDelegatesToCosmos()
        {
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            IEnumerable<FundingStreamPeriodProfilePattern> expectedProfilePatterns = new[]
            {
                NewProfilePattern(),
                NewProfilePattern(),
                NewProfilePattern(),
                NewProfilePattern()
            };

            GivenTheProfilePatterns(fundingStreamId, fundingPeriodId, expectedProfilePatterns);
            
            IEnumerable<FundingStreamPeriodProfilePattern> actualProfilePatterns = await WhenTheProfilePatternsAreQueried(fundingStreamId, fundingPeriodId);

            actualProfilePatterns
                .Should()
                .BeEquivalentTo(expectedProfilePatterns);
        }

        private async Task<FundingStreamPeriodProfilePattern> WhenTheProfilePatternIsQueried(string id)
        {
            return await _repository.GetProfilePattern(id);
        }
        
        private async Task<FundingStreamPeriodProfilePattern> WhenTheProfilePatternIsQueried(string fundingPeriodId, 
            string fundingStreamId, 
            string fundingLineCode,
            string profilePatternKey)
        {
            return await _repository.GetProfilePattern(fundingPeriodId, 
                fundingStreamId, 
                fundingLineCode, 
                profilePatternKey);
        }
        
        private Task<HttpStatusCode> WhenThePatternIsDeleted(string id)
        {
            return _repository.DeleteProfilePattern(id);
        }

        private async Task<IEnumerable<FundingStreamPeriodProfilePattern>> WhenTheProfilePatternsAreQueried(string fundingStreamId,
            string fundingPeriodId)
        {
            return await _repository.GetProfilePatternsForFundingStreamAndFundingPeriod(fundingStreamId, fundingPeriodId);
        }
        
        private void GivenTheProfilePatterns(string fundingStreamId,
            string fundingPeriodId,
            IEnumerable<FundingStreamPeriodProfilePattern> profilePatterns)
        {
            _cosmos.Setup(_ => _.QuerySql<FundingStreamPeriodProfilePattern>(It.Is<CosmosDbQuery>(query =>
                    query.QueryText == @"SELECT 
                                *
                              FROM profilePeriodPattern p
                              WHERE p.documentType = 'FundingStreamPeriodProfilePattern'
                              AND p.deleted = false
                              AND p.content.fundingStreamId = @fundingStreamId
                              AND p.content.fundingPeriodId = @fundingPeriodId" &&
                    query.Parameters.Count(prm => prm.Name == "@fundingPeriodId" &&
                                                  prm.Value.Equals(fundingPeriodId)) == 1 &&
                    query.Parameters.Count(prm => prm.Name == "@fundingStreamId" &&
                                                  prm.Value.Equals(fundingStreamId)) == 1),
                    -1,
                    null))
                .ReturnsAsync(profilePatterns);
        }
        
        private static IEnumerable<object[]> MissingIdExamples()
        {
            yield return new object[] {null};
            yield return new object[] {""};
            yield return new object[] {string.Empty};
        }

        private void GivenTheStatusCodeForDeleteId(string id, HttpStatusCode statusCode)
        {
            _cosmos.Setup(_ => _.DeleteAsync<FundingStreamPeriodProfilePattern>(id, null, false, null))
                .ReturnsAsync(statusCode);
        }

        private void GivenTheProfilePattern(FundingStreamPeriodProfilePattern pattern)
        {
            _cosmos.Setup(_ => _.ReadDocumentByIdAsync<FundingStreamPeriodProfilePattern>(pattern.Id))
                .ReturnsAsync(new DocumentEntity<FundingStreamPeriodProfilePattern>(pattern));
        }

        private string NewRandomString() => new RandomString();
        
        private HttpStatusCode NewRandomStatusCode() => new RandomEnum<HttpStatusCode>();
        
        private FundingStreamPeriodProfilePattern NewProfilePattern() => new FundingStreamPeriodProfilePatternBuilder()
            .Build();
    }
}