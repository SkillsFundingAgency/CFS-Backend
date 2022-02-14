using System;

namespace CalculateFunding.Services.Core.Services
{
    public interface ICurrentDateTime
    {
        DateTime GetUtcNow();
    }
}