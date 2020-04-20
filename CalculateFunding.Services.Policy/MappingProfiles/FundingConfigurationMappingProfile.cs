using AutoMapper;
using CalculateFunding.Models.Policy.FundingPolicy;
using CalculateFunding.Models.Policy.FundingPolicy.ViewModels;

namespace CalculateFunding.Services.Policy.MappingProfiles
{
    public class FundingConfigurationMappingProfile : Profile
    {
        public FundingConfigurationMappingProfile()
        {
            CreateMap<FundingConfigurationViewModel, FundingConfiguration>()
                .ForMember(dest => dest.FundingStreamId, opt => opt.MapFrom((src, dest, destMember, context) => context.Items[nameof(FundingConfiguration.FundingStreamId)]))
                .ForMember(dest => dest.FundingPeriodId, opt => opt.MapFrom((src, dest, destMember, context) => context.Items[nameof(FundingConfiguration.FundingPeriodId)]))
                .AfterMap((source, dest) =>
                {
                    dest.Id = $"config-{dest.FundingStreamId}-{dest.FundingPeriodId}";
                });
        }
    }
}
