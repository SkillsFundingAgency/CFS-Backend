using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Calcs;
using Calculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;

namespace CalculateFunding.Services.Calcs
{
    public interface ITemplateContentsCalculationQuery
    {
        Calculation GetTemplateContentsForMappingItem(TemplateMappingItem mappingItem,
            TemplateMetadataContents templateMetadataContents);
    }
}