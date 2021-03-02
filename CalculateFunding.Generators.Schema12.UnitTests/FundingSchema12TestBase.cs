using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NJsonSchema;
using NJsonSchema.Validation;

namespace CalculateFunding.Generators.Schema12.UnitTests
{
    public abstract class FundingSchema12TestBase
    {
        private const string ResourceFileRoot = "CalculateFunding.Generators.Schema12.UnitTests.Resources";
        private static readonly Assembly Assembly = typeof(FundingSchema12TestBase).Assembly;

        protected JsonSchema FundingSchema;

        protected string GetEmbeddedFileContents(string name) => Assembly.GetEmbeddedResourceFileContents($"{ResourceFileRoot}.{name}");

        [TestInitialize]
        public async Task JsonSchema12TestBaseInitialise()
        {
            FundingSchema = await JsonSchema.FromJsonAsync(GetEmbeddedFileContents("funding-schema1.2.json"));
        }

        protected void ThenTheJsonValidatesAgainstThe1_1FundingSchema(string json)
        {
            ICollection<ValidationError> errors = FundingSchema.Validate(json);

            const string providerFundings = nameof(providerFundings);

            //has to conform to the schema apparently apart from for provider fundings (just take the id list whereas the schema wants an entity list)
            //and funding line codes where there will be null apart from for payment lines (the schema wants them all to have codes) 
            errors = errors.Where(_ => !_.ToString().Contains(providerFundings))
                .ToList();

            if (errors.Count == 0)
            {
                return;
            }

            string validationErrors = errors.Select(_ => _.ToString()).Join("\n");

            Assert.Fail($"Resulting json did not validate against the 1.2 funding schema;\n{validationErrors}");
        }
    }
}