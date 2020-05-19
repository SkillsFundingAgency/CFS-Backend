namespace CalculateFunding.Tests.Common.Helpers
{
    public class RandomBoolean
    {
        private readonly bool _value;

        public RandomBoolean()
        {
            _value = new RandomNumberBetween(0, 2) == 0;
        }

        public static implicit operator bool(RandomBoolean randomNumberBetween)
        {
            return randomNumberBetween._value;
        }

        public override bool Equals(object obj)
        {
            return obj?.GetHashCode() == GetHashCode();
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(_value);
        }

        public override string ToString()
        {
            return _value.ToString();
        }
    }
}