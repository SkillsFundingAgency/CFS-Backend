using System;
using System.Linq;

namespace CalculateFunding.Services.Profiling.Tests.TestHelpers
{
    public class RandomEnum<TEnum>
        where TEnum : struct
    {
        private readonly TEnum _value;

        public RandomEnum(params TEnum[] excluding)
        {
            TEnum[] possibleValues = Enum.GetValues(typeof(TEnum))
                .Cast<TEnum>()
                .Except(excluding)
                .ToArray();

            _value = possibleValues[(int) new RandomNumberBetween(0, possibleValues.Length)];
        }

        public static implicit operator TEnum(RandomEnum<TEnum> randomEnum)
        {
            return randomEnum._value;
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