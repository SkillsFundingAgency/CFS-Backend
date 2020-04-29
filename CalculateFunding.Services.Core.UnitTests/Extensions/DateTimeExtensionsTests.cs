using System;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Core.Extensions
{
    [TestClass]
    public class DateTimeExtensionsTests
    {

        [TestMethod]
        public void TrimToTheSecondStripsMillisecondsFromSuppliedDate()
        {
            DateTime input = NewRandomDateTime();
            DateTime expectedOutput = new DateTime(input.Year, input.Month, input.Day, input.Hour, input.Minute, input.Second);
            
            input.TrimToTheSecond()
                .Should()
                .Be(expectedOutput);
        }

        [TestMethod]
        public void TrimToTheMinuteStripsSecondsAndMillisecondsFromSuppliedDate()
        {
            DateTime input = NewRandomDateTime();
            DateTime expectedOutput = new DateTime(input.Year, input.Month, input.Day, input.Hour, input.Minute, 0);
            
            input.TrimToTheMinute()
                .Should()
                .Be(expectedOutput);
        }
        
        [TestMethod]
        public void DateTimeOffsetTrimToTheMinuteStripsSecondsAndMillisecondsFromSuppliedDate()
        {
            DateTime dateTime = NewRandomDateTime();
            DateTimeOffset? input = new DateTimeOffset(dateTime);
            DateTimeOffset? expectedOutput = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0, dateTime.Kind);
            
            input.TrimToTheMinute()
                .Should()
                .Be(expectedOutput);
        }
        

        private static RandomDateTime NewRandomDateTime() => new RandomDateTime();
    }
}