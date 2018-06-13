using System;
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
    }
}
