using Microsoft.Azure.ServiceBus;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.CalcEngine.Interfaces
{
    public interface ICalculationEngineServiceValidator
    {
        void ValidateMessage(ILogger logger, Message message);
    }
}
