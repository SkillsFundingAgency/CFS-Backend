using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Core.Caching.FileSystem
{
    [TestClass]
    public class FileSystemCacheSettingsTests
    {
        [TestMethod]
        public void DefaultPathToAppData()
        {
            new FileSystemCacheSettings()
                .Path
                .Should()
                .Be(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        }
    }
}