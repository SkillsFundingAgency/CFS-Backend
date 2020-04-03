using CalculateFunding.Models.Jobs;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Jobs
{
    public class JobDefinitionBuilder : TestEntityBuilder
    {
        private string _id;
        private bool _withoutId;
        private string _queueName;
        private string _topicName;

        public JobDefinitionBuilder WithQueueName(string queue)
        {
            _queueName = queue;

            return this;
        }

        public JobDefinitionBuilder WithTopicName(string topicName)
        {
            _topicName = topicName;

            return this;
        }


        public JobDefinitionBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public JobDefinitionBuilder WithoutId()
        {
            _withoutId = true;

            return this;
        }
        
        public JobDefinition Build()
        {
            return new JobDefinition
            {
                Id = _id ?? (_withoutId ? null : NewRandomString()),
                MessageBusTopic = _topicName,
                MessageBusQueue = _queueName
            };
        }
    }
}