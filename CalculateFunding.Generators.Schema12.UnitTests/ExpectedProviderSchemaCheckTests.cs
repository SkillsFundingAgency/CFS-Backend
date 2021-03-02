using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema;
using NJsonSchema.Validation;

namespace CalculateFunding.Generators.Schema12.UnitTests
{
    [TestClass]
    public class ExpectedProviderSchemaCheckTests
    {
        private const string ResourceFileRoot = "CalculateFunding.Generators.Schema12.UnitTests.Resources";

        [DataTestMethod]
        [DataRow("provider_example_1.json")]
        public async Task EnsureExampleFundingJsonValidatesAgainstSchema(string resourceFilename)
        {
            JsonSchema schema = await JsonSchema.FromJsonAsync(GetEmbeddedFileContents("provider-schema-1.2.json"));
            ICollection<ValidationError> errors = schema.Validate(GetEmbeddedFileContents(resourceFilename));
            errors?.Count.Should().Be(0);
        }

        private static readonly Assembly Assembly = typeof(ExpectedProviderSchemaCheckTests).Assembly;
        private string GetEmbeddedFileContents(string name) => Assembly.GetEmbeddedResourceFileContents($"{ResourceFileRoot}.{name}");
    }
}
