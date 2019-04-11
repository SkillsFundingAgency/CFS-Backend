using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace CalculateFunding.Models.Datasets
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DefinitionChangeType
    {
        [EnumMember(Value = "DefinitionName")]
        DefinitionName
    }
}
