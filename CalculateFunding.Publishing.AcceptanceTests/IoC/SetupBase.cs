using BoDi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Publishing.AcceptanceTests.IoC
{
    public abstract class SetupBase
    {
        protected readonly IObjectContainer _objectContainer;

        public SetupBase(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;
        }
        protected void RegisterTypeAs<TType, TInterface>()
             where TType : class, TInterface
             where TInterface : class
        {
            _objectContainer.RegisterTypeAs<TType, TInterface>();
        }

        protected void RegisterInstanceAs<TType>(TType instance)
            where TType : class
        {
            _objectContainer.RegisterInstanceAs(instance);
        }

        protected TType ResolveInstance<TType>()
            where TType : class
        {
            return _objectContainer.Resolve<TType>();
        }

        protected class ConfigurationStub : IConfiguration
        {
            public IConfigurationSection GetSection(string key) => null;

            public IEnumerable<IConfigurationSection> GetChildren() => ArraySegment<IConfigurationSection>.Empty;

            public IChangeToken GetReloadToken() => null;

            public string this[string key]
            {
                get => null;
                set { }
            }
        }
    }
}
