using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Core.Functions
{
    [TestClass]
    public class SmokeTestUnitTests
    {
        private SmokeFunction _smokeFunction;
        private ILogger _logger;
        private IMessengerService _messengerService;
        private string _expectedFileVersion;
        private IUserProfileProvider _userProfileProvider;

        [TestInitialize]
        public void Setup()
        {
            _logger = Substitute.For<ILogger>();
            _messengerService = Substitute.For<IMessengerService>();
            _messengerService
                .ServiceName
                .Returns("SmokeService");

            _userProfileProvider = Substitute.For<IUserProfileProvider>();
        }

        [TestMethod]
        public async Task Run_WhenSmokeTestRunAgainstSmokeMessageInDevelopment_SmokeResponseSentToQueue()
        {
            GivenSmokeTestCreatedInDevelopment();

            string expectedInvocationId = NewRandomString();

            await WhenMessageReceivedBySmokeTest(expectedInvocationId);

            await _messengerService
                .Received(1)
                .SendToQueue(expectedInvocationId,
                    Arg.Is<SmokeResponse>(_ => _.InvocationId == expectedInvocationId &&
                                               _.BuildNumber == _expectedFileVersion),
                    Arg.Is<Dictionary<string, string>>(_ => 
                        _["listener"] == SmokeFunction.FunctionName));
        }

        [TestMethod]
        public async Task Run_WhenSmokeTestRunAgainstSmokeMessageNotInDevelopment_SmokeResponseSentToTopic()
        {
            GivenSmokeTestCreatedNotInDevelopment();

            string expectedInvocationId = Guid.NewGuid().ToString();

            await WhenMessageReceivedBySmokeTest(expectedInvocationId);

            await _messengerService
                .Received(1)
                .SendToTopic("smoketest",
                    Arg.Is<SmokeResponse>(_ => _.InvocationId == expectedInvocationId &&
                                               _.BuildNumber == _expectedFileVersion),
                    Arg.Is<Dictionary<string, string>>(_ => 
                        _["listener"] == SmokeFunction.FunctionName));
        }

        [TestMethod]
        public async Task Run_WhenSmokeTestRunAgainstNonSmokeTest_SmokeTestByPassed()
        {
            string test = null;
            string expectedTest = "ran";

            GivenSmokeTestCreatedInDevelopment(async () => await Task.Run(() => { test = expectedTest; }));

            await WhenNonSmokeMessageReceivedBySmokeTest();

            test
                .Should()
                .BeSameAs(expectedTest);
        }

        private async Task WhenNonSmokeMessageReceivedBySmokeTest()
        {
            Message message = new Message();

            await _smokeFunction.Run(message);
        }

        private async Task WhenMessageReceivedBySmokeTest(string uniqueId)
        {
            Message message = new Message();

            message.UserProperties.Add("smoketest", uniqueId);

            await _smokeFunction.Run(message);
        }

        private void GivenSmokeTestCreatedInDevelopment(Func<Task> action = null)
        {
            _smokeFunction = new SmokeFunction(_logger, _messengerService, true, _userProfileProvider,action);
            _expectedFileVersion = _smokeFunction.BuildNumber;
        }

        private void GivenSmokeTestCreatedNotInDevelopment()
        {
            _smokeFunction = new SmokeFunction(_logger, _messengerService, false, _userProfileProvider);
            _expectedFileVersion = _smokeFunction.BuildNumber;
        }

        private string NewRandomString()
        {
            return new RandomString();
        }
    }
}