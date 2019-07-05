﻿using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Results;
using System.Collections.Generic;

namespace CalculateFunding.Services.CalcEngine.Interfaces
{
    public interface IAllocationModel
    {
        IEnumerable<CalculationResult> Execute(List<ProviderSourceDataset> datasets, ProviderSummary providerSummary, IEnumerable<CalculationAggregation> aggregationValues = null);
    }
}
