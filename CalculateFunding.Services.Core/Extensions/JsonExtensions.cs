using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace CalculateFunding.Services.Core.Extensions
{
    public static class JsonExtensions
    {
        public static string Prettify(this string json)
        {
            return JValue.Parse(json).ToString(Formatting.Indented);
        }
        
        public static TPoco AsPoco<TPoco>(this string json, bool useCamelCase = true)
            where TPoco : class
        {
            return json.IsNullOrWhitespace() ? null : JsonConvert.DeserializeObject<TPoco>(json, NewJsonSerializerSettings(useCamelCase));
        }

        public static string AsJson<TPoco>(this TPoco poco, bool useCamelCase = true)
            where TPoco : class
        {
            return poco == null ? null : JsonConvert.SerializeObject(poco, NewJsonSerializerSettings(useCamelCase));
        }

        public static TPoco DeepCopy<TPoco>(this TPoco poco)
            where TPoco : class
        {
            return poco.AsJson().AsPoco<TPoco>();
        }

        private static JsonSerializerSettings NewJsonSerializerSettings(bool useCamelCase)
        {
            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();

            if (useCamelCase)
                jsonSerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            return jsonSerializerSettings;
        }
    }
}