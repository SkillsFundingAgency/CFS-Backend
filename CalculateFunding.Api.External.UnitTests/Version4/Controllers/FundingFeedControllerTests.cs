using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Api.External.V4.Controllers;
using CalculateFunding.Api.External.V4.Interfaces;
using CalculateFunding.Api.External.V4.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Api.External.UnitTests.Version4.Controllers
{
    [TestClass]
    public class FundingFeedControllerTests
    {
        private const string contracting = "Contracting";

        [TestMethod]
        public async Task GetFunding_CallsCorrectlyForPayment()
        {
            IFundingFeedService fundingFeedService = Substitute.For<IFundingFeedService>();

            FundingFeedController controller = new FundingFeedController(
                fundingFeedService);

            string[] fundingStreamIds = new string[] { "1234" };
            string[] fundingPeriodIds = new string[] { "1234" };
            GroupingReason[] groupReasons = new GroupingReason[] { GroupingReason.Payment };
            VariationReason[] variationReasons = new VariationReason[] { VariationReason.AuthorityFieldUpdated };

            await controller.GetFunding(contracting, fundingStreamIds, fundingPeriodIds, groupReasons, variationReasons, 5, CancellationToken.None) ;

            await fundingFeedService
                .Received(1)
                .GetFundingNotificationFeedPage(Arg.Any<HttpRequest>(),
                    Arg.Any<HttpResponse>(),
                    null,
                    Arg.Is(contracting),
                    Arg.Is(fundingStreamIds),
                    Arg.Is(fundingPeriodIds),
                    Arg.Is(groupReasons),
                    Arg.Is(variationReasons),
                    Arg.Is(5),
                    CancellationToken.None);
        }

        [TestMethod]
        public async Task GetFunding_CallsCorrectlyForInformation()
        {
            IFundingFeedService fundingFeedService = Substitute.For<IFundingFeedService>();

            FundingFeedController controller = new FundingFeedController(
                fundingFeedService);

            string[] fundingStreamIds = new string[] { "1234" };
            string[] fundingPeriodIds = new string[] { "1234" };
            GroupingReason[] groupReasons = new GroupingReason[] { GroupingReason.Information };
            VariationReason[] variationReasons = new VariationReason[] { VariationReason.AuthorityFieldUpdated };

            await controller.GetFunding(contracting, fundingStreamIds, fundingPeriodIds, groupReasons, variationReasons, 5, CancellationToken.None);

            await fundingFeedService
                .Received(1)
                .GetFundingNotificationFeedPage(Arg.Any<HttpRequest>(),
                    Arg.Any<HttpResponse>(),
                    null,
                    Arg.Is(contracting),
                    Arg.Is(fundingStreamIds),
                    Arg.Is(fundingPeriodIds),
                    Arg.Is(groupReasons),
                    Arg.Is(variationReasons),
                    Arg.Is(5),
                    CancellationToken.None);
        }

        [TestMethod]
        public async Task GetFunding_CallsCorrectlyForContracting()
        {
            IFundingFeedService fundingFeedService = Substitute.For<IFundingFeedService>();

            FundingFeedController controller = new FundingFeedController(
                fundingFeedService);

            string[] fundingStreamIds = new string[] { "1234" };
            string[] fundingPeriodIds = new string[] { "1234" };
            GroupingReason[] groupReasons = new GroupingReason[] { GroupingReason.Contracting };
            VariationReason[] variationReasons = new VariationReason[] { VariationReason.AuthorityFieldUpdated };

            await controller.GetFunding(contracting, fundingStreamIds, fundingPeriodIds, groupReasons, variationReasons, 5, CancellationToken.None);

            await fundingFeedService
                .Received(1)
                .GetFundingNotificationFeedPage(Arg.Any<HttpRequest>(),
                    Arg.Any<HttpResponse>(),
                    null,
                    Arg.Is(contracting),
                    Arg.Is(fundingStreamIds),
                    Arg.Is(fundingPeriodIds),
                    Arg.Is(groupReasons),
                    Arg.Is(variationReasons),
                    Arg.Is(5),
                    CancellationToken.None);
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

            await controller.GetFundingPage(contracting, fundingStreamIds, fundingPeriodIds, groupReasons, variationReasons, 5, 1, CancellationToken.None);

            await fundingFeedService
                .Received(1)
                .GetFundingNotificationFeedPage(Arg.Any<HttpRequest>(),
                    Arg.Any<HttpResponse>(),
                    Arg.Is(1),
                    Arg.Is(contracting),
                    Arg.Is(fundingStreamIds),
                    Arg.Is(fundingPeriodIds),
                    Arg.Is(groupReasons),
                    Arg.Is(variationReasons),
                    Arg.Is(5),
                    CancellationToken.None);
        }
    }
}
