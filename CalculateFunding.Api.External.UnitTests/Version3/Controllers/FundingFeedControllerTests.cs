using System.Threading.Tasks;
using CalculateFunding.Api.External.V3.Controllers;
using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Api.External.V3.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Api.External.UnitTests.Version3.Controllers
{
    [TestClass]
    public class FundingFeedControllerTests
    {
        [TestMethod]
        public async Task GetFunding_CallsCorrectly()
        {
            IFundingFeedService fundingFeedService = Substitute.For<IFundingFeedService>();

            FundingFeedController controller = new FundingFeedController(
                fundingFeedService);

            string[] fundingStreamIds = new string[] { "1234" };
            string[] fundingPeriodIds = new string[] { "1234" };
            GroupingReason[] groupReasons = new GroupingReason[] { GroupingReason.Payment };
            VariationReason[] variationReasons = new VariationReason[] { VariationReason.AuthorityFieldUpdated };

            await controller.GetFunding(fundingStreamIds, fundingPeriodIds, groupReasons, variationReasons, 5);

            await fundingFeedService
                .Received(1)
                .GetFunding(Arg.Any<HttpRequest>(),  null, Arg.Is(fundingStreamIds), Arg.Is(fundingPeriodIds), Arg.Is(groupReasons), Arg.Is(variationReasons), Arg.Is(5));
        }

        [TestMethod]
        public async Task GetFundingPage_CallsCorrectly()
        {
            IFundingFeedService fundingFeedService = Substitute.For<IFundingFeedService>();

            FundingFeedController controller = new FundingFeedController(
                fundingFeedService);

            string[] fundingStreamIds = new string[] { "1234" };
            string[] fundingPeriodIds = new string[] { "1234" };
            GroupingReason[] groupReasons = new GroupingReason[] { GroupingReason.Payment };
            VariationReason[] variationReasons = new VariationReason[] { VariationReason.AuthorityFieldUpdated };

            await controller.GetFundingPage(fundingStreamIds, fundingPeriodIds, groupReasons, variationReasons, 5, 1);

            await fundingFeedService
                .Received(1)
                .GetFunding(Arg.Any<HttpRequest>(), Arg.Is(1), Arg.Is(fundingStreamIds), Arg.Is(fundingPeriodIds), Arg.Is(groupReasons), Arg.Is(variationReasons), Arg.Is(5));
        }
    }
}
