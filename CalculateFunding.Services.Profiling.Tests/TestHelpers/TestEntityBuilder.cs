using System;

namespace CalculateFunding.Services.Profiling.Tests.TestHelpers
{
    public abstract class TestEntityBuilder
    {
        protected string NewCleanRandomString() => NewRandomString().Replace("-", "");
        
        protected string NewRandomString() => new RandomString();

        protected bool NewRandomFlag() => new RandomBoolean();
        
        protected TEnum NewRandomEnum<TEnum>() where TEnum : struct => new RandomEnum<TEnum>();
        
        protected int NewRandomNumberBetween(int min, int max) => new RandomNumberBetween(min, max);
        
        protected uint NewRandomUint() => (uint)NewRandomNumberBetween(0, int.MaxValue);
        
        protected DateTimeOffset NewRandomDateTime() => new RandomDateTime();

        protected string NewRandomMonth() => NewRandomDateTime().ToString("MMMM");

        protected int NewRandomYear() => NewRandomDateTime().Year;
    }
}