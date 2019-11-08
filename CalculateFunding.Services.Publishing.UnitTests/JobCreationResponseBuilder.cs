using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class JobCreationResponseBuilder : TestEntityBuilder
    {
        private string _id;

        public JobCreationResponseBuilder WithJobId(string id)
        {
            _id = id;

            return this;
        }

        public JobCreationResponse Build()
        {
            return new JobCreationResponse
            {
                JobId = _id ?? NewRandomString(),
            };
        }
    }
}