using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CalculateFunding.Services.Core.Functions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Tests.Common
{
    [TestClass]
    public abstract class FunctionIoCUnitTestBase : IoCUnitTestBase
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
            IEnumerable<Type> Functions = FunctionAssembly.GetTypes()
                .Where(_ => _.IsSubclassOf(typeof(SmokeTest)) &&
                            !_.IsAbstract)
                .ToArray();

            foreach(Type function in Functions)
            {
                ServiceCollection.AddScoped(function);
            }
            
            using (IServiceScope scope = CreateServiceScope())
            {
                IServiceProvider serviceProvider = scope.ServiceProvider;
                
                foreach (Type function in Functions)
                {
                    serviceProvider
                        .GetService(function)
                        .Should()
                        .NotBeNull(function.Name);
                }
            }
        }
    }
}