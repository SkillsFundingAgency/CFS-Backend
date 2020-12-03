using System;
using CalculateFunding.Services.Core.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Batches
{
    [TestClass]
    public class UniqueIdentifierProviderTests
    {
        [TestMethod]
        public void CreatesGuidStrings()
        {
            string uuid = new UniqueIdentifierProvider().CreateUniqueIdentifier();

            Guid.TryParse(uuid, out Guid _)
                .Should()
                .BeTrue();
        }
    }
}