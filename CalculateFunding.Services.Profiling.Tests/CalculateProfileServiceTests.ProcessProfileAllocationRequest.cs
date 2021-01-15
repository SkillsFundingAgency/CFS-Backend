using CalculateFunding.Services.Core.Interfaces.Threading;
using Moq;
using Polly;

namespace CalculateFunding.Services.Profiling.Tests
{
    using System.Linq;
    using System.Threading.Tasks;
    using CalculateFunding.Common.Caching;
    using CalculateFunding.Services.Profiling.Models;
    using CalculateFunding.Services.Profiling.Repositories;
    using CalculateFunding.Services.Profiling.Services;
    using CalculateFunding.Services.Profiling.Tests.TestHelpers;
    using FluentAssertions;
    using FluentValidation;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSubstitute;
    using Serilog;

    public partial class CalculateProfileServiceTests
    {
        private const string NamespaceResourcesResideIn = "CalculateFunding.Services.Profiling.Tests";
        [TestMethod, TestCategory("UnitTest")]
        public async Task CalculateProfileService_ShouldCorrectlyCalculateNormalPESportsPremium()
        {
            // arrange
            FundingStreamPeriodProfilePattern pattern = TestResource.FromJson<FundingStreamPeriodProfilePattern>(
                NamespaceResourcesResideIn, "Resources.PESPORTSPREM.json");

            ProfileRequest peSportsPremReq = new ProfileRequest(
               fundingStreamId: "PSG",
                fundingPeriodId: "AY-1819",
                fundingLineCode: "FL1",
                fundingValue: 200);

            IProfilePatternRepository mockProfilePatternRepository = Substitute.For<IProfilePatternRepository>();
            mockProfilePatternRepository
                .GetProfilePattern(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(pattern);

            ICalculateProfileService calculateProfileService = GetCalculateProfileServiceWithMockedDependencies(mockProfilePatternRepository);

            // act
            IActionResult responseResult = await calculateProfileService.ProcessProfileAllocationRequest(peSportsPremReq);

            // assert
            responseResult
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult responseAsOkObjectResult = responseResult as OkObjectResult;
            AllocationProfileResponse response = responseAsOkObjectResult.Value as AllocationProfileResponse;

            DeliveryProfilePeriod deliveryProfilePeriodReturnedForOct = response.DeliveryProfilePeriods.ToArray().FirstOrDefault(q => q.TypeValue == "October");
            DeliveryProfilePeriod deliveryProfilePeriodReturnedForApr = response.DeliveryProfilePeriods.ToArray().FirstOrDefault(q => q.TypeValue == "April");

            response.DeliveryProfilePeriods.Length.Should().Be(3);

            deliveryProfilePeriodReturnedForOct
             .Should().NotBeNull();
            deliveryProfilePeriodReturnedForOct
             .ProfileValue
             .Should().Be(117M);

            deliveryProfilePeriodReturnedForApr
             .Should().NotBeNull();
            deliveryProfilePeriodReturnedForApr
             .ProfileValue
             .Should().Be(83M);
        }

        [TestMethod, TestCategory("UnitTest")]
        public async Task CalculateProfileService_ShouldCorrectlyCalculatePESportsPremium_WithOnlyMandatoryFields()
        {
            // arrange
            FundingStreamPeriodProfilePattern pattern = TestResource.FromJson<FundingStreamPeriodProfilePattern>(
             NamespaceResourcesResideIn, "Resources.PESPORTSPREM.json");

            ProfileRequest peSportsPremReq = new ProfileRequest(
                fundingStreamId: "PSG",
                fundingPeriodId: "AY-1819",
                fundingLineCode: "FL1",
                fundingValue: 200);

            IProfilePatternRepository mockProfilePatternRepository = Substitute.For<IProfilePatternRepository>();
            mockProfilePatternRepository
                .GetProfilePattern(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(pattern);

            ICalculateProfileService calculateProfileService = GetCalculateProfileServiceWithMockedDependencies(mockProfilePatternRepository);

            // act

            IActionResult responseResult = await calculateProfileService.ProcessProfileAllocationRequest(peSportsPremReq);

            // assert
            responseResult
                .Should().BeOfType<OkObjectResult>();

            OkObjectResult responseAsOkObjectResult = responseResult as OkObjectResult;
            AllocationProfileResponse response = responseAsOkObjectResult.Value as AllocationProfileResponse;

            // assert
            response.DeliveryProfilePeriods.ToArray().FirstOrDefault(q => q.TypeValue == "October").ProfileValue.Should().Be(117M);
            response.DeliveryProfilePeriods.ToArray().FirstOrDefault(q => q.TypeValue == "April").ProfileValue.Should().Be(83M);
            response.DeliveryProfilePeriods.ToArray().LastOrDefault(q => q.TypeValue == "April").ProfileValue.Should().Be(0M);
            response.DeliveryProfilePeriods.Length.Should().Be(3);
        }

        [TestMethod, TestCategory("UnitTest")]
        public async Task CalculateProfileService_ShouldCorrectlyCalculateNormalPESportsPremium_WithRounding()
        {
            // arrange
            FundingStreamPeriodProfilePattern pattern = TestResource.FromJson<FundingStreamPeriodProfilePattern>(
             NamespaceResourcesResideIn, "Resources.PESPORTSPREM.json");

            // first period rouonds down
            ProfileRequest peSportsPremReq1 = new ProfileRequest(
               fundingStreamId: "PSG",
                fundingPeriodId: "AY-1819",
                fundingLineCode: "FL1",
                fundingValue: 200);

            // first period rounds up
            ProfileRequest peSportsPremReq2 = new ProfileRequest(
                fundingStreamId: "PSG",
                fundingPeriodId: "AY-1819",
                fundingLineCode: "FL1",
                fundingValue: 500);

            ProfileRequest peSportsPremReq3 = new ProfileRequest(
                fundingStreamId: "PSG",
                fundingPeriodId: "AY-1819",
                fundingLineCode: "FL1",
                fundingValue: int.MaxValue);

            IProfilePatternRepository mockProfilePatternRepository = Substitute.For<IProfilePatternRepository>();
            mockProfilePatternRepository
                .GetProfilePattern(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(pattern);

            ICalculateProfileService calculateProfileService = GetCalculateProfileServiceWithMockedDependencies(mockProfilePatternRepository);

            // act
            IActionResult responseResult1 = await calculateProfileService.ProcessProfileAllocationRequest(peSportsPremReq1);
            IActionResult responseResult2 = await calculateProfileService.ProcessProfileAllocationRequest(peSportsPremReq2);
            IActionResult responseResult3 = await calculateProfileService.ProcessProfileAllocationRequest(peSportsPremReq3);

            // assert
            responseResult1
                .Should().BeOfType<OkObjectResult>();
            responseResult2
                .Should().BeOfType<OkObjectResult>();
            responseResult3
                .Should().BeOfType<OkObjectResult>();

            OkObjectResult responseAsOkObjectResult1 = responseResult1 as OkObjectResult;
            OkObjectResult responseAsOkObjectResult2 = responseResult2 as OkObjectResult;
            OkObjectResult responseAsOkObjectResult3 = responseResult3 as OkObjectResult;

            AllocationProfileResponse response1 = responseAsOkObjectResult1.Value as AllocationProfileResponse;
            AllocationProfileResponse response2 = responseAsOkObjectResult2.Value as AllocationProfileResponse;
            AllocationProfileResponse response3 = responseAsOkObjectResult3.Value as AllocationProfileResponse;

            response1.DeliveryProfilePeriods.ToArray().FirstOrDefault(q => q.TypeValue == "October").ProfileValue.Should().Be(117M);
            response1.DeliveryProfilePeriods.ToArray().FirstOrDefault(q => q.TypeValue == "April").ProfileValue.Should().Be(83M);
            response1.DeliveryProfilePeriods.ToArray().LastOrDefault(q => q.TypeValue == "April").ProfileValue.Should().Be(0M);
            response1.DeliveryProfilePeriods.Length.Should().Be(3);

            response2.DeliveryProfilePeriods.ToArray().FirstOrDefault(q => q.TypeValue == "October").ProfileValue.Should().Be(292M);
            response2.DeliveryProfilePeriods.ToArray().FirstOrDefault(q => q.TypeValue == "April").ProfileValue.Should().Be(208M);
            response2.DeliveryProfilePeriods.ToArray().LastOrDefault(q => q.TypeValue == "April").ProfileValue.Should().Be(0M);
            response2.DeliveryProfilePeriods.Length.Should().Be(3);

            response3.DeliveryProfilePeriods.ToArray().FirstOrDefault(q => q.TypeValue == "October").ProfileValue.Should().Be(1252698794M);
            response3.DeliveryProfilePeriods.ToArray().FirstOrDefault(q => q.TypeValue == "April").ProfileValue.Should().Be(894784853M);
            response3.DeliveryProfilePeriods.ToArray().LastOrDefault(q => q.TypeValue == "April").ProfileValue.Should().Be(0M);
            response3.DeliveryProfilePeriods.Length.Should().Be(3);
        }

        [TestMethod, TestCategory("UnitTest")]
        public async Task CalculateProfileService_ShouldCorrectlyCalculateNormalPESportsPremium_WithRoundingFromHalfway()
        {
            // arrange
            FundingStreamPeriodProfilePattern pattern = TestResource.FromJson<FundingStreamPeriodProfilePattern>(
             NamespaceResourcesResideIn, "Resources.PESPORTSPREM.json");

            // first period rouonds down
            ProfileRequest peSportsPremReq1 = new ProfileRequest(
               fundingStreamId: "PSG",
                fundingPeriodId: "AY-1819",
                fundingLineCode: "FL1",
                fundingValue: 18);

            IProfilePatternRepository mockProfilePatternRepository = Substitute.For<IProfilePatternRepository>();
            mockProfilePatternRepository
                .GetProfilePattern(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(pattern);

            ICalculateProfileService calculateProfileService = GetCalculateProfileServiceWithMockedDependencies(mockProfilePatternRepository);

            // act
            IActionResult responseResult = await calculateProfileService.ProcessProfileAllocationRequest(peSportsPremReq1);

            // assert
            responseResult
                .Should().BeOfType<OkObjectResult>();
            OkObjectResult responseAsOkObjectResult = responseResult as OkObjectResult;

            AllocationProfileResponse response1 = responseAsOkObjectResult.Value as AllocationProfileResponse;

            response1.DeliveryProfilePeriods.ToArray().FirstOrDefault(q => q.TypeValue == "October").ProfileValue.Should().Be(11M);
            response1.DeliveryProfilePeriods.ToArray().FirstOrDefault(q => q.TypeValue == "April").ProfileValue.Should().Be(7M);
            response1.DeliveryProfilePeriods.ToArray().LastOrDefault(q => q.TypeValue == "April").ProfileValue.Should().Be(0M);
            response1.DeliveryProfilePeriods.Length.Should().Be(3);
        }

        [TestMethod, TestCategory("UnitTest")]
        public async Task CalculateProfileService_ShouldCorrectlyProfileFullLengthAllocation()
        {
            // arrange
            FundingStreamPeriodProfilePattern pattern = TestResource.FromJson<FundingStreamPeriodProfilePattern>(
             NamespaceResourcesResideIn, "Resources.PESPORTSPREM.json");

            // first period rouonds down
            ProfileRequest peSportsPremReq1 = new ProfileRequest(
               fundingStreamId: "PSG",
                fundingPeriodId: "AY-1819",
                fundingLineCode: "FL1",
                fundingValue: 10000000);

            IProfilePatternRepository mockProfilePatternRepository = Substitute.For<IProfilePatternRepository>();
            mockProfilePatternRepository
                .GetProfilePattern(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(pattern);

            ICalculateProfileService calculateProfileService = GetCalculateProfileServiceWithMockedDependencies(mockProfilePatternRepository);

            // act
            IActionResult responseResult = await calculateProfileService.ProcessProfileAllocationRequest(peSportsPremReq1);

            // assert
            responseResult
                .Should().BeOfType<OkObjectResult>();

            OkObjectResult responseAsOkObjectResult = responseResult as OkObjectResult;
            AllocationProfileResponse response = responseAsOkObjectResult.Value as AllocationProfileResponse;

            response.DeliveryProfilePeriods.ToArray().FirstOrDefault(q => q.TypeValue == "October").ProfileValue.Should().Be(5833333M);
            response.DeliveryProfilePeriods.ToArray().FirstOrDefault(q => q.TypeValue == "April").ProfileValue.Should().Be(4166667M);
            response.DeliveryProfilePeriods.ToArray().LastOrDefault(q => q.TypeValue == "April").ProfileValue.Should().Be(0M);
            response.DeliveryProfilePeriods.Length.Should().Be(3);
        }

        [TestMethod, TestCategory("UnitTest")]
        public async Task CalculateProfileService_ShouldCorrectlyProfileFullLengthAllocationWithRoundUp()
        {
            // arrange
            FundingStreamPeriodProfilePattern pattern = TestResource.FromJson<FundingStreamPeriodProfilePattern>(
             NamespaceResourcesResideIn, "Resources.DSG.json");

            // first period rouonds down
            ProfileRequest peSportsPremReq1 = new ProfileRequest(
               fundingStreamId: "DSG",
                fundingPeriodId: "FY-2021",
                fundingLineCode: "DSG-002",
                fundingValue: 10000543);

            IProfilePatternRepository mockProfilePatternRepository = Substitute.For<IProfilePatternRepository>();
            mockProfilePatternRepository
                .GetProfilePattern(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(pattern);

            ICalculateProfileService calculateProfileService = GetCalculateProfileServiceWithMockedDependencies(mockProfilePatternRepository);

            // act
            IActionResult responseResult = await calculateProfileService.ProcessProfileAllocationRequest(peSportsPremReq1);

            // assert
            responseResult
                .Should().BeOfType<OkObjectResult>();

            OkObjectResult responseAsOkObjectResult = responseResult as OkObjectResult;
            AllocationProfileResponse response = responseAsOkObjectResult.Value as AllocationProfileResponse;

            response.DeliveryProfilePeriods.ToArray().FirstOrDefault(q => q.TypeValue == "April").ProfileValue.Should().Be(400021M);
            response.DeliveryProfilePeriods.ToArray().LastOrDefault(q => q.TypeValue == "March").ProfileValue.Should().Be(400039M);
            response.DeliveryProfilePeriods.Length.Should().Be(25);
        }

        [TestMethod, TestCategory("UnitTest")]
        public async Task CalculateProfileService_ShouldCorrectlyProfileAllocationStartsLate()
        {
            // arrange
            FundingStreamPeriodProfilePattern pattern = TestResource.FromJson<FundingStreamPeriodProfilePattern>(
             NamespaceResourcesResideIn, "Resources.PESPORTSPREM.json");

            // first period rouonds down
            ProfileRequest peSportsPremReq1 = new ProfileRequest(
                fundingStreamId: "PSG",
                fundingPeriodId: "AY-1819",
                fundingLineCode: "FL1",
                fundingValue: 1000);

            IProfilePatternRepository mockProfilePatternRepository = Substitute.For<IProfilePatternRepository>();
            mockProfilePatternRepository
                .GetProfilePattern(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(pattern);

            ICalculateProfileService calculateProfileService = GetCalculateProfileServiceWithMockedDependencies(mockProfilePatternRepository);

            // act 
            IActionResult responseResult = await calculateProfileService.ProcessProfileAllocationRequest(peSportsPremReq1);

            // assert
            responseResult
                .Should().BeOfType<OkObjectResult>();

            OkObjectResult responseAsOkObjectResult = responseResult as OkObjectResult;
            AllocationProfileResponse response = responseAsOkObjectResult.Value as AllocationProfileResponse;

            response.DeliveryProfilePeriods.ToArray().FirstOrDefault(q => q.TypeValue == "April").ProfileValue.Should().Be(417M);
            response.DeliveryProfilePeriods.Length.Should().Be(3);
        }

        [TestMethod, TestCategory("UnitTest")]
        public async Task CalculateProfileService_ShouldCorrectlyProfileEdgeCaseLowValue()
        {
            // arrange
            FundingStreamPeriodProfilePattern pattern = TestResource.FromJson<FundingStreamPeriodProfilePattern>(
             NamespaceResourcesResideIn, "Resources.PESPORTSPREM.json");

            // first period rouonds down
            ProfileRequest peSportsPremReq1 = new ProfileRequest(
                fundingStreamId: "PSG",
                fundingPeriodId: "AY-1819",
                fundingLineCode: "FL1",
                fundingValue: 1);

            IProfilePatternRepository mockProfilePatternRepository = Substitute.For<IProfilePatternRepository>();
            mockProfilePatternRepository
                .GetProfilePattern(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(pattern);


            ICalculateProfileService calculateProfileService = GetCalculateProfileServiceWithMockedDependencies(mockProfilePatternRepository);

            // act
            IActionResult responseResult = await calculateProfileService.ProcessProfileAllocationRequest(peSportsPremReq1);

            // assert
            responseResult
                .Should().BeOfType<OkObjectResult>();

            OkObjectResult responseAsOkObjectResult = responseResult as OkObjectResult;
            AllocationProfileResponse response = responseAsOkObjectResult.Value as AllocationProfileResponse;

            response.DeliveryProfilePeriods.ToArray().FirstOrDefault(q => q.TypeValue == "October").ProfileValue.Should().Be(1M);
            response.DeliveryProfilePeriods.ToArray().FirstOrDefault(q => q.TypeValue == "April").ProfileValue.Should().Be(0M);
            response.DeliveryProfilePeriods.Length.Should().Be(3);
        }

        //     // parked until we have confirmation of expected behaviour
        //     //[TestMethod, TestCategory("UnitTest")]
        //     //public void CalculateProfileService_ShouldCorrectlyProfileEdgeCaseDurationMissesPattern()
        //     //{
        //     //    // arrange
        //     //    FundingStreamPeriodProfilePattern pattern = TestResource.FromJson<FundingStreamPeriodProfilePattern>(
        //     //        "AllocationProfilingService.Web.Tests", "Resources.PESPORTSPREM.json");

        //     //    // first period rouonds down
        //     //    var peSportsPremReq1 = new ProfileRequest(
        //     //        allocationOrganisation: null,
        //     //        fundingStreamPeriod: "PSG1819",
        //     //        allocationStartDate: new DateTime(2014, 11, 1),
        //     //        allocationEndDate: new DateTime(2015, 3, 30),
        //     //        allocationValueByDistributionPeriod: new[]
        //     //        {
        //     //            new ProfileRequestPeriodValue("AY1819", 1000.00M)
        //     //        });

        //     //    // act
        //     //    var response1 = CalculateProfileService.ProfileAllocation(peSportsPremReq1, pattern);

        //     //    // assert
        //     //    Assert.AreEqual(response1.DeliveryProfilePeriods.Length, 0);
        //     //}

        [TestMethod, TestCategory("UnitTest")]
        public async Task CalculateProfileService_ShouldCorrectlyProfileEdgeCaseAllocationJustWithinPatternMonths()
        {
            // arrange
            FundingStreamPeriodProfilePattern pattern = TestResource.FromJson<FundingStreamPeriodProfilePattern>(
             NamespaceResourcesResideIn, "Resources.PESPORTSPREM.json");

            // first period rouonds down
            ProfileRequest peSportsPremReq1 = new ProfileRequest(
                fundingStreamId: "PSG",
                fundingPeriodId: "AY-1819",
                fundingLineCode: "FL1",
                fundingValue: 1000);

            IProfilePatternRepository mockProfilePatternRepository = Substitute.For<IProfilePatternRepository>();
            mockProfilePatternRepository
                .GetProfilePattern(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(pattern);

            ICalculateProfileService calculateProfileService = GetCalculateProfileServiceWithMockedDependencies(mockProfilePatternRepository);

            // act
            IActionResult responseResult = await calculateProfileService.ProcessProfileAllocationRequest(peSportsPremReq1);

            // assert
            responseResult
                .Should().BeOfType<OkObjectResult>();

            OkObjectResult responseAsOkObjectResult = responseResult as OkObjectResult;
            AllocationProfileResponse response = responseAsOkObjectResult.Value as AllocationProfileResponse;

            response.DeliveryProfilePeriods.ToArray().FirstOrDefault(q => q.TypeValue == "October").ProfileValue.Should().Be(583.00M);
            response.DeliveryProfilePeriods.ToArray().FirstOrDefault(q => q.TypeValue == "April").ProfileValue.Should().Be(417.00M);
            response.DeliveryProfilePeriods.Length.Should().Be(3);
        }

        [TestMethod, TestCategory("UnitTest")]
        public async Task CalculateProfileService_ShouldCorrectlyProfileEdgeCaseOfFiftyPence()
        {
            // arrange
            FundingStreamPeriodProfilePattern pattern = TestResource.FromJson<FundingStreamPeriodProfilePattern>(
             NamespaceResourcesResideIn, "Resources.PESPORTSPREM.json");

            // first period rouonds down
            ProfileRequest peSportsPremReq1 = new ProfileRequest(
                fundingStreamId: "PSG",
                fundingPeriodId: "AY-1819",
                fundingLineCode: "FL1",
                fundingValue: 16);

            IProfilePatternRepository mockProfilePatternRepository = Substitute.For<IProfilePatternRepository>();
            mockProfilePatternRepository
                .GetProfilePattern(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(pattern);

            ICalculateProfileService calculateProfileService = GetCalculateProfileServiceWithMockedDependencies(mockProfilePatternRepository);

            // act
            IActionResult responseResult = await calculateProfileService.ProcessProfileAllocationRequest(peSportsPremReq1);

            // assert
            responseResult
                .Should().BeOfType<OkObjectResult>();

            OkObjectResult responseAsOkObjectResult = responseResult as OkObjectResult;
            AllocationProfileResponse response = responseAsOkObjectResult.Value as AllocationProfileResponse;

            response.DeliveryProfilePeriods.ToArray().FirstOrDefault(q => q.TypeValue == "October").ProfileValue.Should().Be(9M);
            response.DeliveryProfilePeriods.ToArray().FirstOrDefault(q => q.TypeValue == "April").ProfileValue.Should().Be(7M);
            response.DeliveryProfilePeriods.ToArray().LastOrDefault(q => q.TypeValue == "April").ProfileValue.Should().Be(0M);
            response.DeliveryProfilePeriods.Length.Should().Be(3);
        }

        [TestMethod, TestCategory("UnitTest")]
        public async Task CalculateProfileService_ProcessProfileAllocationRequest_WhenFinancialEnvelopeShouldBeReturned_ThenObjectSetSuccessfully()
        {
            // arrange
            FundingStreamPeriodProfilePattern pattern = TestResource.FromJson<FundingStreamPeriodProfilePattern>(
             NamespaceResourcesResideIn, "Resources.PESPORTSPREM.json");

            // first period rouonds down
            ProfileRequest peSportsPremReq1 = new ProfileRequest(
               fundingStreamId: "PSG",
                fundingPeriodId: "AY-1819",
                fundingLineCode: "FL1",
                fundingValue: 6);

            IProfilePatternRepository mockProfilePatternRepository = Substitute.For<IProfilePatternRepository>();
            mockProfilePatternRepository
                .GetProfilePattern(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(pattern);

            ICalculateProfileService calculateProfileService = GetCalculateProfileServiceWithMockedDependencies(mockProfilePatternRepository);

            // act
            IActionResult responseResult = await calculateProfileService.ProcessProfileAllocationRequest(peSportsPremReq1);

            // assert
            responseResult
                .Should()
                .BeOfType<OkObjectResult>();

            OkObjectResult responseAsOkObjectResult = responseResult as OkObjectResult;
            AllocationProfileResponse response = responseAsOkObjectResult.Value as AllocationProfileResponse;

            response
                .DeliveryProfilePeriods
                .Should()
                .NotBeNull();

            response
               .DeliveryProfilePeriods
               .Should()
               .HaveCount(3);

        }


        private ICalculateProfileService GetCalculateProfileServiceWithMockedDependencies(
            IProfilePatternRepository profilePatternRepository = null,
            ICacheProvider cacheProvider = null,
            ILogger logger = null)
        {
            return new CalculateProfileService(
                profilePatternRepository ?? Substitute.For<IProfilePatternRepository>(),
                cacheProvider ?? Substitute.For<ICacheProvider>(),
                new Mock<IValidator<ProfileBatchRequest>>().Object,
                logger ?? Substitute.For<ILogger>(),
                new ProfilingResiliencePolicies
                {
                    Caching = Policy.NoOpAsync(),
                    ProfilePatternRepository = Policy.NoOpAsync()
                },
                new Mock<IProducerConsumerFactory>().Object,
                new FundingValueProfiler());
        }
    }
}
