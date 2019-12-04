using CalculateFunding.Common.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Calculator
{
    public class ProviderSourceDatasetBuilder : TestEntityBuilder
    {
        public ProviderSourceDataset Build()
        {
            return new ProviderSourceDataset
            {
                SpecificationId = NewRandomString(),
                DataRelationship = new Reference
                {
                    Id = NewRandomString()
                },
                ProviderId = NewRandomString()
            };
        }
    }
}