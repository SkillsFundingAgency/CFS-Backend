using CalculateFunding.Common.Models;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.Core.ServiceBus;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Core.Functions
{
    [TestClass]
    public class SmokeTestUnitTests
    {
        private SmokeFunction _smokefunction;
        private ILogger _logger;
        private IMessengerService _messengerservice;

        [TestInitialize]
        public void Setup()
        {
            _logger = Substitute.For<ILogger>();
            _messengerservice = Substitute.For<IMessengerService>();
            _messengerservice
                .ServiceName
                .Returns("SmokeService");
        }

        [TestMethod]
        public async Task Run_WhenSmokeTestRunAgainstSmokeMessageInDevelopment_SmokeResponseSentToQueue()
        {
            GivenSmokeTestCreatedInDevelopment();

            string uniqueId = Guid.NewGuid().ToString();

            await WhenMessageReceivedBySmokeTest(uniqueId);

            Dictionary<string, string> properties = new Dictionary<string, string> { { "listener", SmokeFunction.FunctionName } };

            SmokeResponse smokeResponse = NewSmokeResponse(_ =>
            {
                _.WithInvocationId(uniqueId)
                .WithListener(SmokeFunction.FunctionName)
                .WithServiceName(_messengerservice.ServiceName);
            });

            await _messengerservice
                .Received(1)
                .SendToQueue(uniqueId, 
                    Arg.Is<SmokeResponse>(_ => _.InvocationId == smokeResponse.InvocationId), 
                    Arg.Is<Dictionary<string, string>>(_ => _["listener"] == properties["listener"]));
        }

        [TestMethod]
        public async Task Run_WhenSmokeTestRunAgainstSmokeMessageNotInDevelopment_SmokeResponseSentToTopic()
        {
            GivenSmokeTestCreatedNotInDevelopment();

            string uniqueId = Guid.NewGuid().ToString();

            await WhenMessageReceivedBySmokeTest(uniqueId);

            Dictionary<string, string> properties = new Dictionary<string, string> { { "listener", SmokeFunction.FunctionName } };

            SmokeResponse smokeResponse = NewSmokeResponse(_ =>
            {
                _.WithInvocationId(uniqueId)
                .WithListener(SmokeFunction.FunctionName)
                .WithServiceName(_messengerservice.ServiceName);
            });

            await _messengerservice
                .Received(1)
                .SendToTopic("smoketest",
                    Arg.Is<SmokeResponse>(_ => _.InvocationId == smokeResponse.InvocationId),
                    Arg.Is<Dictionary<string, string>>(_ => _["listener"] == properties["listener"]));
        }

        [TestMethod]
        public async Task Run_WhenSmokeTestRunAgainstNonSmokeTest_SmokeTestByPassed()
        {
            string test = null;

            GivenSmokeTestCreatedInDevelopment(async () => await Task.Run(() => { test = "ran"; }));

            await WhenNonSmokeMessageReceivedBySmokeTest();

            test.Equals("ran");
        }

        private async Task WhenNonSmokeMessageReceivedBySmokeTest()
        {
            Message message = new Message();

            await _smokefunction.Run(message);
        }

        private async Task WhenMessageReceivedBySmokeTest(string uniqueId)
        {
            Message message = new Message();

            message.UserProperties.Add("smoketest", uniqueId);

            await _smokefunction.Run(message);
        }

        private void GivenSmokeTestCreatedInDevelopment(Func<Task> action = null)
        {
            _smokefunction = new SmokeFunction(_logger, _messengerservice, true, action);
        }

        private void GivenSmokeTestCreatedNotInDevelopment()
        {
            _smokefunction = new SmokeFunction(_logger, _messengerservice, false);
        }

        private SmokeResponse NewSmokeResponse(Action<SmokeResponseBuilder> setUp = null)
        {
            SmokeResponseBuilder smokeResponseBuilder = new SmokeResponseBuilder();

            setUp?.Invoke(smokeResponseBuilder);

            return smokeResponseBuilder.Build();
        }
    }
}
