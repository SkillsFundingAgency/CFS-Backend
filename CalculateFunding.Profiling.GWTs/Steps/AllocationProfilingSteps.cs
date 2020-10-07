namespace CalculateFunding.Profiling.GWTs.Steps
{
	using System.Text;
	using Helpers;
	using System;
	using System.Linq;
	using System.Net;
	using System.Net.Http;
	using Dtos;
	using Utilities;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Newtonsoft.Json;
	using TechTalk.SpecFlow;

	[Binding]
    public class HappyPathSteps
    {
        private ProfileRequest _request = null;
        private ProfileResponse _response = null;
        private HttpStatusCode? _responseStatusCode = null;

        [Given(@"an ADP request exists for OrgId '(.*)' IdentifierName '(.*)' Identifier '(.*)' AllocationStartDate '(.*)' AllocationEndDate '(.*)' and FSP '(.*)' as follows")]
        public void GivenAnADPRequestExistsForOrgIdIdentifierNameIdentifierAllocationStartDateAllocationEndDateAndFSPAsFollows(string orgId, string identifierName, string identifier, string allocStart, string allocEnd, string fspCode, Table table)
        {
            _request = new ProfileRequest
            {
                AllocationOrganisation = new AllocationOrganisation
                {
                    OrganisationIdentifier = orgId.ParseStringIfPresent(),
                    AlternateOrganisation = new AlternateOrganisation
                    {
                        Identifier = identifier.ParseStringIfPresent(),
                        IdentifierName = identifierName.ParseStringIfPresent()
                    }
                },
                FundingStreamPeriod = fspCode.ParseStringIfPresent(),
                AllocationStartDate = allocStart.ParseDateTimeIfPresent(),
                AllocationEndDate = allocEnd.ParseDateTimeIfPresent(),
                AllocationValueByDistributionPeriod = table.Rows.Select(r =>
                    new AllocationValueForDistributionPeriod
                    {
                        AllocationValue = r.ParseDecimal("AllocationValue"),
                        DistributionPeriod = r["DistributionPeriod"]
                    }).ToArray()
            };
        }
        
        [When(@"the request is processed")]
        public void WhenTheRequestIsProcessed()
        {
	        using (HttpClient client = HttpClientHelper.GetAuthorizedClient(ConfigHolder.GetWebConfigDto().ApsUrl))
            {
                HttpResponseMessage response = client.PostAsync("api/profiling", new StringContent(JsonConvert.SerializeObject(_request), Encoding.UTF8, "application/json")).Result;

                _responseStatusCode = response.StatusCode;

                if (response.IsSuccessStatusCode)
                {
                    _response = JsonConvert.DeserializeObject<ProfileResponse>(
                        response.Content.ReadAsStringAsync().Result);
                }
            }
        }
        
        [Then(@"an ADP Allocation Profile response is created for OrgId '(.*)' IdentifierName '(.*)' Identifier '(.*)' AllocationStartDate '(.*)' AllocationEndDate '(.*)' and FSP '(.*)' as follows")]
        public void ThenAnADPAllocationProfileResponseIsCreatedForOrgIdIdentifierNameIdentifierAndFSPWhichContainsTheFollowing(string orgId, string identifierName, string identifier, string allocStart, string allocEnd, string fspCode, Table table)
        {
            Assert.IsNotNull(_response, "Did not get a successful response from API");

            ProfileRequest expectedRequest = new ProfileRequest
            {
                AllocationOrganisation = new AllocationOrganisation
                {
                    OrganisationIdentifier = orgId.ParseStringIfPresent(),
                    AlternateOrganisation = new AlternateOrganisation
                    {
                        IdentifierName = identifierName.ParseStringIfPresent(),
                        Identifier = identifier.ParseStringIfPresent()
                    }
                },
                AllocationStartDate = allocStart.ParseDateTimeIfPresent(),
                AllocationEndDate = allocEnd.ParseDateTimeIfPresent(),
                FundingStreamPeriod = fspCode,
                AllocationValueByDistributionPeriod = table.Rows.Select(r =>
                    new AllocationValueForDistributionPeriod
                    {
                        AllocationValue = r.ParseDecimal("AllocationValue"),
                        DistributionPeriod = r["DistributionPeriod"]
                    }).ToArray()
            };

            Assert.AreEqual(expectedRequest.AllocationStartDate, _response.AllocationProfileRequest.AllocationStartDate, $"StartDate did not match, response profile request was {_response.AllocationProfileRequest}");
            Assert.AreEqual(expectedRequest.AllocationEndDate, _response.AllocationProfileRequest.AllocationEndDate, $"EndDate did not match, response profile request was {_response.AllocationProfileRequest}");
            Assert.AreEqual(expectedRequest.FundingStreamPeriod, _response.AllocationProfileRequest.FundingStreamPeriod, $"FundingStreamPeriod did not match, response profile request was {_response.AllocationProfileRequest}");
            Assert.AreEqual(expectedRequest.AllocationOrganisation.OrganisationIdentifier, _response.AllocationProfileRequest.AllocationOrganisation.OrganisationIdentifier, $"OrganisationIdentifier did not match, response profile request was {_response.AllocationProfileRequest}");
            Assert.AreEqual(expectedRequest.AllocationOrganisation.AlternateOrganisation.IdentifierName, _response.AllocationProfileRequest.AllocationOrganisation.AlternateOrganisation.IdentifierName, $"AlternateIdentifierName did not match, response profile request was {_response.AllocationProfileRequest}");
            Assert.AreEqual(expectedRequest.AllocationOrganisation.AlternateOrganisation.Identifier, _response.AllocationProfileRequest.AllocationOrganisation.AlternateOrganisation.Identifier, $"Alternate Identifier did not match, response profile request was {_response.AllocationProfileRequest}");

	        foreach (var avbdp in expectedRequest.AllocationValueByDistributionPeriod)
	        {
		        Assert.IsTrue(_response.AllocationProfileRequest.AllocationValueByDistributionPeriod.Any(
				        resp => resp.AllocationValue == avbdp.AllocationValue
				                && resp.DistributionPeriod == avbdp.DistributionPeriod),
			        $"Could not find the following AllocValue in response {avbdp}{Environment.NewLine}Response Profile Request was {_response.AllocationProfileRequest}");

	        };
        }

        [Then(@"an ADP Delivery Profile response is created which contains the following")]
        public void ThenAnADPDeliveryProfileResponseIsCreatedWhichContainsTheFollowing(Table table)
        {
            Assert.IsNotNull(_response, "Did not get a successful response from API");

            DeliveryProfilePeriod[] expected = table.Rows
                .Select(r =>
                    new DeliveryProfilePeriod(
                        period: r["Period"],
                        occurrence: r.ParseInt("Occurrence"),
                        periodType: r["PeriodType"],
                        periodYear: r.ParseInt("PeriodYear"),
                        profileValue: r.ParseDecimal("ProfileValue"),
                        distributionPeriod: r["DistributionPeriod"])).ToArray();

            CollectionAssert.AreEquivalent(expected, _response.DeliveryProfilePeriods);
        }

        [Then(@"the service returns HTTP status code '(.*)'")]
        public void ThenTheReturnCodeIs(int expectedStatusCode)
        {
            Assert.IsNotNull(_responseStatusCode, "The API has not yet been called");

            Assert.AreEqual(expectedStatusCode, (int) _responseStatusCode,
                $"Test expected {expectedStatusCode} {(HttpStatusCode) expectedStatusCode} but instead got {(int) _responseStatusCode} {_responseStatusCode}");
        }
    }
}
