using CalculateFunding.Services.FundingDataZone.SqlModels;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.FundingDataZone.UnitTests
{
    public class PublishingAreaOrganisationBuilder : TestEntityBuilder
    {
        public PublishingAreaOrganisation Build()
        {
            return new PublishingAreaOrganisation
            {
                Name = NewRandomString()
            };
        }
    }
}