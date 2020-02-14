﻿using System;
using System.Collections.Generic;
using System.Text;

namespace System
{
    public static class DateTimeOffsetExtensions
    {
        public static DateTimeOffset? ToNullableLocal(this DateTimeOffset? theDate)
        {
            if (!theDate.HasValue)
                return null;

            return new DateTimeOffset(theDate.Value.LocalDateTime);
        }

        public static string ToCosmosString(this DateTimeOffset theDate)
        {
            return theDate.ToString("yyyy-MM-ddThh:mm:ss.sssZ");
        }
    }
}
