﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Versioning
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PublishStatus
    {
        Draft,
        Approved,
        Updated,
        Archived
    }
}