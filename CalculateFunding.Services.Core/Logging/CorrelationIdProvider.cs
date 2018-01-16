using CalculateFunding.Services.Core.Interfaces.Logging;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Services.Core.Logging
{
    public class CorrelationIdProvider : ICorrelationIdProvider
    {
        string _correlationId = "";

        public string GetCorrelationId()
        {
            if (string.IsNullOrWhiteSpace(_correlationId))
            {
                return Guid.NewGuid().ToString();
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
