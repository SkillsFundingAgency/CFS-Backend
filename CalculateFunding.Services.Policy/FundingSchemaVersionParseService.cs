using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Policy.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Policy
{
    public class FundingSchemaVersionParseService : IFundingSchemaVersionParseService
    {
        JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings()
        {
            MaxDepth = 1024
        };

        public string GetInputTemplateSchemaVersion(string templateContents)
        {
            Guard.IsNullOrWhiteSpace(templateContents, nameof(templateContents));

            TemplateSchemaVersionParseResult result = JsonConvert.DeserializeObject<TemplateSchemaVersionParseResult>(templateContents, _jsonSerializerSettings);

            return result?.SchemaVersion;
        }
    }
}