using AutoMapper;
using CalculateFunding.Models.Users;

namespace CalculateFunding.Models.MappingProfiles
{
    public class UsersMappingProfile : Profile
    {
        public UsersMappingProfile()
        {
            CreateMap<FundingStreamPermissionUpdateModel, FundingStreamPermission>()
                .ForMember(m => m.UserId, opt => opt.Ignore())
                .ForMember(m => m.FundingStreamId, opt => opt.Ignore());

            CreateMap<FundingStreamPermission, FundingStreamPermissionCurrent>();
        }
    }
}
