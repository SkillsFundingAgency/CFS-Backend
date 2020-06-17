using AutoMapper;
using PolicyApiClientModel = CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Api.External.V3.MappingProfiles
{
    public class ExternalServiceMappingProfile : Profile
    {
        public ExternalServiceMappingProfile()
        {
            CreateMap<PolicyApiClientModel.FundingStream, Models.FundingStream>();
            CreateMap<PolicyApiClientModel.FundingPeriod, Models.FundingPeriod>();
            CreateMap<PolicyApiClientModel.PublishedFundingTemplate, Models.PublishedFundingTemplate>()
                .ForMember(dest => dest.MajorVersion, opt => opt.MapFrom(src => GetVersionFromTemplateVersion(src.TemplateVersion, true)))
                .ForMember(dest => dest.MinorVersion, opt => opt.MapFrom(src => GetVersionFromTemplateVersion(src.TemplateVersion, false)));
        }

        private static string GetVersionFromTemplateVersion(string templareVersion, bool major)
        {
            if(string.IsNullOrWhiteSpace(templareVersion))
            {
                return string.Empty;
            }

            string[] versionParts = templareVersion.Split(".");
            if(versionParts.Length >= 2)
            {
                return major ? versionParts[0] : versionParts[1];
            }

            return string.Empty;
        }
    }
}
