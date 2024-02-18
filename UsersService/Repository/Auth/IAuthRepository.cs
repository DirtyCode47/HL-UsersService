using Microsoft.EntityFrameworkCore;
using UsersService.Entities;

namespace UsersService.Repository.Auth
{
    public interface IAuthRepository:IRepository<AuthInfo>
    {
        public AuthInfo? GetAuthInfoByLogin(string login);
    }
}
