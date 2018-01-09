using AutoMapper;
using CalculateFunding.Models.Specs;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.MappingProfiles
{
    public class SpecificationsMappingProfile : Profile
    {
        public SpecificationsMappingProfile()
        {
            CreateMap<SpecificationCreateModel, Specification>().AfterMap((src, dest) => { dest.Id = Guid.NewGuid().ToString(); });

            CreateMap<PolicyCreateModel, Policy>().AfterMap((src, dest) => { dest.Id = Guid.NewGuid().ToString(); });
        }
    }
}
