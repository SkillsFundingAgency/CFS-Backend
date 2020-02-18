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
        
        protected abstract Assembly FunctionAssembly { get; }
        protected abstract IServiceScope CreateServiceScope();

        protected IServiceCollection ServiceCollection { get; private set; }

        [TestMethod]
        public void CanResolveAllFunctionClasses()
        {
            IEnumerable<Type> EntryPoints = FunctionAssembly.GetTypes()
                .Where(_ => _.IsSubclassOf(typeof(TBase)) &&
                            !_.IsAbstract)
                .ToArray();

            foreach(Type function in EntryPoints)
            {
                ServiceCollection.AddScoped(function);
            }

            AddExtraRegistrations(ServiceCollection);
            
            using (IServiceScope scope = CreateServiceScope())
            {
                IServiceProvider serviceProvider = scope.ServiceProvider;
                
                foreach (Type entryPoint in EntryPoints)
                {
                    serviceProvider
                        .GetService(entryPoint)
                        .Should()
                        .NotBeNull(entryPoint.Name);
                }
            }
        }

        protected virtual void AddExtraRegistrations(IServiceCollection serviceCollection)
        {
        }
    }
}