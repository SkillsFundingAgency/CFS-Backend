using AutoMapper;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Search.Results;

namespace CalculateFunding.Services.Providers
{
    public class ProviderMappingProfile : Profile
    {
        public ProviderMappingProfile()
        {
            CreateMap<ProviderSearchResult, ProviderSummary>()
                .ForMember(p => p.Id, opt => opt.MapFrom(s => s.ProviderId))
                .ForMember(p => p.DateOpened, opt => opt.MapFrom(s => s.OpenDate))
                .ForMember(p => p.DateClosed, opt => opt.MapFrom(s => s.CloseDate));
        }
    }
}
