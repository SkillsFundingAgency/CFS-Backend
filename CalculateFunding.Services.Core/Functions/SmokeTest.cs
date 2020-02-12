using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using Microsoft.Azure.ServiceBus;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Functions
{
    public abstract class SmokeTest
    {
        private ILogger _logger;
        private IMessengerService _messengerService;
        private const string _smoketest = "smoketest";
        private bool _useAzureStorage;
        private string _functionName;

        public SmokeTest(ILogger logger, 
            IMessengerService messengerService, 
            string functionName, 
            bool useAzureStorage)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));
            Guard.IsNullOrWhiteSpace(functionName, nameof(functionName));

            _logger = logger;
            _messengerService = messengerService;
            _useAzureStorage = useAzureStorage;
            _functionName = functionName;
        }

        private bool IsSmokeTest(Message message) => message.UserProperties.ContainsKey("smoketest");

        protected async Task Run(Func<Task> function, Message message)
        {
            if (IsSmokeTest(message))
            {
                _logger.Information($"running smoke test for {_messengerService.ServiceName} listener {_functionName}");

                Dictionary<string, string> properties = new Dictionary<string, string> { { "listener", _functionName } };

                if (_useAzureStorage)
                {
                    await _messengerService.SendToQueue(message.UserProperties[_smoketest].ToString(), 
                        new SmokeResponse { Listener = _functionName, 
                            InvocationId = message.UserProperties[_smoketest].ToString(), 
                            Service = _messengerService.ServiceName }, 
                        properties);
                }
                else
                {
                    await _messengerService.SendToTopic(_smoketest, 
                        new SmokeResponse { Listener = _functionName, 
                            InvocationId = message.UserProperties[_smoketest].ToString(), 
                            Service = _messengerService.ServiceName }, 
                        properties);
                }
            }
            else
            {
                await function();
            }
        }
    }
}
