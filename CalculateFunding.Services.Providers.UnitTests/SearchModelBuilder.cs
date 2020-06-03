using CalculateFunding.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Providers.UnitTests
{
    public class SearchModelBuilder : TestEntityBuilder
    {
        public SearchModel Build()
        {
            return new SearchModel();
        }
    }
}