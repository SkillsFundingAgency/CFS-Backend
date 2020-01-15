using AutoMapper;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.ViewModels;

namespace CalculateFunding.Services.Datasets.MappingProfiles
{
    public class CalculationsMappingProfile : Profile
    {
        public CalculationsMappingProfile()
        {
            CreateMap<DatasetRelationshipSummary, Common.ApiClient.Calcs.Models.DatasetRelationshipSummary>();
            CreateMap<Models.Datasets.Schema.DatasetDefinition, Common.ApiClient.Calcs.Models.Schema.DatasetDefinition>();
            CreateMap<Common.ApiClient.Calcs.Models.Build, Build>();
            CreateMap<Common.ApiClient.Calcs.Models.BuildProject, BuildProject>();
        }
    }
}
