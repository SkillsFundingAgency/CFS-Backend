using AutoMapper;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.ViewModels;

namespace CalculateFunding.Models.MappingProfiles
{
    public class DatasetsMappingProfile : Profile
    {
        public DatasetsMappingProfile()
        {
            CreateMap<CreateNewDatasetModel, NewDatasetVersionResponseModel>()
                .ForMember(c => c.BlobUrl, opt => opt.Ignore())
                .ForMember(c => c.DatasetId, opt => opt.Ignore())
                .ForMember(c => c.Author, opt => opt.Ignore())
                .ForMember(c => c.Version, opt => opt.MapFrom(c => 1));

            CreateMap<DatasetVersionUpdateModel, NewDatasetVersionResponseModel>()
                .ForMember(c => c.BlobUrl, opt => opt.Ignore())
                .ForMember(c => c.DatasetId, opt => opt.Ignore())
                .ForMember(c => c.Author, opt => opt.Ignore())
                .ForMember(c => c.Name, opt => opt.Ignore())
                .ForMember(c => c.Description, opt => opt.Ignore())
                .ForMember(c => c.DefinitionId, opt => opt.Ignore())
                .ForMember(c => c.Version, opt => opt.Ignore());



            CreateMap<Dataset, DatasetViewModel>()
                .ForMember(m => m.Versions, opt => opt.MapFrom(s => s.History));

            CreateMap<DatasetVersion, DatasetVersionViewModel>();
        }
    }
}
