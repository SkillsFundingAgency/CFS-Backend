using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using System;
using System.Collections.Generic;

namespace CalculateFunding.Tests.Common.Helpers
{
    public class TestConfigurationRefresherProvider : IConfigurationRefresherProvider
    {
        public IEnumerable<IConfigurationRefresher> Refreshers => throw new NotImplementedException();
    }
}
