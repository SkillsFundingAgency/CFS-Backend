using CalculateFunding.Models.FundingDataZone;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.FundingDataZone.UnitTests
{
    public class PaymentOrganisationBuilder : TestEntityBuilder
    {
        public PaymentOrganisation Build()
        {
            return new PaymentOrganisation
            {
                Name = NewRandomString()
            };
        }
    }
}