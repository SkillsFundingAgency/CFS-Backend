using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Api.External.V1.Models;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Api.External.MappingProfiles
{
	public class ExternalApiMappingProfile : Profile
	{
		public ExternalApiMappingProfile()
		{
			CreateMap<Models.Specs.Period, V1.Models.Period>()
				.ForMember(p => p.PeriodId, mce => mce.MapFrom(fp => fp.Id))
				.ForMember(p => p.StartDate, mce => mce.MapFrom(fp => fp.StartDate))
				.ForMember(p => p.EndDate, mce => mce.MapFrom(fp => fp.EndDate))
				.ForAllOtherMembers(mce => mce.Ignore());

            CreateMap<Models.Specs.AllocationLine, V1.Models.AllocationLine>()
                .ForMember(a => a.AllocationLineCode, mce => mce.MapFrom(al => al.Id))
                .ForMember(a => a.AllocationLineName, mce => mce.MapFrom(al => al.Name))
                .ForAllOtherMembers(mce => mce.Ignore());

            CreateMap<Models.Specs.FundingStream, V1.Models.FundingStream>()
                .ForMember(f => f.FundingStreamCode, mce => mce.MapFrom(fs => fs.Id))
                .ForMember(f => f.FundingStreamName, mce => mce.MapFrom(fs => fs.Name))
                .ForMember(f => f.AllocationLines, mce => mce.MapFrom(fs => fs.AllocationLines))
                .ForAllOtherMembers(mce => mce.Ignore());
        }
	}
}
