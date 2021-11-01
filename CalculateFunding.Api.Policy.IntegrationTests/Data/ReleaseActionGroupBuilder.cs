using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Tests.Common.Helpers;
using System;
using System.Collections.Generic;

namespace CalculateFunding.Api.Policy.IntegrationTests.Data
{
    public class ReleaseActionGroupBuilder : TestEntityBuilder
    {
        private string _id;
        private string _name;
        private int _sortOrder;
        private string _description;
        private IEnumerable<string> _channelCodes;

        public ReleaseActionGroupBuilder WithId(string id)
        {
            _id = id;
            return this;
        }

        public ReleaseActionGroupBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        public ReleaseActionGroupBuilder WithSortOrder(int sortOrder)
        {
            _sortOrder = sortOrder;
            return this;
        }

        public ReleaseActionGroupBuilder WithDescription(string description)
        {
            _description = description;
            return this;
        }

        public ReleaseActionGroupBuilder WithReleaseChannelCodes(params string[] releaseChannelCodes)
        {
            _channelCodes = releaseChannelCodes;
            return this;
        }

        public ReleaseActionGroup Build()
            => new ReleaseActionGroup()
            {
                Id = _id,
                Name = _name,
                SortOrder = _sortOrder,
                Description = _description ?? NewRandomString(),
                ChannelCodes = _channelCodes ?? Array.Empty<string>()
            };
    }
}
