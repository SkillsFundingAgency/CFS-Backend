namespace CalculateFunding.Tests.Common.Helpers
{
    public abstract class TestEntityBuilder
    {
        protected string NewCleanRandomString() => NewRandomString().Replace("-", "");
        
        protected string NewRandomString() => new RandomString();

        protected bool NewRandomFlag() => new RandomNumberBetween(0, 1) == 1;
    }
}