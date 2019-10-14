using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Services.Core.AspNet.HealthChecks;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Core.AspNet.UnitTests.HealthChecks
{
    [TestClass]
    public class ControllerResolverHealthCheckTests
    {
        private IServiceProvider _services;
        private ControllerResolverHealthCheck _healthCheck;

        [TestInitialize]
        public void SetUp()
        {
            _services = Substitute.For<IServiceProvider>();
            _healthCheck = new ControllerResolverHealthCheck(_services);
        }

        [TestMethod]
        public async Task CatchesResolveExceptionsForControllerTypes()
        {
            Exception exception = new Exception(new RandomString());

            GivenResolvingTheTypeThrowsTheException(typeof(ControllerOne), exception);

            ServiceHealth serviceHealth = await _healthCheck.IsHealthOk();

            serviceHealth
                .Should()
                .NotBeNull();

            serviceHealth
                .Dependencies
                .Should()
                .ContainEquivalentOf(new DependencyHealth
                {
                    Message = $"{exception.Message}{Environment.NewLine}{exception.StackTrace}",
                    HealthOk = false,
                    DependencyName = nameof(ControllerOne)
                });

            serviceHealth
                .Dependencies
                .Should()
                .ContainEquivalentOf(new DependencyHealth
                {
                    HealthOk = true,
                    DependencyName = nameof(ControllerTwo)
                });
        }

        private void GivenResolvingTheTypeThrowsTheException(Type type, Exception exception)
        {
            _services.When(_ => _.GetService(type))
                .Throw(exception);
        }

        private class ControllerOne : Controller
        {
        }

        private class ControllerTwo : Controller
        {
        }
    }
}