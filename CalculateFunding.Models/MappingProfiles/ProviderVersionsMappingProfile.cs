using AutoMapper;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Providers.ViewModels;

namespace CalculateFunding.Models.MappingProfiles
{
    public class ProviderMappingProfile : Profile
    {
        private const string MASTER_KEY = "master";

        public ProviderMappingProfile()
        {
            CreateMap<MasterProviderVersionViewModel, MasterProviderVersion>()
                .ForMember(c => c.ProviderVersionId, opt => opt.MapFrom(c => c.ProviderVersionId))
                .ForMember(c => c.Id, opt => opt.MapFrom(s => MASTER_KEY));

            CreateMap<ProviderVersionViewModel, ProviderVersion>()
                .ForMember(c => c.ProviderVersionId, opt => opt.MapFrom(c => c.ProviderVersionId))
                .ForMember(c => c.Providers, opt => opt.MapFrom(c => c.Providers));
        }
    }
}
