using AutoMapper;
using CalculateFunding.Common.ApiClient.DataSets.Models;

namespace CalculateFunding.Services.Scenarios.MappingProfiles
{
    public class DatasetsMappingProfile : Profile
    {
        public DatasetsMappingProfile()
        {
            CreateMap<DatasetSpecificationRelationshipViewModel, Models.Datasets.ViewModels.DatasetSpecificationRelationshipViewModel>();
            CreateMap<DatasetDefinitionViewModel, Models.Datasets.ViewModels.DatasetDefinitionViewModel>();
            CreateMap<DatasetDefinition, DatasetDefinition>();
            CreateMap<TableDefinition, TableDefinition>();
            CreateMap<FieldDefinition, FieldDefinition>();
            CreateMap<FieldType, FieldType>();
            CreateMap<IdentifierFieldType, IdentifierFieldType>();
        }
    }
}
