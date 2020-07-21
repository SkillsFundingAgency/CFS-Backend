using CalculateFunding.Services.FundingDataZone.SqlModels;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.FundingDataZone.UnitTests
{
    public class PublishingAreaProviderBuilder : TestEntityBuilder
    {
        public PublishingAreaProvider Build()
        {
            return new PublishingAreaProvider();
        } 
    }
}