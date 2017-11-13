﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Allocations.Models.Specs
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TargetLanguage
    {
        CSharp,
        VisualBasic
    }
}