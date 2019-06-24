using AutoMapper;
using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Search.Results;
using System.Collections.Generic;

namespace CalculateFunding.Services.Providers
{
    public class ProviderMappingProfile : Profile
    {
        public ProviderMappingProfile()
        {
            CreateMap<ProviderSearchResult, Models.Results.ProviderSummary>()
                .ForMember(p => p.Id, opt => opt.MapFrom(s => s.ProviderId))
                .ForMember(p => p.DateOpened, opt => opt.MapFrom(s => s.OpenDate))
                .ForMember(p => p.DateClosed, opt => opt.MapFrom(s => s.CloseDate));

            CreateMap<Provider, Models.Results.ProviderSummary>()
                .ForMember(p => p.Id, opt => opt.MapFrom(s => s.ProviderId))
                .ForMember(p => p.TrustStatus, opt => opt.Ignore());

            CreateMap<Models.Results.ProviderSummary, CalculateFunding.Common.ApiClient.Providers.Models.ProviderSummary>();
        }
    }
}
