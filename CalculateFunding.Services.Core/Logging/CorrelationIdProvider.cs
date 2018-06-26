using CalculateFunding.Services.Core.Interfaces.Logging;
using System;

namespace CalculateFunding.Services.Core.Logging
{
    public class CorrelationIdProvider : ICorrelationIdProvider
    {
        string _correlationId = "";

        public string GetCorrelationId()
        {
            if (string.IsNullOrWhiteSpace(_correlationId))
            {
                _correlationId = Guid.NewGuid().ToString();
            }
            return _correlationId;
        }

        public void SetCorrelationId(string correlationId)
        {
            if (string.IsNullOrWhiteSpace(_correlationId))
            {
                _correlationId = correlationId;
            }
        }

    }
}
