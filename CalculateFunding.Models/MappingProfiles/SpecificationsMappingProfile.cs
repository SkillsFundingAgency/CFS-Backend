using AutoMapper;
using CalculateFunding.Models.Specs;
using System;

namespace CalculateFunding.Models.MappingProfiles
{
    public class SpecificationsMappingProfile : Profile
    {
        public SpecificationsMappingProfile()
        {
            CreateMap<PolicyCreateModel, Policy>()
                .AfterMap((src, dest) => { dest.Id = Guid.NewGuid().ToString(); })
                .ForMember(m => m.Id, opt => opt.Ignore())
                .ForMember(m => m.Calculations, opt => opt.Ignore())
                .ForMember(m => m.SubPolicies, opt => opt.Ignore());

            CreateMap<CalculationCreateModel, Calculation>()
                .AfterMap((src, dest) => { dest.Id = Guid.NewGuid().ToString(); })
                .ForMember(m => m.Id, opt => opt.Ignore())
                .ForMember(m => m.AllocationLine, opt => opt.Ignore())
                .ForMember(d => d.CalculationType, opt => opt.ResolveUsing(o => Enum.Parse(typeof(Calcs.CalculationType), o.CalculationType, true)));

            CreateMap<Specification, SpecificationSummary>()
                .ForMember(m => m.Description, opt => opt.MapFrom(s => s.Current.Description))
                .ForMember(m => m.FundingPeriod, opt => opt.MapFrom(s => s.Current.FundingPeriod))
                .ForMember(m => m.FundingStreams, opt => opt.MapFrom(s => s.Current.FundingStreams));

            CreateMap<Calculation, CalculationCurrentVersion>()
                .ForMember(m => m.PolicyId, opt => opt.Ignore())
                .ForMember(m => m.PolicyName, opt => opt.Ignore());
        }
    }
}
