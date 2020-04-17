using CalculateFunding.Models.Graph;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Graph.UnitTests
{
    public class DatasetFieldBuilder : TestEntityBuilder
    {
        public DatasetField Build()
        {
            return new DatasetField
            {
                DatasetFieldId = NewRandomString(),
                DatasetFieldName = NewRandomString(),
                DatasetFieldIsAggregable = true
            };
        }
    }
}
