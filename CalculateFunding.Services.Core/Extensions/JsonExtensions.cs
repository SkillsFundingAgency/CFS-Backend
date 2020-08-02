using System;
using System.IO;
using System.Text;
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
        
        public static TPoco AsPoco<TPoco>(this Stream jsonStream, bool useCamelCase = true)
            where TPoco : class
        {
            using (BinaryReader reader = new BinaryReader(jsonStream))
            {
                return Encoding.UTF8.GetString(reader.ReadBytes((int) jsonStream.Length))
                    .AsPoco<TPoco>();
            }
        }

        public static byte[] AsJsonBytes<TPoco>(this TPoco poco) 
            where TPoco : class
        {
            return Encoding.UTF8.GetBytes(poco.AsJson());
        }

        public static TPoco AsPocoWithSettings<TPoco>(this string json,
            JsonSerializerSettings settings)
            where TPoco : class
        {
            return json.IsNullOrWhitespace() ? null : JsonConvert.DeserializeObject<TPoco>(json, settings);    
        }

        public static TPoco AsPoco<TPoco>(this string json, bool useCamelCase = true)
            where TPoco : class
        {
            return json.AsPocoWithSettings<TPoco>(NewJsonSerializerSettings(useCamelCase));
        }

        public static string AsJson<TPoco>(this TPoco poco, bool useCamelCase = true)
            where TPoco : class
        {
            return poco == null ? null : JsonConvert.SerializeObject(poco, NewJsonSerializerSettings(useCamelCase));
        }

        public static TPoco DeepCopy<TPoco>(this TPoco poco, bool useCamelCase = true)
            where TPoco : class
        {
            return poco.AsJson(useCamelCase).AsPoco<TPoco>();
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