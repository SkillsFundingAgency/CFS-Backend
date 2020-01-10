using CalculateFunding.Api.Providers.Controllers;
using CalculateFunding.Services.Providers.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Threading.Tasks;

namespace CalculateFunding.Api.Providers.UnitTests.Controllers
{
    [TestClass]
    public class ProvidersControllerTests
    {
        [TestMethod]
        public async Task GetLocalAuthorityNames_CallsCorrectly()
        {
            IProviderVersionSearchService providerVersionSearchService = Substitute.For<IProviderVersionSearchService>();

            ProvidersController controller = new ProvidersController(
                providerVersionSearchService);

            await controller.GetLocalAuthorityNames();

            await providerVersionSearchService
                .Received(1)
                .GetFacetValues("authority");
        }
    }
}
