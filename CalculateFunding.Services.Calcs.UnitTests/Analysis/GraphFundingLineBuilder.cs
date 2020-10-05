using CalculateFunding.Models.Graph;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Calcs.UnitTests.Analysis
{
    public class GraphFundingLineBuilder : TestEntityBuilder
    {
        private string _id;

        public GraphFundingLineBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public FundingLine Build()
        {
            return new FundingLine
            {
                FundingLineId = _id ?? NewRandomString(),
            };
        }
    }
}
