using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Errors
{
    public class ProfilePatternKeyBuilder : TestEntityBuilder
    {
        private string _fundingLineCode;
        private string _key;

        public ProfilePatternKeyBuilder WithFundingLineCode(string fundingLineCode)
        {
            _fundingLineCode = fundingLineCode;

            return this;
        }

        public ProfilePatternKeyBuilder WithKey(string key)
        {
            _key = key;

            return this;
        }
        
        public ProfilePatternKey Build()
        {
            return new ProfilePatternKey
            {
                FundingLineCode = _fundingLineCode ?? NewRandomString(),
                Key = _key ?? NewRandomString()
            };
        }     
    }
}