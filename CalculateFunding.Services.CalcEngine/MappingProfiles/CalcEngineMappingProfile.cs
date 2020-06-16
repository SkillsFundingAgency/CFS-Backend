using AutoMapper;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using GeneratorModels = CalculateFunding.Generators.Funding.Models;
using TemplateModels = CalculateFunding.Common.TemplateMetadata.Models;

namespace CalculateFunding.Services.CalcEngine.MappingProfiles
{
    public class CalcEngineMappingProfile : Profile
    {
        public CalcEngineMappingProfile()
        {
            CreateMap<Common.ApiClient.DataSets.Models.DatasetAggregations, DatasetAggregation>();
            CreateMap<Common.ApiClient.DataSets.Models.AggregatedField, AggregatedField>();
            CreateMap<Common.ApiClient.DataSets.Models.AggregatedType, AggregatedType>();

            CreateMap<TemplateModels.DistributionPeriod, GeneratorModels.DistributionPeriod>();
            CreateMap<TemplateModels.ProfilePeriod, GeneratorModels.ProfilePeriod>();
            CreateMap<TemplateModels.FundingLine, GeneratorModels.FundingLine>();
            CreateMap<TemplateModels.Calculation, GeneratorModels.Calculation>();
            CreateMap<TemplateModels.ReferenceData, GeneratorModels.ReferenceData>();

            CreateMap<Common.ApiClient.Calcs.Models.TemplateMappingItem, TemplateMappingItem>();
            CreateMap<Common.ApiClient.Calcs.Models.TemplateMapping, TemplateMapping>();
        }
    }
}
