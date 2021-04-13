using CalculateFunding.Models.Users;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Users.Interfaces
{
    public interface IUsersCsvGenerator
    {
        Task<FundingStreamPermissionCurrentDownloadModel> Generate(UserPermissionCsvGenerationMessage message);
    }
}
