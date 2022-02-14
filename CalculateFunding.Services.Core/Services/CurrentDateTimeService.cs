using System;

namespace CalculateFunding.Services.Core.Services
{
    public class CurrentDateTimeService : ICurrentDateTime
    {
        public DateTime GetUtcNow()
        {
            return DateTime.UtcNow;
        }
    }
}
