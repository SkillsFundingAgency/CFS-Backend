using AutoMapper;

namespace CalculateFunding.Api.External.MappingProfiles
{
    public class ExternalApiMappingProfile : Profile
    {
        public ExternalApiMappingProfile()
        {
            // Create mappings for v1 models
            CreateMap<Models.Policy.Period, V1.Models.Period>()
                .ForMember(p => p.Id, mce => mce.MapFrom(fp => fp.Id))
                .ForMember(p => p.StartYear, mce => mce.MapFrom(fp => fp.StartYear))
                .ForMember(p => p.EndYear, mce => mce.MapFrom(fp => fp.EndYear));

            CreateMap<Models.Policy.AllocationLine, V1.Models.AllocationLine>()
                .ForMember(a => a.Id, mce => mce.MapFrom(al => al.Id))
                .ForMember(a => a.ContractRequired, mce => mce.MapFrom(al => al.IsContractRequired ? "Y" : "N"))
                .ForMember(a => a.Name, mce => mce.MapFrom(al => al.Name));

            CreateMap<Models.Policy.FundingStream, V1.Models.FundingStream>()
                .ForMember(f => f.Id, mce => mce.MapFrom(fs => fs.Id))
                .ForMember(f => f.Name, mce => mce.MapFrom(fs => fs.Name))
                .ForMember(f => f.AllocationLines, mce => mce.MapFrom(fs => fs.AllocationLines));

            // Create mappings for v2 models
            CreateMap<Models.Policy.Period, V2.Models.Period>()
                .ForMember(p => p.Id, mce => mce.MapFrom(fp => fp.Id))
                .ForMember(p => p.StartYear, mce => mce.MapFrom(fp => fp.StartYear))
                .ForMember(p => p.EndYear, mce => mce.MapFrom(fp => fp.EndYear));

            CreateMap<Models.Policy.AllocationLine, V2.Models.AllocationLine>()
                .ForMember(a => a.Id, mce => mce.MapFrom(al => al.Id))
                .ForMember(a => a.ContractRequired, mce => mce.MapFrom(al => al.IsContractRequired ? "Y" : "N"))
                .ForMember(a => a.Name, mce => mce.MapFrom(al => al.Name));

            CreateMap<Models.Policy.FundingStream, V2.Models.FundingStream>()
                .ForMember(f => f.Id, mce => mce.MapFrom(fs => fs.Id))
                .ForMember(f => f.Name, mce => mce.MapFrom(fs => fs.Name))
                .ForMember(f => f.AllocationLines, mce => mce.MapFrom(fs => fs.AllocationLines));
        }
    }
}
