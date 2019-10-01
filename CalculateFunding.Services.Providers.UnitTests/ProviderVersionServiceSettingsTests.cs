using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Providers.UnitTests
{
    [TestClass]
    public class ProviderVersionServiceSettingsTests
    {
        [TestMethod]
        public void DefaultsIsFileSystemCacheEnabledToTrue()
        {
            new ProviderVersionServiceSettings()
                .IsFileSystemCacheEnabled
                .Should()
                .BeTrue();
        }
    }
}