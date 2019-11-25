using CalculateFunding.Models.Calcs;

namespace CalculateFunding.Services.Calcs
{
    public static class TemplateMappingExtensionMethods
    {
        public static TemplateMappingSummary ToSummaryResponseModel(this TemplateMapping templateMapping)
        {
            return new TemplateMappingSummary()
            {
                SpecificationId = templateMapping.SpecificationId,
                FundingStreamId = templateMapping.FundingStreamId,
                TemplateMappingItems = templateMapping?.TemplateMappingItems,
            };
        }
    }
}
