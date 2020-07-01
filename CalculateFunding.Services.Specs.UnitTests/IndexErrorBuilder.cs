using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Specs.UnitTests
{
    public class IndexErrorBuilder : TestEntityBuilder
    {
        private string _message;
        private string _key;
        
        public IndexErrorBuilder WithKey(string key)
        {
            _key = key;

            return this;
        }

        public IndexErrorBuilder WithMessage(string message)
        {
            _message = message;

            return this;
        }
        
        public IndexError Build()
        {
            return new IndexError
            {
                Key = _key ?? NewRandomString(),
                ErrorMessage = _message ?? NewRandomString()
            };
        }
    }
}