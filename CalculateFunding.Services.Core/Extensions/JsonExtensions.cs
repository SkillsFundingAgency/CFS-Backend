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


        public static bool AreEqual<TItem1, TItem2>(this TItem1 item1, TItem2 item2)
        {
            if (ReferenceEquals(item1, item2)) return true;
            if ((item1 == null) || (item2 == null)) return false;
            if (item1.GetType() != item2.GetType()) return false;

            var item1Json = JsonConvert.SerializeObject(item1);
            var item2Json = JsonConvert.SerializeObject(item2);

            return item1Json == item2Json;
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