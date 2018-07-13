using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Users
{
    public class UserProfile : Reference
    {
        public UserProfile(string id, string name): base (id, name)
        { }

        public UserProfile(Reference user)
        {
            Id = user != null && !string.IsNullOrWhiteSpace(user.Id) ? user.Id : "Unknown";
            Name = user != null && !string.IsNullOrWhiteSpace(user.Name) ? user.Name : "Unknown";
        }
    }
}
