using FluentAssertions;

namespace CalculateFunding.Tests.Common.Helpers
{
    public class ConstraintHelpers
    {
        public static bool AreEquivalent<TItem>(TItem actual,
            TItem expected)
        {
            try
            {
                actual
                    .Should()
                    .BeEquivalentTo(expected);
                
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}