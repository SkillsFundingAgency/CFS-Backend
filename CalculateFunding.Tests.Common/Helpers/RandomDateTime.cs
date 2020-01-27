using System;
using System.Globalization;

namespace CalculateFunding.Tests.Common.Helpers
{
    public class RandomDateTime
    {
        private readonly DateTime _value;

        public RandomDateTime()
        {
            _value = DateTime.UtcNow.AddDays(-new RandomNumberBetween(1, 10)).AddHours(new RandomNumberBetween(0, 23)).AddMonths(new RandomNumberBetween(0, 12));
        }
        
        private RandomDateTime(DateTime value)
        {
            _value = value;
        }
        
        public static implicit operator DateTime(RandomDateTime randomDateTime)
        {
            return randomDateTime._value;
        }
        
        public static implicit operator RandomDateTime(DateTime dateTime)
        {
            return new RandomDateTime(dateTime);
        }
        
        public static implicit operator DateTimeOffset(RandomDateTime randomDateTime)
        {
            return randomDateTime._value;
        }
        
        public static implicit operator RandomDateTime(DateTimeOffset dateTime)
        {
            return new RandomDateTime(dateTime.DateTime);
        }
        
        public override bool Equals(object obj)
        {
            return obj?.GetHashCode() == GetHashCode();
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override string ToString()
        {
            return _value.ToString(CultureInfo.InvariantCulture);
        }
    }
}