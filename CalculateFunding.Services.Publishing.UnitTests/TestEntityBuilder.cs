using CalculateFunding.Services.Core.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public abstract class TestEntityBuilder
    {
        protected string NewRandomString() => new RandomString();

        protected bool NewRandomFlag() => new RandomNumberBetween(0, 1) == 1;
    }
}