using CalculateFunding.Common.Models;
using CalculateFunding.Models.Users;

namespace CalculateFunding.Services.Core.Extensions
{
    public static class ReferenceExtensions
    {
        public static UserProfile ToUserProfile(this Reference reference)
        {
            return new UserProfile(reference.Id, reference.Name);
        }
    }
}
