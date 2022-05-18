﻿using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.SqlExport;
using System.Collections.Generic;

namespace CalculateFunding.Services.Results.SqlExport
{
    public interface ISqlImportContext
    {
        ICosmosDbFeedIterator Documents { get; set; }

        IDataTableBuilder<ProviderResult> CalculationRuns { get; set; }
        IDataTableBuilder<ProviderResult> ProviderSummaries { get; set; }
        IDataTableBuilder<ProviderResult> PaymentFundingLines { get; set; }
        IDataTableBuilder<ProviderResult> InformationFundingLines { get; set; }
        IDataTableBuilder<ProviderResult> TemplateCalculations { get; set; }
        IDataTableBuilder<ProviderResult> AdditionalCalculations { get; set; }

        HashSet<string> Providers { get; set; }

        void AddRows(ProviderResult dto);
    }
}
