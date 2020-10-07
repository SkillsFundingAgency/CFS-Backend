//Would not surface anything that the GWTs wouldn't. To do later

//using System;
//using System.Net;
//using System.Net.Http;
//using System.Text;
//using CalculateFunding.Api.Profiling.Tests.TestHelpers;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Newtonsoft.Json;

//namespace CalculateFunding.Api.Profiling.Tests
//{
//    [TestClass]
//    public class ProfilingControllerTests
//    {
//        private const string ProfilingUrl = "api/profiling";

//        [TestMethod, TestCategory("IntegrationTest")]
//        public void ProfilingController_ShouldReturnProfileWithMinimalInput()
//        {
//            // arrange
//            var request = new ProfileRequest(
//                allocationOrganisation: null,
//                fundingStreamPeriod: "PSG1819",
//                allocationStartDate: null,
//                allocationEndDate: null,
//                allocationValueByDistributionPeriod: new[]
//                {
//                    new ProfileRequestPeriodValue("AY1819", 12000.00M),
//                });

//            using (var client = HttpHelpers.GetAuthorizedClient())
//            {
//                // act
//                var result = client.PostAsync(ProfilingUrl,
//                    new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json")).Result;

//                // assert
//                var resultContent = result.Content.ReadAsStringAsync().Result;
//                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode, "API did not respond with 200 OK");

//                string jsonContent = result.Content.ReadAsStringAsync().Result;

//                var response = JsonConvert.DeserializeObject<AllocationProfileResponse>(jsonContent);

//                Assert.AreEqual(expected: 7000M, actual: response.DeliveryProfilePeriods.FirstOrDefault(q => q.Period == "Oct").ProfileValue);
//                Assert.AreEqual(expected: 5000M, actual: response.DeliveryProfilePeriods.FirstOrDefault(q => q.Period == "Apr").ProfileValue);
//                Assert.AreEqual(response.DeliveryProfilePeriods.Length, 2);
//            }
//        }

//        [TestMethod, TestCategory("IntegrationTest")]
//        public void ProfilingController_ShouldReturnProfileWithFullInput()
//        {
//            // arrange
//            var request = new ProfileRequest(
//                allocationOrganisation: null,
//                fundingStreamPeriod: "PSG1819",
//                allocationStartDate: new DateTime(2018, 8, 1),
//                allocationEndDate: new DateTime(2019, 7, 31),
//                allocationValueByDistributionPeriod: new[]
//                {
//                    new ProfileRequestPeriodValue("AY1819", 24000.00M),
//                });

//            using (var client = HttpHelpers.GetAuthorizedClient())
//            {
//                // act
//                var result = client.PostAsync(ProfilingUrl,
//                    new StringContent(JsonConvert.SerializeObject(request))).Result;

//                // assert
//                Assert.AreEqual(HttpStatusCode.OK, result.StatusCode, "API did not respond with 200 OK");

//                string jsonContent = result.Content.ReadAsStringAsync().Result;

//                var response = JsonConvert.DeserializeObject<AllocationProfileResponse>(jsonContent);

//                Assert.AreEqual(expected: 14000M, actual: response.DeliveryProfilePeriods.FirstOrDefault(q => q.Period == "Oct").ProfileValue);
//                Assert.AreEqual(expected: 10000M, actual: response.DeliveryProfilePeriods.FirstOrDefault(q => q.Period == "Apr").ProfileValue);
//                Assert.AreEqual(response.DeliveryProfilePeriods.Length, 2);
//            }
//        }

//        [TestMethod, TestCategory("IntegrationTest")]
//        public void ProfilingController_ShouldBeSecureEndpoint()
//        {
//            var request = new ProfileRequest(
//                allocationOrganisation: null,
//                fundingStreamPeriod: "PSG1819",
//                allocationStartDate: new DateTime(2018, 8, 1),
//                allocationEndDate: new DateTime(2019, 7, 31),
//                allocationValueByDistributionPeriod: new[]
//                {
//                    new ProfileRequestPeriodValue("AY1819", 24000.00M),
//                });

