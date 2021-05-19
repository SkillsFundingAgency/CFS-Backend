using System;
using System.Linq.Expressions;
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

        public static bool BooleanExpressionMatches<TItem>(Expression<Func<TItem, bool>> expression,
            TItem shouldMatchForExpression)
            => expression.Compile()(shouldMatchForExpression);
    }
}