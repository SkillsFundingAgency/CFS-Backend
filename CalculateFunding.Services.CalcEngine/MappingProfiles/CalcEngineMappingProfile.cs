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

            CreateMap<Common.ApiClient.Providers.Models.Search.ProviderVersionSearchResult, Models.ProviderLegacy.ProviderSummary>()
                .ForMember(dst => dst.CompaniesHouseNumber, map => map.Ignore())
                .ForMember(dst => dst.GroupIdNumber, map => map.Ignore())
                .ForMember(dst => dst.GovernmentOfficeRegionName, map => map.Ignore())
                .ForMember(dst => dst.GovernmentOfficeRegionCode, map => map.Ignore())
                .ForMember(dst => dst.DistrictName, map => map.Ignore())
                .ForMember(dst => dst.DistrictCode, map => map.Ignore())
                .ForMember(dst => dst.WardName, map => map.Ignore())
                .ForMember(dst => dst.WardCode, map => map.Ignore())
                .ForMember(dst => dst.CensusWardName, map => map.Ignore())
                .ForMember(dst => dst.CensusWardCode, map => map.Ignore())
                .ForMember(dst => dst.MiddleSuperOutputAreaName, map => map.Ignore())
                .ForMember(dst => dst.MiddleSuperOutputAreaCode, map => map.Ignore())
                .ForMember(dst => dst.LowerSuperOutputAreaName, map => map.Ignore())
                .ForMember(dst => dst.LowerSuperOutputAreaCode, map => map.Ignore())
                .ForMember(dst => dst.ParliamentaryConstituencyName, map => map.Ignore())
                .ForMember(dst => dst.ParliamentaryConstituencyCode, map => map.Ignore());


        }
    }
}
