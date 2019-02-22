using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Models.UnitTests
{
    [TestClass]
    public class ExtensionModelTests
    {
        [TestMethod]
        public void ToBatches()
        {
            IEnumerable<int> testNumbers = Enumerable.Range(0, 5026);

            IEnumerable<IEnumerable<int>> results = testNumbers.ToBatches(1000);

            results
                .Should()
                .HaveCount(6);

            results
                .First()
               .Should()
               .HaveCount(1000);

            results
                .Last()
                .Should()
                .HaveCount(26);
        }
    }
}
