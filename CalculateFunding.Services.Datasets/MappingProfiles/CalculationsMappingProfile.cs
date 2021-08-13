using AutoMapper;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Datasets.ViewModels;

namespace CalculateFunding.Services.Datasets.MappingProfiles
{
    public class CalculationsMappingProfile : Profile
    {
        public CalculationsMappingProfile()
        {
            CreateMap<DatasetRelationshipSummary, Common.ApiClient.Calcs.Models.DatasetRelationshipSummary>();
            CreateMap<PublishedSpecificationConfiguration, Common.ApiClient.Calcs.Models.PublishedSpecificationConfiguration>();
            CreateMap<PublishedSpecificationItem, Common.ApiClient.Calcs.Models.PublishedSpecificationItem>();
            CreateMap<FieldType, Common.ApiClient.Calcs.Models.Schema.FieldType>();
            CreateMap<DatasetDefinition, Common.ApiClient.Calcs.Models.Schema.DatasetDefinition>();
            CreateMap<TableDefinition, Common.ApiClient.Calcs.Models.Schema.TableDefinition>();
            CreateMap<FieldDefinition, Common.ApiClient.Calcs.Models.Schema.FieldDefinition>();
            CreateMap<Common.ApiClient.Calcs.Models.CompilerMessage, CompilerMessage>();
            CreateMap<Common.ApiClient.Calcs.Models.SourceFile, SourceFile>();
            CreateMap<Common.ApiClient.Calcs.Models.SourceLocation, SourceLocation>();
            CreateMap<Common.ApiClient.Calcs.Models.Severity, Severity>();
            CreateMap<Common.ApiClient.Calcs.Models.Schema.IdentifierFieldType, IdentifierFieldType>();
            CreateMap<Common.ApiClient.Calcs.Models.Schema.FieldType, FieldType>();
            CreateMap<Common.ApiClient.Calcs.Models.Schema.FieldDefinition, FieldDefinition>();
            CreateMap<Common.ApiClient.Calcs.Models.Schema.TableDefinition, TableDefinition>();
            CreateMap<Common.ApiClient.Calcs.Models.Schema.DatasetDefinition, DatasetDefinition>();
            CreateMap<Common.ApiClient.Calcs.Models.DatasetRelationshipSummary, DatasetRelationshipSummary>();
            CreateMap<Common.ApiClient.Calcs.Models.PublishedSpecificationConfiguration, PublishedSpecificationConfiguration>();
            CreateMap<Common.ApiClient.Calcs.Models.PublishedSpecificationItem, PublishedSpecificationItem>();
            CreateMap<Common.ApiClient.Calcs.Models.Build, Build>();
            CreateMap<Common.ApiClient.Calcs.Models.BuildProject, BuildProject>();
            CreateMap<Common.ApiClient.Calcs.Models.Job, Job>();
        }
    }
}
