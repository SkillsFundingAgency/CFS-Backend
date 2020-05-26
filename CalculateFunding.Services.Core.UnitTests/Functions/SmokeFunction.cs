using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
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
            IUserProfileProvider userProfileProvider,
            Func<Task> action = null) : base(logger,
                messengerService,
                FunctionName,               
                isDevelopment,
                userProfileProvider)
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
