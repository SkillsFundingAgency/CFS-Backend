using CalculateFunding.Common.ApiClient.Policies.Models;
using System;
using Enum = CalculateFunding.Common.ApiClient.Graph.Models.Enum;

namespace CalculateFunding.Services.Specs.ObsoleteItems
{
    public class TemplateCalculationEnumDetail
    {
        public TemplateMetadataCalculation TemplateCalculation { get; set; }

        public Enum @Enum { get; set; }
    }
}
