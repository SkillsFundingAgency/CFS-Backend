using AutoMapper;
using CalculateFunding.Models.Calcs;
using ApiModels = CalculateFunding.Common.ApiClient.Calcs.Models;

namespace CalculateFunding.Models.MappingProfiles
{
    public class ResultsMappingProfile : Profile
    {
        public ResultsMappingProfile()
        {
            CreateMap<ApiModels.CalculationValueType, CalculationValueType>();

            CreateMap<CalculationResult, CalculationResultResponse>()
                .ForMember(m => m.CalculationValueType, opt => opt.Ignore());

            CreateMap<ProviderResult, ProviderResultResponse>();
        }
    }
}
