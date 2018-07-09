using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Users
{
    public class UserProfile : Reference
    {
        public UserProfile(string id, string name): base (id, name)
        { }
    }
}
