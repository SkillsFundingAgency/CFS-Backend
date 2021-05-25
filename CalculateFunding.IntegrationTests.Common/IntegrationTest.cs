using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models;
using CalculateFunding.IntegrationTests.Common.Configuration;
using CalculateFunding.IntegrationTests.Common.IoC;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Tests.Common.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog.Core;

namespace CalculateFunding.IntegrationTests.Common
{
    public abstract class IntegrationTest
    {
        protected static ServiceLocator ServiceLocator;
        protected static IConfiguration Configuration;
        
        protected List<IDisposable> TrackedForTearDown;

        protected static void SetUpConfiguration()
        {
            Configuration = ConfigurationFactory.CreateConfiguration();
        }

        [TestInitialize]
        public void IntegrationTestSetUp()
        {
            TrackedForTearDown = new List<IDisposable>();
        }

        [TestCleanup]
        public void IntegrationTestTearDown()
        {
            Task[] teardownTasks = TrackedForTearDown.Select(SafeDispose)
                .ToArray();

            TaskHelper.WhenAllAndThrow(teardownTasks)
                .Wait();
        }

        private Task SafeDispose(IDisposable disposable)
        {
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    disposable.Dispose();
                }
                catch
                {
                    // ignored
                }
            });
        }

        protected void TrackForTeardown(params IDisposable[] disposableComponents)
            => TrackedForTearDown.AddRange(disposableComponents);

        protected static void SetUpServices(params Action<IServiceCollection, IConfiguration>[] setUps)
            => ServiceLocator = ServiceLocator.Create(Configuration, setUps);

        protected TService GetService<TService>()
            where TService : class
            => ServiceLocator.GetService<TService>();

        protected static void AddCacheProvider(IServiceCollection serviceCollection,
            IConfiguration configuration)
        {
            RedisSettings redisSettings = new RedisSettings();

            Configuration.Bind("redisSettings", redisSettings);

            serviceCollection.AddSingleton(redisSettings);
            serviceCollection.AddSingleton<ICacheProvider, StackExchangeRedisClientCacheProvider>();
        }

        protected static void AddNullLogger(IServiceCollection serviceCollection,
            IConfiguration configuration)
            => serviceCollection.AddSingleton(Logger.None);

        protected static void AddUserProvider(IServiceCollection serviceCollection,
            IConfiguration configuration) =>
            serviceCollection.AddSingleton<IUserProfileProvider, UserProfileProvider>();

        protected IEnumerable<TItem> AsEnumerable<TItem>(params TItem[] items) => items;

        protected static string NewRandomString() => new RandomString();

        protected static int NewRandomInteger() => new RandomNumberBetween(1, int.MaxValue - 1);
    }
}