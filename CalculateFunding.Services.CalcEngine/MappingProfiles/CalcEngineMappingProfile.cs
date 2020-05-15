using AutoMapper;
using CalculateFunding.Models.Aggregations;

namespace CalculateFunding.Services.CalcEngine.MappingProfiles
{
    public class CalcEngineMappingProfile : Profile
    {
        public CalcEngineMappingProfile()
        {
            CreateMap<Common.ApiClient.DataSets.Models.DatasetAggregations, DatasetAggregation>();
            CreateMap<Common.ApiClient.DataSets.Models.AggregatedField, AggregatedField>();
            CreateMap<Common.ApiClient.DataSets.Models.AggregatedType, AggregatedType>();
        }
    }
}
