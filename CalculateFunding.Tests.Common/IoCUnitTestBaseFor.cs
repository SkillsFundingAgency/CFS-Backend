using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Tests.Common
{
    public abstract class IoCUnitTestBaseFor<TBase> : IoCUnitTestBase
    {
        [TestInitialize]
        public void FunctionIoCUnitTestBaseSetUp()
        {
            ServiceCollection = new ServiceCollection();
        }
        
        protected abstract Assembly EntryAssembly { get; }

        protected virtual IServiceScope CreateServiceScope() => ServiceCollection
            .BuildServiceProvider()
            .CreateScope();

        protected IServiceCollection ServiceCollection { get; private set; }

        [TestMethod]
        public void CanResolveAllSystemEntryPoints()
        {
            IEnumerable<Type> EntryPoints = EntryAssembly.GetTypes()
                .Where(_ => typeof(TBase).IsAssignableFrom(_) &&
                            !_.IsAbstract)
                .ToArray();

            RegisterEntryPoints(EntryPoints);
            RegisterDependencies();
            AddExtraRegistrations();

            using IServiceScope scope = CreateServiceScope();

            IServiceProvider serviceProvider = scope.ServiceProvider;
                
            foreach (Type entryPoint in EntryPoints)
            {
                serviceProvider
                    .GetService(entryPoint)
                    .Should()
                    .NotBeNull(entryPoint.Name);
            }
        }

        private void RegisterEntryPoints(IEnumerable<Type> EntryPoints)
        {
            foreach (Type function in EntryPoints)
            {
                ServiceCollection.AddScoped(function);
            }
        }

        protected abstract void RegisterDependencies();

        protected virtual void AddExtraRegistrations()
        {
        }
    }
}