using CalculateFunding.Api.External.V3.Controllers;
using CalculateFunding.Api.External.V3.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.UnitTests.Version3.Controllers
{
    [TestClass]
    public class ProviderFundingVersionControllerTests
    {
        [TestMethod]
        public async Task GetFunding_CallsCorrectly()
        {
            IProviderFundingVersionService providerFundingVersionService = Substitute.For<IProviderFundingVersionService>();

            ProviderFundingVersionController controller = new ProviderFundingVersionController(
                providerFundingVersionService);

            string providerFundingVersion = "1234";

            await controller.GetFunding(providerFundingVersion);

            await providerFundingVersionService
                .Received(1)
                .GetProviderFundingVersion(Arg.Is(providerFundingVersion));
        }

        [TestMethod]
        public async Task GetFundings_CallsCorrectly()
        {
            IProviderFundingVersionService providerFundingVersionService = Substitute.For<IProviderFundingVersionService>();

            ProviderFundingVersionController controller = new ProviderFundingVersionController(
                providerFundingVersionService);

            string publishedProviderVersion = "1234";

            await controller.GetFundings(publishedProviderVersion);

            await providerFundingVersionService
                .Received(1)
                .GetFundings(Arg.Is(publishedProviderVersion));
        }
    }
}
