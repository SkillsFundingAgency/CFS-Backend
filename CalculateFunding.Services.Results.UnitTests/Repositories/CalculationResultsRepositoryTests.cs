using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Results.Repositories;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.UnitTests.Repositories
{
    [TestClass]
    public class CalculationResultsRepositoryTests
    {
        private CalculationResultsRepository _calculationResultsRepository;
        private EngineSettings _engineSettings;
        private Mock<ICosmosRepository> _mockCosmosRepository; 

        [TestInitialize]
        public void Initialize()
        {
            _engineSettings = new EngineSettings();
            _mockCosmosRepository = new Mock<ICosmosRepository>();

            _calculationResultsRepository 
                = new CalculationResultsRepository(_mockCosmosRepository.Object, _engineSettings);
        }

        [TestMethod]
        public async Task ProviderResultsBatchProcessing_GeneratesCorrectQuery()
        {
            string specificationId = NewRandomString();

            await WhenProviderResultsBatchProcessing(specificationId, providerResults => { return Task.CompletedTask; }, 1000);

            _mockCosmosRepository
                .Verify(_ => _.DocumentsBatchProcessingAsync(
                    It.IsAny<Func<List<ProviderResult>, Task>>(), 
                    It.Is<CosmosDbQuery>(_ => 
                        _.Parameters.Count() == 1 && _.Parameters.SingleOrDefault().Value.ToString() == specificationId &&
                        _.QueryText == $@"SELECT
                                c.id as id,
                                c.createdAt as createdAt,
                                c.content.specificationId as specificationId,
                                {{
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
                                ""dateOpened"" : c.content.provider.dateOpened }} AS provider,
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

        private async Task WhenProviderResultsBatchProcessing(string specificationId, Func<List<ProviderResult>, Task> processProcessProviderResultsBatch, int itemsPerPage = 1000)
        {
            await _calculationResultsRepository.ProviderResultsBatchProcessing(specificationId, processProcessProviderResultsBatch, itemsPerPage);
        }

        private string NewRandomString() => new RandomString();

    }
}
