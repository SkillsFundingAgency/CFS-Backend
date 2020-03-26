using System;

namespace CalculateFunding.Services.Core.Interfaces.Helpers
{
    public interface IDateTimeProvider
    {
        DateTimeOffset UtcNow { get; }
    }
}