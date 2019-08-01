using System.Threading.Tasks;
using CalculateFunding.Api.External.V3.Controllers;
using CalculateFunding.Api.External.V3.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Api.External.UnitTests.Version3.Controllers
{
    [TestClass]
    public class FundingControllerTests
    {
        [TestMethod]
        public async Task GetFunding_CallsCorrectly()
        {
            string id = "1234";

            IFundingFeedItemByIdService fundingService = Substitute.For<IFundingFeedItemByIdService>();

            FundingFeedItemController controller = new FundingFeedItemController(
                fundingService);

            await controller.GetFunding(id);

            await fundingService
                .Received(1)
                .GetFundingByFundingResultId(Arg.Is(id));
        }
    }
}
