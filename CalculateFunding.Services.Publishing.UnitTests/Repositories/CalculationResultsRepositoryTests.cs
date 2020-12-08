using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Repositories;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
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

            _cosmosRepository.Query<PublishedProvider>()
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
        [Ignore("The setup needs to be fixed with this test")]
        public async Task GetCalculationResultSummariesDelegatesToCosmosUsingCustomSqlQuery()
        {
            string specificationId = NewRandomString();
            string providerId = NewRandomString();
            ProviderCalculationResult resultOne = NewProviderResultSummary();
            ProviderCalculationResult resultTwo = NewProviderResultSummary();
            ProviderCalculationResult resultThree = NewProviderResultSummary();

            resultOne.ProviderId = providerId;

            string[] keys = new string[] { resultOne.ProviderId, resultTwo.ProviderId, resultThree.ProviderId };

            GivenTheSqlQueryResultsForSpecificationResults(specificationId, resultOne, resultTwo, resultThree);

            IEnumerable<ProviderCalculationResult> result = await WhenTheCalculationResultsAreQueriedBySpecificationIdAndProviderId(specificationId, providerId);

            result
                .Should()
                .BeOfType<IEnumerable<ProviderCalculationResult>>()
                .And
                .NotBeNull()
                .And
                .HaveCount(1);

            ProviderCalculationResult providerCalculationResult = result.FirstOrDefault();

            providerCalculationResult
                .Should()
                .BeEquivalentTo(resultOne);
        }

        private async Task<IEnumerable<ProviderCalculationResult>> WhenTheCalculationResultsAreQueriedBySpecificationIdAndProviderId(string specificationId, string providerId)
        {
            return await _repository.GetCalculationResultsBySpecificationAndProvider(specificationId, providerId);
        }

        private ProviderCalculationResult NewProviderResultSummary()
        {
            return new ProviderCalculationResult();
        }

        private void GivenTheSqlQueryResultsForSpecificationResults(string specificationId, params ProviderCalculationResult[] results)
        {
            _cosmosRepository.DynamicQueryPartitionedEntity<ProviderCalculationResult>(Arg.Is<CosmosDbQuery>(sql =>
                    sql.QueryText == @"SELECT
	                                        doc.content.provider.id AS providerId,
	                                        ARRAY(SELECT calcResult.calculation.id,
	                                                       calcResult['value']
	                                                FROM   calcResult IN doc.content.calcResults) AS Results
                                        FROM 	doc
                                        WHERE   doc.documentType='ProviderResult'
                                        AND     doc.content.specificationId = @specificationId" &&
                    sql.Parameters.First().Name == "@specificationId" &&
                    sql.Parameters.First().Value.ToString() == specificationId), Arg.Is(specificationId))
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