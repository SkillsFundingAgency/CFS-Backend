using CalculateFunding.Services.Profiling.ReProfilingStrategies;
using CalculateFunding.Services.Profiling.Tests.TestHelpers;
using Moq;

namespace CalculateFunding.Services.Profiling.Tests.Services
{
    public class ReProfilingStrategyStubBuilder : TestEntityBuilder
    {
        private string _key;
        private string _name;
        private string _description;

        public ReProfilingStrategyStubBuilder WithKey(string key)
        {
            _key = key;

            return this;
        }

        public ReProfilingStrategyStubBuilder WithDescription(string description)
        {
            _description = description;

            return this;
        }

        public ReProfilingStrategyStubBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public IReProfilingStrategy Build()
        {
            Mock<IReProfilingStrategy> stub = new Mock<IReProfilingStrategy>();

            stub.Setup(_ => _.DisplayName)
                .Returns(_name ?? NewRandomString());
            stub.Setup(_ => _.Description)
                .Returns(_description ?? NewRandomString());
            stub.Setup(_ => _.StrategyKey)
                .Returns(_key ?? NewRandomString());

            return stub.Object;
        }
    }
}