using CalculateFunding.Models.Users;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Core.Services
{
    public class UserProfileProvider : IUserProfileProvider
    {
        private UserProfile _userProfile;

        public UserProfile GetUser()
        {
            return _userProfile;
        }

        public void SetUser(string id, string name)
        {
            Guard.IsNullOrWhiteSpace(id, nameof(id));
            Guard.IsNullOrWhiteSpace(name, nameof(name));

            _userProfile = new UserProfile(id, name);
        }
    }
}
