using CalculateFunding.Models.Jobs;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Jobs
{
    public class JobDefinitionBuilder : TestEntityBuilder
    {
        private string _id;

        public JobDefinitionBuilder WithId(string id)
        {
            _id = id;

            return this;
        }
        
        public JobDefinition Build()
        {
            return new JobDefinition
            {
                Id = _id ?? NewRandomString()
            };
        }
    }
}