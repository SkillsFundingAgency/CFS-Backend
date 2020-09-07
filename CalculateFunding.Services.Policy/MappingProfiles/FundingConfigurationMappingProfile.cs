using AutoMapper;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Policy;
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

            CreateMap<FundingDateViewModel, FundingDate>()
                .ForMember(dest => dest.FundingStreamId, opt => opt.MapFrom((src, dest, destMember, context) => context.Items[nameof(FundingDate.FundingStreamId)]))
                .ForMember(dest => dest.FundingPeriodId, opt => opt.MapFrom((src, dest, destMember, context) => context.Items[nameof(FundingDate.FundingPeriodId)]))
                .ForMember(dest => dest.FundingLineId, opt => opt.MapFrom((src, dest, destMember, context) => context.Items[nameof(FundingDate.FundingLineId)]))
                .AfterMap((source, dest) =>
                {
                    dest.Id = $"fundingdate-{dest.FundingStreamId}-{dest.FundingPeriodId}-{dest.FundingLineId}";
                });

            CreateMap<FundingLine, TemplateMetadataFundingLine>();
            CreateMap<Calculation, TemplateMetadataCalculation>();
        }
    }
}
