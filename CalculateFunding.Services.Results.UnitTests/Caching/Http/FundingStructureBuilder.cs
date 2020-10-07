using System;
using CalculateFunding.Models.Result;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Results.UnitTests.Caching.Http
{
    public class FundingStructureBuilder : TestEntityBuilder
    {
        private DateTimeOffset? _lastModified;

        public FundingStructureBuilder WithLastModified(DateTimeOffset lastModified)
        {
            _lastModified = lastModified;

            return this;
        }
        
        public FundingStructure Build()
        {
            return new FundingStructure
            {
                LastModified = _lastModified.GetValueOrDefault(NewRandomDateTime())
            };
        }
    }
}