using CalculateFunding.Api.External.V4.Controllers;
using CalculateFunding.Api.External.V4.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.UnitTests.Version4.Controllers
{
    [TestClass]
    public class ProviderFundingVersionControllerTests
    {
        private const string contracting = "Contracting";

        [TestMethod]
        public async Task GetFunding_CallsCorrectly()
        {
            IProviderFundingVersionService providerFundingVersionService = Substitute.For<IProviderFundingVersionService>();

            ProviderFundingVersionController controller = new ProviderFundingVersionController(
                providerFundingVersionService);

            string providerFundingVersion = "1234";

            await controller.GetFunding(contracting, providerFundingVersion);

            await providerFundingVersionService
                .Received(1)
                .GetProviderFundingVersion(Arg.Is(contracting), Arg.Is(providerFundingVersion));
        }

        [TestMethod]
        public async Task GetFundings_CallsCorrectly()
        {
            IProviderFundingVersionService providerFundingVersionService = Substitute.For<IProviderFundingVersionService>();

            ProviderFundingVersionController controller = new ProviderFundingVersionController(
                providerFundingVersionService);

            string publishedProviderVersion = "1234";

            await controller.GetFundings(contracting, publishedProviderVersion);

            await providerFundingVersionService
                .Received(1)
                .GetFundings(Arg.Is(contracting), Arg.Is(publishedProviderVersion));
        }
    }
}
