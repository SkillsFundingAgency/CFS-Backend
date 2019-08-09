using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Specs.UnitTests
{
    public class ReferenceDataBuilder : TestEntityBuilder
    {
        public ReferenceData Build()
        {
            return new ReferenceData();
        }
    }
}