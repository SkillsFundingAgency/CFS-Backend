using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using Microsoft.Azure.ServiceBus;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Services;

namespace CalculateFunding.Services.Core.Functions
{
    public abstract class SmokeTest
    {
        private const string SmokeTestKey = "smoketest";
        
        private readonly ILogger _logger;
        private readonly IMessengerService _messengerService;
        private readonly bool _useAzureStorage;
        private readonly IProcessingService _processingService;
        private readonly IUserProfileProvider _userProfileProvider;
        private readonly string _functionName;

        protected SmokeTest(ILogger logger, 
            IMessengerService messengerService, 
            string functionName, 
            bool useAzureStorage,
            IUserProfileProvider userProfileProvider,
            IProcessingService processingService)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));
            Guard.ArgumentNotNull(processingService, nameof(processingService));
            Guard.IsNullOrWhiteSpace(functionName, nameof(functionName));

            _logger = logger;
            _messengerService = messengerService;
            _useAzureStorage = useAzureStorage;
            _userProfileProvider = userProfileProvider;
            _processingService = processingService;
            _functionName = functionName;
        }

        private bool IsSmokeTest(Message message) => message.UserProperties.ContainsKey(SmokeTestKey);

        public string BuildNumber => FileVersionInfo.GetVersionInfo(GetType().Assembly.Location).FileVersion;

        protected virtual async Task Run(Message message, Func<Task> func = null)
        {
            _userProfileProvider.UserProfile = message.GetUserProfile();

            if (IsSmokeTest(message))
            {
                _logger.Information($"running smoke test for {_messengerService.ServiceName} listener {_functionName}");

                Dictionary<string, string> properties = new Dictionary<string, string> { { "listener", _functionName } };

                string invocationId = message.UserProperties[SmokeTestKey].ToString();
                
                if (_useAzureStorage)
                {
                    await _messengerService.SendToQueue(invocationId, 
                        BuildResponseFor(invocationId), 
                        properties);
                }
                else
                {
                    await _messengerService.SendToTopic(SmokeTestKey, 
                        BuildResponseFor(invocationId), 
                        properties);
                }
            }
            else
            {
                await _processingService.Run(message, func);
            }
        }

        private SmokeResponse BuildResponseFor(string invocationId)
        {
            return new SmokeResponse 
            { 
                Listener = _functionName, 
                InvocationId = invocationId, 
                Service = _messengerService.ServiceName,
                BuildNumber = BuildNumber
            };
        }
    }
}
