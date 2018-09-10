using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Globalization;

namespace CalculateFunding.Models.Converters
{
    public class DateTimeOffsetConverter : DateTimeConverterBase
    {
       
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(((DateTimeOffset)value).ToString("dd/MM/yyyy"));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null || string.IsNullOrWhiteSpace(reader.Value.ToString()))
            {
                return null;
            }

            var date = reader.Value.ToString();

            DateTimeOffset result;

            if (DateTimeOffset.TryParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                return result;
            }
            else if (DateTimeOffset.TryParseExact(date, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                return result;
            }

            return null;
        }
    }
}
