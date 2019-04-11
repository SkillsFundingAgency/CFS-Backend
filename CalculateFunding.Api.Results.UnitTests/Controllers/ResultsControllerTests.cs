using CalculateFunding.Api.Results.Controllers;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Threading.Tasks;

namespace CalculateFunding.Api.Results.UnitTests.Controllers
{
    [TestClass]
    public class ResultsControllerTests
    {
        [TestMethod]
        [DataRow(null, null, null)]
        [DataRow("a", "b", "c")]
        public async Task GetPublishedProviderProfileForProviderIdAndSpecificationIdAndFundingStreamId_CallsCorrectly(
            string providerId,
            string specificationId,
            string fundingStreamId)
        {
            IResultsService resultsService = Substitute.For<IResultsService>();
            IResultsSearchService resultsSearchService = Substitute.For<IResultsSearchService>();
            ICalculationProviderResultsSearchService calculationProviderResultsSearchService = Substitute.For<ICalculationProviderResultsSearchService>();
            IPublishedResultsService publishedResultsService = Substitute.For<IPublishedResultsService>();
            IProviderCalculationResultsSearchService providerCalculationsResultsSearchService = Substitute.For<IProviderCalculationResultsSearchService>();
            IFeatureToggle featureToggle = Substitute.For<IFeatureToggle>();
            IProviderCalculationResultsReIndexerService providerCalculationResultsReIndexerService = Substitute.For<IProviderCalculationResultsReIndexerService>();

            ResultsController controller = new ResultsController(
                resultsService,
                resultsSearchService,
                calculationProviderResultsSearchService,
                publishedResultsService,
                providerCalculationsResultsSearchService,
                featureToggle,
                providerCalculationResultsReIndexerService);

            await controller.GetPublishedProviderProfileForProviderIdAndSpecificationIdAndFundingStreamId(providerId, specificationId, fundingStreamId);

            await publishedResultsService
                .Received(1)
                .GetPublishedProviderProfileForProviderIdAndSpecificationIdAndFundingStreamId(providerId, specificationId, fundingStreamId);

            //Moq has a .VerifyNoOtherCalls method which would be really useful here to confirm the others weren't called.
        }
    }
}
