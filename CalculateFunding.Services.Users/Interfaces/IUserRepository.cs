using CalculateFunding.Models.Users;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Users.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetUserById(string id);

        Task<HttpStatusCode> SaveUser(User user);
    }
}
