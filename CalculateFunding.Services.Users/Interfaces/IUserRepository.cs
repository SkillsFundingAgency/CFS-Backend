using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.Users;

namespace CalculateFunding.Services.Users.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetUserById(string id);

        Task<HttpStatusCode> SaveUser(User user);

        Task<FundingStreamPermission> GetFundingStreamPermission(string userId, string fundingStreamId);

        Task<IEnumerable<FundingStreamPermission>> GetFundingStreamPermissions(string userId);

        Task<IEnumerable<FundingStreamPermission>> GetUsersWithFundingStreamPermissions(string fundingStreamId);

        Task<HttpStatusCode> UpdateFundingStreamPermission(FundingStreamPermission fundingStreamPermission);

        Task<IEnumerable<User>> GetAllUsers();
    }
}
