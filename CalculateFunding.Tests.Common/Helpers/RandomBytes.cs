using System.Linq;
using System.Text;
using CalculateFunding.Common.Extensions;

namespace CalculateFunding.Tests.Common.Helpers
{
    public class RandomBytes
    {
        private readonly byte[] _value;

        public RandomBytes()
            : this(Encoding.UTF8.GetBytes(new RandomString()))
        {
        }

        private RandomBytes(byte[] value)
        {
            _value = value;
        }
        
        public static implicit operator byte[](RandomBytes value)
        {
            return value._value;
        }

        public static implicit operator RandomBytes(byte[] value)
        {
            return new RandomBytes(value);
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
            return _value.Select(_ => _.ToString()).JoinWith(',');
        }
    }
}