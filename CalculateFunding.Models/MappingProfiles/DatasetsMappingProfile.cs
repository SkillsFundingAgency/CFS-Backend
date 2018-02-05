using AutoMapper;
using CalculateFunding.Models.Datasets;

namespace CalculateFunding.Models.MappingProfiles
{
    public class DatasetsMappingProfile : Profile
    {
        public DatasetsMappingProfile()
        {
            CreateMap<CreateNewDatasetModel, CreateNewDatasetResponseModel>();
        }
    }
}
