using System;
using AutoMapper;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Scenarios;

namespace CalculateFunding.Services.TestEngine.MappingProfiles
{   
    public class TestEngineMappingProfile : Profile
    {
        public TestEngineMappingProfile()
        {
          
            CreateMap<Common.ApiClient.Calcs.Models.BuildProject, BuildProject>();            
            CreateMap<TestScenarioResult, TestScenarioResultIndex>()
                .ForMember(m => m.Id, opt => opt.Ignore())
                .ForMember(m => m.SpecificationId, opt => opt.MapFrom(s => s.Specification.Id))
                .ForMember(m => m.SpecificationName, opt => opt.MapFrom(s => s.Specification.Name))
                .ForMember(m => m.ProviderId, opt => opt.MapFrom(s => s.Provider.Id))
                .ForMember(m => m.ProviderName, opt => opt.MapFrom(s => s.Provider.Name))
                .ForMember(m => m.TestScenarioId, opt => opt.MapFrom(s => s.TestScenario.Id))
                .ForMember(m => m.TestScenarioName, opt => opt.MapFrom(s => s.TestScenario.Name))
                .ForMember(m => m.LastUpdatedDate, opt => opt.Ignore()).
                AfterMap((source, dest) =>
                {
                    dest.LastUpdatedDate = DateTimeOffset.Now;
                })
                .ForMember(m => m.ProviderType, opt => opt.Ignore())
                .ForMember(m => m.LocalAuthority, opt => opt.Ignore())
                .ForMember(m => m.ProviderSubType, opt => opt.Ignore())
                .ForMember(m => m.UKPRN, opt => opt.Ignore())
                .ForMember(m => m.UPIN, opt => opt.Ignore())
                .ForMember(m => m.URN, opt => opt.Ignore())
                .ForMember(m => m.EstablishmentNumber, opt => opt.Ignore())
                .ForMember(m => m.OpenDate, opt => opt.Ignore());
        }
    }
}
