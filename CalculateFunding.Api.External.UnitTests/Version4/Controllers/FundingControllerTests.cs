using System.Threading.Tasks;
using CalculateFunding.Api.External.V4.Controllers;
using CalculateFunding.Api.External.V4.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Api.External.UnitTests.Version4.Controllers
{
    [TestClass]
    public class FundingControllerTests
    {
        [TestMethod]
        public async Task GetFunding_CallsCorrectly()
        {
            string id = "1234";
            string contracting = "Contracting";

            IFundingFeedItemByIdService fundingService = Substitute.For<IFundingFeedItemByIdService>();

            FundingFeedItemControllerV4 controller = new FundingFeedItemControllerV4(
                fundingService);

            await controller.GetFunding(contracting, id);

            await fundingService
                .Received(1)
                .GetFundingByFundingResultId(Arg.Is(contracting), Arg.Is(id));
        }
    }
}
