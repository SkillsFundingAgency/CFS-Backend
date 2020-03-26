using System;
using CalculateFunding.Services.Core.Interfaces.Helpers;

namespace CalculateFunding.Services.Core.Helpers
{
    public class DateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}