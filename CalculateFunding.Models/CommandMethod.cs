using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CommandMethod
    {
        Post,
        Put,
        Delete
    }
}