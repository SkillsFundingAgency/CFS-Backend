using System;

namespace CalculateFunding.Tests.Common.Helpers
{
    public class RandomNumberBetween
    {
        private readonly int _value;

        public RandomNumberBetween(int min,
            
            int max)
        {
            _value = new Random().Next(min, max);
        }

        public static implicit operator int(RandomNumberBetween randomNumberBetween)
        {
            return randomNumberBetween._value;
        }

        public static implicit operator double(RandomNumberBetween randomNumberBetween)
        {
            return randomNumberBetween._value;
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
            return _value.ToString();
        }
    }
}