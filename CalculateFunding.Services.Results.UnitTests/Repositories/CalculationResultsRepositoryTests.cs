using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Results.Repositories;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Results.UnitTests.Repositories
{
    [TestClass]
    public class CalculationResultsRepositoryTests
    {
        private CalculationResultsRepository _calculationResults;
        private EngineSettings _engineSettings;
        private Mock<ICosmosRepository> _cosmos;

        [TestInitialize]
        public void Initialize()
        {
            _engineSettings = new EngineSettings();
            _cosmos = new Mock<ICosmosRepository>();

            _calculationResults
                = new CalculationResultsRepository(_cosmos.Object, _engineSettings);
        }

        [TestMethod]
        public async Task ProviderResultsBatchProcessing_GeneratesCorrectQuery()
        {
            string specificationId = NewRandomString();

            await WhenProviderResultsBatchProcessing(specificationId, providerResults => Task.CompletedTask);

            _cosmos
                .Verify(_ => _.DocumentsBatchProcessingAsync(
                        It.IsAny<Func<List<ProviderResult>, Task>>(),
                        It.Is<CosmosDbQuery>(_ =>
                            _.Parameters.Count() == 1 && _.Parameters.SingleOrDefault().Value.ToString() == specificationId &&
                            _.QueryText == @"SELECT
                                c.id as id,
                                c.createdAt as createdAt,
                                c.content.specificationId as specificationId,
                                c.content.isIndicativeProvider as isIndicativeProvider,
                                {
                                ""urn"" : c.content.provider.urn,
                                ""ukPrn"" : c.content.provider.ukPrn,
                                ""upin"" : c.content.provider.upin,
                                ""Id"" : c.content.provider.id,
                                ""Name"" : c.content.provider.name,
                                ""providerType"" : c.content.provider.providerType,
                                ""providerSubType"" : c.content.provider.providerSubType,
                                ""authority"" : c.content.provider.authority,
                                ""laCode"" : c.content.provider.laCode,
                                ""localAuthorityName"" : c.content.provider.localAuthorityName,
                                ""establishmentNumber"" : c.content.provider.establishmentNumber,
                                ""dateOpened"" : c.content.provider.dateOpened } AS provider,
                                ARRAY(
    	                            SELECT calcResult.calculation as calculation, 
    	                            calcResult[""value""],
    	                            calcResult.exceptionType as exceptionType,
    	                            calcResult.exceptionMessage as exceptionMessage,
    	                            calcResult.calculationType as calculationType
    	                            FROM calcResult IN c.content.calcResults) AS calcResults,
                                ARRAY(
                                    SELECT fundingLineResult.fundingLine as fundingLine,
                                    fundingLineResult.fundingLineFundingStreamId as fundingLineFundingStreamId,
                                    fundingLineResult[""value""],
    	                            fundingLineResult.exceptionType as exceptionType,
    	                            fundingLineResult.exceptionMessage as exceptionMessage
                                    FROM fundingLineResult IN c.content.fundingLineResults) AS fundingLineResults
                            FROM    calculationresults c
                            WHERE   c.content.specificationId = @SpecificationId 
                                    AND c.documentType = 'ProviderResult' 
                                    AND c.deleted = false
                            ORDER BY c.content.provider.ukPrn ASC"),
                        1000),
                    Times.Once());
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task CheckHasNewResultsForSpecificationIdAndTime(bool expectedCheckFlag)
        {
            string specificationId = NewRandomString();
            DateTimeOffset dateFrom = NewRandomDateTime();
            
            GivenTheSpecificationHasNewResultsSince(specificationId, dateFrom, expectedCheckFlag);

            bool actualCheckFlag = await WhenTheSpecificationNewResultsAreChecked(specificationId, dateFrom);

            actualCheckFlag
                .Should()
                .Be(expectedCheckFlag);
        }

        private async Task WhenProviderResultsBatchProcessing(string specificationId,
            Func<List<ProviderResult>, Task> processProcessProviderResultsBatch,
            int itemsPerPage = 1000) =>
            await _calculationResults.ProviderResultsBatchProcessing(specificationId, processProcessProviderResultsBatch, itemsPerPage);

        private async Task<bool> WhenTheSpecificationNewResultsAreChecked(string specificationId,
            DateTimeOffset dateFrom)
            => await _calculationResults.CheckHasNewResultsForSpecificationIdAndTime(specificationId,
                dateFrom);

        private void GivenTheSpecificationHasNewResultsSince(string specificationId,
            DateTimeOffset dateFrom,
            bool expectedNewResultsFlag)
        {
            _cosmos.Setup(_ => _.DynamicQuery(It.Is<CosmosDbQuery>(qry =>
                    qry.QueryText == @"SELECT c.id 
                            FROM results c
                            WHERE c.content.specificationId = @SpecificationId
                            AND c.documentType = 'ProviderResult'
                            AND c.updatedAt > @DateFrom
                            AND c.deleted = false" &&
                    HasParameters(qry.Parameters,
                        NewParameter("@SpecificationId", specificationId),
                        NewParameter("@DateFrom", dateFrom))), 1))
                .ReturnsAsync(expectedNewResultsFlag
                    ? new[]
                    {
                        new object()
                    }
                    : new dynamic[0]);
        }

        private CosmosDbQueryParameter NewParameter(string name,
            object value) => new CosmosDbQueryParameter(name, value);

        private bool HasParameters(IEnumerable<CosmosDbQueryParameter> parameters,
            params CosmosDbQueryParameter[] expectedParameters) =>
            parameters.Count() == expectedParameters.Length &&
            expectedParameters.All(_ => parameters.Any(prm => prm.Name.Equals(_.Name) &&
                                                              prm.Value.Equals(_.Value)));

        private string NewRandomString() => new RandomString();

        private DateTimeOffset NewRandomDateTime() => new RandomDateTime();
    }
}