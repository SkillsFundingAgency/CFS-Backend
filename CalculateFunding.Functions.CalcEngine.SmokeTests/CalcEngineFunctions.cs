using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Functions.CalcEngine.ServiceBus;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Functions;
using CalculateFunding.Tests.Common;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.CalcEngine.SmokeTests
{
    [TestClass]
    public class CalcEngineFunctions : SmokeTestBase
    {
        private static ICalculationEngineService _calcEngineService;
        private static ILogger _logger;
        private static IUserProfileProvider _userProfileProvider;

        [ClassInitialize]
        public static void SetupTests(TestContext tc)
        {
            SetupTests("calcengine");

            _logger = CreateLogger();

            _calcEngineService = CreateCalcEngineService();

            _userProfileProvider = CreateUserProfileProvider();
        }

        [TestMethod]
        public async Task OnCalcsGenerateAllocationResults_SmokeTestSucceeds()
        {
            OnCalcsGenerateAllocationResults onCalcsGenerateAllocationResults = new OnCalcsGenerateAllocationResults(_logger,
                _calcEngineService,
                Services.BuildServiceProvider().GetRequiredService<IMessengerService>(),
                _userProfileProvider,
                IsDevelopment);

            SmokeResponse response = await RunSmokeTest(ServiceBusConstants.QueueNames.CalcEngineGenerateAllocationResults, 
                (Message smokeResponse) => onCalcsGenerateAllocationResults.Run(smokeResponse));

            response
                .Should()
                .NotBeNull();
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static ICalculationEngineService CreateCalcEngineService()
        {
            return Substitute.For<ICalculationEngineService>();
        }

        private static IUserProfileProvider CreateUserProfileProvider()
        {
            return Substitute.For<IUserProfileProvider>();
        }
    }
}