//            using (var client = HttpHelpers.GetUnauthorizedClient())
//            {
//                var result = client.PostAsync(ProfilingUrl, new StringContent(JsonConvert.SerializeObject(request)))
//                    .Result;

//                Assert.AreEqual(HttpStatusCode.Unauthorized, result.StatusCode, "API did not respond with Unauthorized response");
//            }
//        }

//        [TestMethod, TestCategory("IntegrationTest")]
//        public void ProfilingController_ShouldReturnErrorIfFspIsNull()
//        {
//            var request = new ProfileRequest(
//                allocationOrganisation: null,
//                fundingStreamPeriod: null,
//                allocationStartDate: new DateTime(2018, 8, 1),
//                allocationEndDate: new DateTime(2019, 7, 31),
//                allocationValueByDistributionPeriod: new[]
//                {
//                    new ProfileRequestPeriodValue("AY1819", 24000.00M),
//                });

//            using (var client = HttpHelpers.GetAuthorizedClient())
//            {
//                var result = client.PostAsync(ProfilingUrl, new StringContent(JsonConvert.SerializeObject(request)))
//                    .Result;

//                Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode, "API returned incorrect status code");
//            }
//        }

//        [TestMethod, TestCategory("IntegrationTest")]
//        public void ProfilingController_ShouldReturnNotFoundErrorIfFspIsNonsense()
//        {
//            var request = new ProfileRequest(
//                allocationOrganisation: null,
//                fundingStreamPeriod: "This is not a real funding stream period",
//                allocationStartDate: new DateTime(2018, 8, 1),
//                allocationEndDate: new DateTime(2019, 7, 31),
//                allocationValueByDistributionPeriod: new[]
//                {
//                    new ProfileRequestPeriodValue("AY1819", 24000.00M),
//                });

//            using (var client = HttpHelpers.GetAuthorizedClient())
//            {
//                var result = client.PostAsync(ProfilingUrl, new StringContent(JsonConvert.SerializeObject(request)))
//                    .Result;

//                Assert.AreEqual(HttpStatusCode.NotFound, result.StatusCode, "API returned incorrect status code");
//            }
//        }

//        [TestMethod, TestCategory("IntegrationTest")]
//        public void ProfilingController_ShouldReturnErrorIfNoAllocationValueSpecified()
//        {
//            var request = new ProfileRequest(
//                allocationOrganisation: null,
//                fundingStreamPeriod: "PSG1819",
//                allocationStartDate: new DateTime(2018, 8, 1),
//                allocationEndDate: new DateTime(2019, 7, 31),
//                allocationValueByDistributionPeriod: new ProfileRequestPeriodValue[0]);

//            using (var client = HttpHelpers.GetAuthorizedClient())
//            {
//                var result = client.PostAsync(ProfilingUrl, new StringContent(JsonConvert.SerializeObject(request)))
//                    .Result;

//                Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode, "API returned incorrect status code");
//            }
//        }

//        [TestMethod, TestCategory("IntegrationTest")]
//        public void ProfilingController_ShouldReturnErrorIfAnyDistributionPeriodsDontExistInTheFsp()
//        {
//            var request = new ProfileRequest(
//                allocationOrganisation: null,
//                fundingStreamPeriod: "PSG1819",
//                allocationStartDate: new DateTime(2018, 8, 1),
//                allocationEndDate: new DateTime(2019, 7, 31),
//                allocationValueByDistributionPeriod: new[]
//                {
//                    new ProfileRequestPeriodValue("NOTREAL1819", 24000.00M),
//                });

//            using (var client = HttpHelpers.GetAuthorizedClient())
//            {
//                var result = client.PostAsync(ProfilingUrl, new StringContent(JsonConvert.SerializeObject(request)))
//                    .Result;

//                Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode, "API returned incorrect status code");
//            }
//        }
//    }
//}