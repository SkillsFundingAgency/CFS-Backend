using AutoMapper;
using CalculateFunding.Models.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.MappingProfiles
{
    public class ResultsMappingProfile : Profile
    {
        public ResultsMappingProfile()
        {
            CreateMap<TestScenarioResult, TestScenarioResultIndex>()
                .ForMember(m => m.Id, opt => opt.Ignore())
                .ForMember(m => m.SpecificationId, opt => opt.MapFrom(s => s.Specification.Id))
                .ForMember(m => m.SpecificationName, opt => opt.MapFrom(s => s.Specification.Name))
                .ForMember(m => m.ProviderId, opt => opt.MapFrom(s => s.Provider.Id))
                .ForMember(m => m.ProviderName, opt => opt.MapFrom(s => s.Provider.Name))
                .ForMember(m => m.TestScenarioId, opt => opt.MapFrom(s => s.TestScenario.Id))
                .ForMember(m => m.TestScenarioName, opt => opt.MapFrom(s => s.TestScenario.Name));
        }
    }
}
