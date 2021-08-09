using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace CalculateFunding.Models.Calcs.ObsoleteItems
{
    public enum DatasetFieldType
    {
        Boolean,
        Char,
        Byte,
        Integer,
        Float,
        Decimal,
        DateTime,
        String,
        NullableOfInteger,
        NullableOfDecimal,
        NullableOfBoolean
    }
}
