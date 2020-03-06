using CalculateFunding.Models.Graph;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Graph.UnitTests
{
    public class DataFieldBuilder : TestEntityBuilder
    {
        public DataField Build()
        {
            return new DataField
            {
                DataFieldId = NewRandomString(),
                FieldName = NewRandomString(),
                Name = NewRandomString()
            };
        }
    }
}