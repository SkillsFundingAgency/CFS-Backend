using CalculateFunding.Services.Results.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Results.UnitTests
{
    public class ProviderInformationBuilder : TestEntityBuilder
    {
        private string _id;
        
        public ProviderInformationBuilder WithId(string id)
        {
            _id = id;

            return this;
        }
        
        public ProviderInformation Build()
        {
            return new ProviderInformation
            {
                Id = _id ?? NewRandomString(),
            };
        }
    }
}