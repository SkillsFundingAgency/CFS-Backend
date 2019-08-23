using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class ApiCalculationMetadataBuilder : TestEntityBuilder
    {
        private string _name;
        private PublishStatus? _publishStatus;

        
        
        public ApiCalculationMetadataBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public ApiCalculationMetadataBuilder WithPublishStatus(PublishStatus publishStatus)
        {
            _publishStatus = publishStatus;

            return this;
        }
        
        public CalculationMetadata Build()
        {
            return new CalculationMetadata
            {
                Name = _name ?? NewRandomString(),
                PublishStatus = _publishStatus.GetValueOrDefault(NewRandomEnum<PublishStatus>())
            };
        }
    }
}