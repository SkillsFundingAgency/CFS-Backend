using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Examples;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CalculateFunding.Api.External.Swagger.OperationFilters
{
    public class OperationFilter<T> : IOperationFilter
    {
        public void Apply(Operation operation, OperationFilterContext context)
        {
            SetResponseModelExamples(operation, context.ApiDescription);
        }

        private static void SetResponseModelExamples(Operation operation, ApiDescription apiDescription)
        {
            var responseAttributes = apiDescription.ActionAttributes().OfType<SwaggerResponseExampleAttribute>();

            foreach (var attr in responseAttributes)
            {
                var statusCode = ((int)attr.StatusCode).ToString();

                var response = operation.Responses.FirstOrDefault(r => r.Key == statusCode);

                if (response.Equals(default(KeyValuePair<string, Response>)) == false)
                {
                    if (response.Value != null)
                    {
                        IExamplesProvider provider = (IExamplesProvider)Activator.CreateInstance(attr.ExamplesProviderType);

                        var serializerSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver(), Formatting = Formatting.Indented, NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore };
                        response.Value.Examples = FormatJson(provider, serializerSettings, true);
                    }
                }
            }
        }

        private static object FormatJson(IExamplesProvider provider, JsonSerializerSettings serializerSettings, bool includeMediaType)
        {
            var stringwriter = new System.IO.StringWriter();
            var serializer = new XmlSerializer(typeof(T));
            serializer.Serialize(stringwriter, provider.GetExamples());

            object examples;
            if (includeMediaType)
            {
                if (typeof(T).Name.ToLowerInvariant().Contains("atom"))
                {
                    examples = new Dictionary<string, object>
                    {
                        {
                            "application/atom+json", provider.GetExamples()
                        }
                    };
                }
                else
                {
                    examples = new Dictionary<string, object>
                    {
                        {
                            "application/json", provider.GetExamples()
                        }
                    };
                }

            }
            else
            {
                examples = provider.GetExamples();
            }



            var jsonString = JsonConvert.SerializeObject(examples, serializerSettings);
            var result = JsonConvert.DeserializeObject(jsonString);
            return result;
        }

    }
}
