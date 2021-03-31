using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Tests.Common.Builders
{
    public class IndexErrorBuilder : TestEntityBuilder
    {
        private string _key;
        private string _errorMessage;

        public IndexErrorBuilder WithKey(string key)
        {
            _key = key;

            return this;
        }

        public IndexErrorBuilder WithErrorMessage(string errorMessage)
        {
            _errorMessage = errorMessage;

            return this;
        }
        
        public IndexError Build()
        {
            return new IndexError
            {
                Key = _key ?? NewRandomString(),
                ErrorMessage = _errorMessage ?? NewRandomString()
            };
        }
    }
}