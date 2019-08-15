using Microsoft.AspNetCore.Http;

namespace CalculateFunding.Tests.Common.Helpers
{
    public abstract class TestEntityBuilder
    {
        protected string NewCleanRandomString() => NewRandomString().Replace("-", "");
        
        protected string NewRandomString() => new RandomString();

        protected bool NewRandomFlag() => new RandomNumberBetween(0, 1) == 1;
        
        protected TEnum NewRandomEnum<TEnum>() where TEnum : struct => new RandomEnum<TEnum>();
        
        protected int NewRandomNumberBetween(int min, int max) => new RandomNumberBetween(min, max);
        
        protected uint NewRandomUint() => (uint)NewRandomNumberBetween(0, int.MaxValue);
    }
}