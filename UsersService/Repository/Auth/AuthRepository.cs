using Microsoft.EntityFrameworkCore;
using UsersService.Entities;

namespace UsersService.Repository.Auth
{
    public class AuthRepository : Repository<AuthInfo>, IAuthRepository
    {
        public UserAuthDbContext _dbContext { get; set; }

        public AuthRepository(UserAuthDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public AuthInfo? GetAuthInfoByLogin(string login)
        {
            return _dbContext?.AuthInfo?.FirstOrDefault(u => u.login == login);
        }
    }
}
