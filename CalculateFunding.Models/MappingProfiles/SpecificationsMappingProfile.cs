using System;
using AutoMapper;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Models.MappingProfiles
{
    public class SpecificationsMappingProfile : Profile
    {
        public SpecificationsMappingProfile()
        {
            CreateMap<CalculationCreateModel, Calculation>()
                .AfterMap((src, dest) => { dest.Id = Guid.NewGuid().ToString(); })
                .ForMember(m => m.Id, opt => opt.Ignore())
                .ForMember(m => m.AllocationLine, opt => opt.Ignore())
                .ForMember(m => m.LastUpdated, opt => opt.Ignore());

            CreateMap<Specification, SpecificationSummary>()
                .ForMember(m => m.Description, opt => opt.MapFrom(s => s.Current.Description))
                .ForMember(m => m.FundingPeriod, opt => opt.MapFrom(s => s.Current.FundingPeriod))
                .ForMember(m => m.FundingStreams, opt => opt.MapFrom(s => s.Current.FundingStreams))
                .ForMember(m => m.ApprovalStatus, opt => opt.MapFrom(p => p.Current.PublishStatus))
                .ForMember(m => m.ProviderVersionId, opt => opt.MapFrom(p => p.Current.ProviderVersionId))
                .ForMember(m => m.TemplateId, opt => opt.MapFrom(p => p.Current.TemplateId));

            CreateMap<Calculation, CalculationCurrentVersion>();
        }
    }
}
