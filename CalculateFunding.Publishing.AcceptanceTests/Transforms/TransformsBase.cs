using FluentAssertions;
using TechTalk.SpecFlow;

namespace CalculateFunding.Publishing.AcceptanceTests.Transforms
{
    public class TransformsBase
    {
        protected static void EnsureTableHasData(Table variationPointersTable)
        {
            variationPointersTable
                .Should()
                .NotBeNull();

            variationPointersTable
                .RowCount
                .Should()
                .BeGreaterOrEqualTo(1);
        }
    }
}