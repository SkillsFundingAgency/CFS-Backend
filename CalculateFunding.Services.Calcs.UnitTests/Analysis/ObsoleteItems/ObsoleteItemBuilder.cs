using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Calcs.ObsoleteItems;
using CalculateFunding.Tests.Common.Helpers;
using Polly;

namespace CalculateFunding.Services.Calcs.UnitTests.Analysis.ObsoleteItems
{
    public class ObsoleteItemBuilder : TestEntityBuilder
    {
        private string _id;
        private IEnumerable<string> _calculationIds;
        private string _codeReference;
        
        public ObsoleteItemBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public ObsoleteItemBuilder WithCalculationIds(params string[] calculationIds)
        {
            _calculationIds = calculationIds;

            return this;
        }

        public ObsoleteItemBuilder WithCodeReference(string sourceCodeReference)
        {
            _codeReference = sourceCodeReference;

            return this;
        }
        
        public ObsoleteItem Build()
        {
            return new ObsoleteItem
            {
                Id = _id ?? NewRandomString(),
                CalculationIds = _calculationIds?.ToList(),
                CodeReference = _codeReference ?? NewRandomString()
            };
        }
    }
}