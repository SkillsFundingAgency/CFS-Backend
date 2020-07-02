using CalculateFunding.Api.External.V3.Controllers;
using CalculateFunding.Api.External.V3.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.UnitTests.Version3.Controllers
{
    [TestClass]
    public class ProviderControllerTests
    {
        [TestMethod]
        public async Task GetPublishedProviderInformation_CallsCorrectly()
        {
            IPublishedProviderRetrievalService publishedProviderRetrievalService = Substitute.For<IPublishedProviderRetrievalService>();

            ProviderController controller = new ProviderController(publishedProviderRetrievalService);

            string publishedProviderVersion = "1234";

            await controller.GetPublishedProviderInformation(publishedProviderVersion);

            await publishedProviderRetrievalService
                .Received(1)
                .GetPublishedProviderInformation(Arg.Is(publishedProviderVersion));
        }
    }
}
