using CalculateFunding.Api.External.V4.Controllers;
using CalculateFunding.Api.External.V4.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.UnitTests.Version4.Controllers
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
            string contracting = "Contracting";

            await controller.GetPublishedProviderInformation(contracting, publishedProviderVersion);

            await publishedProviderRetrievalService
                .Received(1)
                .GetPublishedProviderInformation(Arg.Is(contracting),
                    Arg.Is(publishedProviderVersion));
        }
    }
}
