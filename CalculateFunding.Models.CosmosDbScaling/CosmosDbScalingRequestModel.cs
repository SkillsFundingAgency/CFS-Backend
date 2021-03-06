﻿using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.CosmosDbScaling
{
    public class CosmosDbScalingRequestModel
    {
        public IEnumerable<CosmosCollectionType> RepositoryTypes { get; set; }

        public string JobDefinitionId { get; set; }
    }
}
