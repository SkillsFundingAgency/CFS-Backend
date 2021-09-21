﻿using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    /// <summary>
    /// Scoped class to store mappings of entities to SQL
    /// </summary>
    public class ReleaseToChannelSqlMappingContext : IReleaseToChannelSqlMappingContext
    {
        public ReleaseToChannelSqlMappingContext()
        {
            ReleasedProviders = new Dictionary<string, ReleasedProvider>();
        }

        public Dictionary<string, ReleasedProvider> ReleasedProviders { get; }
    }
}