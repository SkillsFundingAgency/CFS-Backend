using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Logging
{
    public interface ILoggingService
    {
        string CorrelationId { get; }

        void Trace(string message);

        void Exception(string message, Exception exception);

        void FatalException(string message, Exception exception);
    }
}
