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
            return theDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.sssK");
        }
    }
}
