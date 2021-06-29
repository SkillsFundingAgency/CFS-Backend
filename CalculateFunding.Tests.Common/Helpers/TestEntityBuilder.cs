using System;

namespace CalculateFunding.Tests.Common.Helpers
{
    public abstract class TestEntityBuilder
    {
        protected string NewCleanRandomString() => NewRandomString().Replace("-", "");
        
        protected string NewRandomString() => new RandomString();

        protected bool NewRandomFlag() => new RandomBoolean();
        
        protected TEnum NewRandomEnum<TEnum>(params TEnum[] except) where TEnum : struct => new RandomEnum<TEnum>(except);
        
        protected int NewRandomNumberBetween(int min, int max) => new RandomNumberBetween(min, max);
        
        protected int NewRandomTimeStamp() => new RandomNumberBetween(10000, int.MaxValue);

        protected uint NewRandomUint() => (uint)NewRandomNumberBetween(0, int.MaxValue);
        
        protected DateTimeOffset NewRandomDateTime() => new RandomDateTime();

        protected string NewRandomMonth() => NewRandomDateTime().ToString("MMMM");

        protected int NewRandomYear() => NewRandomDateTime().Year;
    }
}