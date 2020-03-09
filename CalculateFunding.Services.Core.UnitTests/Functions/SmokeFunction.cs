using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using Microsoft.Azure.ServiceBus;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Functions
{
    public class SmokeFunction : SmokeTest
    {
        public const string FunctionName = "smokefunction";
        private readonly Func<Task> _action;

        public SmokeFunction(ILogger logger,
            IMessengerService messengerService,
            bool isDevelopment,
            Func<Task> action = null) : base(logger,
                messengerService,
                FunctionName,
                isDevelopment)
        {
            _action = action;
        }

        public async Task Run(Message message)
        {
            await Run(_action,
            message);
        }
    }
}
