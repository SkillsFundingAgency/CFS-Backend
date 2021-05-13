using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Core.Extensions
{
    [TestClass]
    public class CollectionExtensionTests
    {
        [TestMethod]
        [DynamicData(nameof(AddOrUpdateRangeExamples), DynamicDataSourceType.Method)]
        public void AddOrUpdateRangeHandlesExistingKeys(IEnumerable<(int, int)> dictionaryContents,
            IEnumerable<(int, int)> itemsToAddOrUpdate,
            IEnumerable<(int, int)> expectedDictionaryContents)
        {
            IDictionary<int, int> dictionary = new Dictionary<int, int>(AsKeyValuePairs(dictionaryContents));

            dictionary.AddOrUpdateRange(AsKeyValuePairs(itemsToAddOrUpdate));

            IEnumerable<KeyValuePair<int, int>> expectedContents = AsKeyValuePairs(expectedDictionaryContents);

            dictionary
                .Should()
                .BeEquivalentTo(expectedContents);
        }

        public static IEnumerable<object[]> AddOrUpdateRangeExamples()
        {
            yield return new object[]
            {
                AsArray((1, 1), (2, 3)),
                AsArray((2, 4), (3, 5)),
                AsArray((1, 1), (2, 4), (3, 5))
            };
            yield return new object[]
            {
                AsArray((1, 1), (2, 3)),
                AsArray((2, 4), (3, 5), (1, 9)),
                AsArray((1, 9), (2, 4), (3, 5))
            };
            yield return new object[]
            {
                AsArray((1, 1), (2, 3)),
                AsArray((3, 4), (4, 5), (5, 9)),
                AsArray((1, 1), (2, 3), (3, 4), (4, 5), (5, 9))
            };
        }

        private static TItem[] AsArray<TItem>(params TItem[] items) => items;

        private KeyValuePair<int, int> AsKeyValuePair((int key, int value) item) => new KeyValuePair<int, int>(item.key, item.value);

        private IEnumerable<KeyValuePair<int, int>> AsKeyValuePairs(IEnumerable<(int key, int value)> items) => items.Select(AsKeyValuePair);
    }
}