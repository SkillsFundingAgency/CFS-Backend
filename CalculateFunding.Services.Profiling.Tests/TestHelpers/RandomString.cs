using System;

namespace CalculateFunding.Services.Profiling.Tests.TestHelpers
{
    public class RandomString
    {
        private readonly string _value;

        public RandomString()
            : this(Guid.NewGuid().ToString())
        {
        }

        private RandomString(string value)
        {
            _value = value;
        }

        public static implicit operator string(RandomString value)
        {
            return value._value;
        }

        public static implicit operator RandomString(string value)
        {
            return new RandomString(value);
        }

        public override bool Equals(object obj)
        {
            return (obj is string || obj is RandomString) &&
                   obj.GetHashCode() == GetHashCode();
        }

        public override int GetHashCode()
        {
            return _value?.GetHashCode() ?? 0;
        }

        public override string ToString()
        {
            return _value;
        }
    }
}