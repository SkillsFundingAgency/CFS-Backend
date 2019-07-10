using AutoMapper;
using CalculateFunding.Models.Policy;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Calcs.MappingProfiles
{
    public class PolicyMappingProfile : Profile
    {
        public PolicyMappingProfile()
        {
            CreateMap<FundingStream, CalculateFunding.Common.ApiClient.Policies.Models.FundingStream>();
        }
    }
}
