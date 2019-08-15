using AutoMapper;
using CalculateFunding.Models.Publishing;
using CommonModels = CalculateFunding.Common.ApiClient.Calcs.Models;

namespace CalculateFunding.Models.MappingProfiles
{
    public class PublishingMappingProfile : Profile
    {
        public PublishingMappingProfile()
        {
            CreateMap<TemplateMapping, CommonModels.TemplateMapping>();
        }
    }
}
