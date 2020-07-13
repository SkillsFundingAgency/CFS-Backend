using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Results.UnitTests
{
    public class JobBuilder : TestEntityBuilder
    {
        private string _id;

        public JobBuilder WithId(string id)
        {
            _id = id;

            return this;
        }
        
        public Job NewJob()
        {
            return new Job
            {
                Id = _id ?? NewRandomString()
            };
        }
    }
}