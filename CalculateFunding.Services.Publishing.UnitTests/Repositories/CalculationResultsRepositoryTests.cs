using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Repositories;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.Documents;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Publishing.UnitTests.Repositories
{
    [TestClass]
    public class CalculationResultsRepositoryTests
    {
        private List<PublishedProvider> _providers;
        private ICosmosRepository _cosmosRepository;

        private CalculationResultsRepository _repository;

        [TestInitialize]
        public void SetUp()
        {
            _providers = new List<PublishedProvider>();
            _cosmosRepository = Substitute.For<ICosmosRepository>();

            _cosmosRepository.Query<PublishedProvider>(true)
                .Returns(_providers.AsQueryable());

            _repository = new CalculationResultsRepository(_cosmosRepository);
        }

        [TestMethod]
        public async Task IsHealthOkChecksCosmosRepository()
        {
            bool expectedIsOkFlag = new RandomBoolean();
            string expectedMessage = NewRandomString();

            GivenTheRepositoryServiceHealth(expectedIsOkFlag, expectedMessage);

            ServiceHealth isHealthOk = await _repository.IsHealthOk();

            isHealthOk
                .Should()
                .NotBeNull();

            isHealthOk
                .Name
                .Should()
                .Be(nameof(CalculationResultsRepository));

            DependencyHealth dependencyHealth = isHealthOk
                .Dependencies
                .FirstOrDefault();

            dependencyHealth
                .Should()
                .Match<DependencyHealth>(_ => _.HealthOk == expectedIsOkFlag &&
                                              _.Message == expectedMessage);
        }

        [TestMethod]
        public async Task GetCalculationResultSummariesDelegatesToCosmosUsingCustomSqlQuery()
        {
            string specificationId = NewRandomString();
            ProviderResult resultOne = NewProviderResultSummary();
            ProviderResult resultTwo = NewProviderResultSummary();
            ProviderResult resultThree = NewProviderResultSummary();

            GivenTheSqlQueryResultsForSpecificationResults(specificationId, resultOne, resultTwo, resultThree);

            IEnumerable<ProviderResult> results = await WhenTheCalculationResultsAreQueriedBySpecificationId(specificationId);

            results
                .Should()
                .BeEquivalentTo(resultOne, resultTwo, resultThree);
        }

        private async Task<IEnumerable<ProviderResult>> WhenTheCalculationResultsAreQueriedBySpecificationId(string specificationId)
        {
            return await _repository.GetCalculationResultsBySpecificationId(specificationId);
        }

        private ProviderResult NewProviderResultSummary()
        {
            return new ProviderResult();
        }

        private void GivenTheSqlQueryResultsForSpecificationResults(string specificationId, params ProviderResult[] results)
        {
            _cosmosRepository.DynamicQueryPartionedEntity<ProviderResult>(Arg.Is<SqlQuerySpec>(sql =>
                    sql.QueryText == @"
SELECT
	    doc.content.id AS providerId,
	    ARRAY(  SELECT calcResult.calculation.id,
	                   calcResult.value 
	            FROM   calcResult IN doc.content.calcResults) AS Results
FROM 	doc
WHERE   doc.documentType='ProviderResult'
AND     doc.content.specificationId = @specificationId" &&
                    sql.Parameters.First().Name == "@specificationId" &&
                    sql.Parameters.First().Value == specificationId))
                .Returns(results.AsQueryable());
        }

        private void GivenTheRepositoryServiceHealth(bool expectedIsOkFlag, string expectedMessage)
        {
            _cosmosRepository.IsHealthOk().Returns((expectedIsOkFlag, expectedMessage));
        }

        private string NewRandomString()
        {
            return new RandomString();
        }
    }
}