using CalculateFunding.Models.Providers;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Providers.UnitTests
{
    public class MasterProviderVersionBuilder : TestEntityBuilder
    {
        private string _id;

        public MasterProviderVersionBuilder WithId(string id)
        {
            _id = id;

            return this;
        }
        
        
        public MasterProviderVersion Build()
        {
            return new MasterProviderVersion
            {
                Id = _id ?? NewRandomString()
            };
        }
    }
}