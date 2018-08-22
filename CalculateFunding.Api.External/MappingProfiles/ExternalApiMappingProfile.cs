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
			CreateMap<FundingPeriod, Period>()
				.ForMember(p => p.PeriodId, mce => mce.MapFrom(fp => fp.Id))
				.ForMember(p => p.PeriodType, mce => mce.MapFrom(fp => fp.Type))
				.ForMember(p => p.StartDate, mce => mce.MapFrom(fp => fp.StartDate))
				.ForMember(p => p.EndDate, mce => mce.MapFrom(fp => fp.EndDate))
				.ForAllOtherMembers(mce => mce.Ignore());
		}
	}
}
