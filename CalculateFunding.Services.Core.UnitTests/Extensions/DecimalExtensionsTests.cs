using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Core.Extensions
{
    [TestClass]
    public class DecimalExtensionsTests
    {
        [TestMethod]
        public void WhenAWholeNumberWithinInt32RangeAndInputIsDecimalThenDisplayIsRounded()
        {
            object result = 1M.DecimalAsObject();

            result
                .Should()
                .Be(1);
        }

        [TestMethod]
        public void WhenAWholeNumberWithinInt32RangeAndInputIsNullableDecimalThenDisplayIsRounded()
        {
            decimal? inputNumber = 1M;
            object result = inputNumber.DecimalAsObject();

            result
                .Should()
                .Be(1);
        }

        [TestMethod]
        public void WhenAWholeNumberOutsideInt32RangeThenDisplayIsReturnedAsDecimal()
        {
            decimal inputNumber = int.MaxValue + 5M;

            object result = inputNumber.DecimalAsObject();

            result
                .Should()
                .Be(2147483652M);
        }

        [TestMethod]
        public void WhenAWholeNumberOutsideInt32RangeAndInputIsNullableDecimalThenDisplayIsReturnedAsDecimal()
        {
            decimal? inputNumber = int.MaxValue + 5M;

            object result = inputNumber.DecimalAsObject();

            result
                .Should()
                .Be(2147483652M);
        }

        [TestMethod]
        public void WhenAWholeNumberOutsideInt32RangeMinAndInputIsDecimalThenDisplayIsReturnedAsDecimal()
        {
            decimal? inputNumber = int.MinValue - 1M;

            object result = inputNumber.DecimalAsObject();

            result
                .Should()
                .Be(-2147483649M);
        }

        [TestMethod]
        public void WhenAWholeNumberIsMinInt32_ThenIntegerIsReturned()
        {
            decimal? inputNumber = int.MinValue;

            object result = inputNumber.DecimalAsObject();

            result
                .Should()
                .Be(-2147483648);
        }

        [TestMethod]
        public void WhenAWholeNumberIsMaxInt32_ThenIntegerIsReturned()
        {
            decimal? inputNumber = int.MaxValue;

            object result = inputNumber.DecimalAsObject();

            result
                .Should()
                .Be(2147483647);
        }

        [TestMethod]
        public void WhenANullDecimalIsProvided_ThenNullableDecimalIsReturned()
        {
            object result = ((decimal?) null).DecimalAsObject();

            result
                .Should()
                .BeNull();
        }
    }
}
