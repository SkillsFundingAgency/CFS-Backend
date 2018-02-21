using AutoMapper;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.ViewModels;

namespace CalculateFunding.Models.MappingProfiles
{
    public class DatasetsMappingProfile : Profile
    {
        public DatasetsMappingProfile()
        {
            CreateMap<CreateNewDatasetModel, CreateNewDatasetResponseModel>()
                .ForMember(c => c.BlobUrl, opt => opt.Ignore())
                .ForMember(c => c.DatasetId, opt => opt.Ignore())
                .ForMember(c => c.Author, opt => opt.Ignore());

            CreateMap<Dataset, DatasetViewModel>()
                .ForMember(m => m.Versions, opt => opt.MapFrom(s => s.History));

            CreateMap<DatasetVersion, DatasetVersionViewModel>();
        }
    }
}
