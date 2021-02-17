using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Profiling;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling
{
    [TestClass]
    public class FundingStreamPaymentDatesRepositoryTests
    {
        private Mock<ICosmosRepository> _cosmosRepository;
        private FundingStreamPaymentDatesRepository _repository;
        
        [TestInitialize]
        public void SetUp()
        {
            _cosmosRepository = new Mock<ICosmosRepository>();
            
            _repository = new FundingStreamPaymentDatesRepository(_cosmosRepository.Object);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task IsHealthOkChecksCosmosRepository(bool expectedIsOkFlag)
        {
            string expectedMessage = NewRandomString();

            GivenTheCosmosRepositoryHealth(expectedIsOkFlag, expectedMessage);

            ServiceHealth isHealthOk = await _repository.IsHealthOk();

            isHealthOk?
                .Name
                .Should()
                .Be(nameof(FundingStreamPaymentDatesRepository));

            isHealthOk?
                .Dependencies
                .FirstOrDefault()
                .Should()
                .Match<DependencyHealth>(_ => _.HealthOk == expectedIsOkFlag &&
                                              _.Message == expectedMessage);
        }

        [TestMethod]
        public async Task SaveFundingStreamUpdatedDatesDelegatesToCosmosRepository()
        {
            FundingStreamPaymentDates paymentDates = NewPaymentDates();

            await _repository.SaveFundingStreamUpdatedDates(paymentDates);
            
            _cosmosRepository
                .Verify(_ => _.UpsertAsync(paymentDates, paymentDates.FundingStreamId, 
                        false, 
                        true,
                        null),
                    Times.Once);
        }

        [TestMethod]
        [DataRow(null, "id", "fundingStreamId")]
        [DataRow("id", null, "fundingPeriodId")]
        public void GetUpdateDatesGuardsAgainstMissingParameters(string fundingStreamId,
            string fundingPeriodId,
            string expectedParameterName)
        {
            Func<Task<FundingStreamPaymentDates>> invocation = 
                () => WhenThePaymentDatesAreQueried(fundingStreamId, fundingPeriodId);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be(expectedParameterName);
        }

        [TestMethod]
        public async Task GetUpdateDatesExecutesSqlQueryAgainstCosmosRepositoryForSuppliedFundingStreamAndPeriodIds()
        {
            string fundingPeriodId = NewRandomString();
            string fundingStreamId = NewRandomString();

            FundingStreamPaymentDates expectedPaymentDates = NewPaymentDates();
            
            GivenThePaymentDatesForTheFundingStreamAndPeriodIds(fundingPeriodId, fundingStreamId, expectedPaymentDates);

            FundingStreamPaymentDates actualPaymentDates = await WhenThePaymentDatesAreQueried(fundingStreamId, fundingPeriodId);

            actualPaymentDates
                .Should()
                .BeSameAs(expectedPaymentDates);
            
            _cosmosRepository
                .Verify();
        }

        private void GivenThePaymentDatesForTheFundingStreamAndPeriodIds(string fundingPeriodId, 
            string fundingStreamId, 
            FundingStreamPaymentDates paymentDates)
        {
            _cosmosRepository.Setup(_ => _.QuerySql<FundingStreamPaymentDates>(It.Is<CosmosDbQuery>(query =>
                    query.QueryText == @"SELECT *
                              FROM c
                              WHERE c.documentType = 'FundingStreamPaymentDates'
                              AND c.deleted = false
                              AND c.content.fundingStreamId = @fundingStreamId
                              AND c.content.fundingPeriodId = @fundingPeriodId" &&
                    HasParameter(query.Parameters, "@fundingStreamId", fundingStreamId) &&
                    HasParameter(query.Parameters, "@fundingPeriodId", fundingPeriodId)), 
                    -1, 
                    1))
                .ReturnsAsync(new[] {paymentDates})
                .Verifiable();
        }

        private bool HasParameter(IEnumerable<CosmosDbQueryParameter> parameters,
            string name, 
            string value)
        {
            return parameters.Count(_ => _.Name == name &&
                                         _.Value.Equals(value)) == 1;
        }

        private async Task<FundingStreamPaymentDates> WhenThePaymentDatesAreQueried(string fundingStreamId,
            string fundingPeriodId)
        {
            return await _repository.GetUpdateDates(fundingStreamId, fundingPeriodId);
        }
        
        private void GivenTheCosmosRepositoryHealth(bool expectedIsOkFlag, string expectedMessage)
        {
            _cosmosRepository
                .Setup(_ => _.IsHealthOk())
                .Returns((expectedIsOkFlag, expectedMessage));
        }
        
        private FundingStreamPaymentDates NewPaymentDates() => new FundingStreamPaymentDatesBuilder()
            .Build();
        
        private string NewRandomString() => new RandomString();
    }
}