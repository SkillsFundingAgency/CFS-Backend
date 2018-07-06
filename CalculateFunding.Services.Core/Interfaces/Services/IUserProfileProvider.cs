using CalculateFunding.Models.Users;

namespace CalculateFunding.Services.Core.Interfaces.Services
{
    public interface IUserProfileProvider
    {
        UserProfile GetUser();

        void SetUser(string id, string name);
    }
}
