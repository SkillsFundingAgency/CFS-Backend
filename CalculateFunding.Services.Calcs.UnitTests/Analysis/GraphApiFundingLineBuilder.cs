using CalculateFunding.Common.ApiClient.Graph.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Calcs.UnitTests.Analysis
{
    public class GraphApiFundingLineBuilder : TestEntityBuilder
    {
        private string _id;

        public GraphApiFundingLineBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public FundingLine Build()
        {
            return new FundingLine
            {
                FundingLineId = _id ?? NewRandomString(),
                FundingLineName = NewRandomString()
            };
        }        
    }
}