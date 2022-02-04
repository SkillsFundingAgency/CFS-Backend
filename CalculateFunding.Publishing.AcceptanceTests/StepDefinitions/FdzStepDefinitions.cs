using CalculateFunding.Common.ApiClient.FundingDataZone;
using CalculateFunding.Common.ApiClient.FundingDataZone.Models;
using CalculateFunding.Publishing.AcceptanceTests.Repositories;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace CalculateFunding.Publishing.AcceptanceTests.StepDefinitions
{
    public class FdzStepDefinitions : StepDefinitionBase
    {
        private readonly IFundingDataZoneApiClient _fundingDataZoneApiClient;
        private readonly FDZInMemoryClient _emulatedClient;

        public FdzStepDefinitions(IFundingDataZoneApiClient fundingDataZoneApiClient)
        {
            _fundingDataZoneApiClient = fundingDataZoneApiClient;
            _emulatedClient = (FDZInMemoryClient)fundingDataZoneApiClient;
        }

        [Given(@"the payment organisations are available for provider snapshot '([^']*)' from FDZ")]
        public void GivenThePaymentOrganisationsAreAvailableForProviderSnapshotFromFDZ(int providerSnapshotId)
        {
            string contents = this.GetTestDataContents($"ReleaseManagementData.FDZ.{providerSnapshotId}-paymentorganisations.json");
            IEnumerable<PaymentOrganisation> paymentOrganisations = JsonConvert.DeserializeObject<IEnumerable<PaymentOrganisation>>(contents);

            _emulatedClient.SetPaymentOrganisationsForProviderSnapshot(providerSnapshotId, paymentOrganisations);
        }
    }
}
