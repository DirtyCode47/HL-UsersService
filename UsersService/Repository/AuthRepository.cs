using Microsoft.EntityFrameworkCore;
using UsersService.Entities;

namespace UsersService.Repository
{
    public class AuthRepository:Repository<AuthInfo>
    {
        public UserAuthDbContext _dbContext { get; set; }

        public AuthRepository(UserAuthDbContext dbContext):base(dbContext)
        {
            this._dbContext = dbContext;
        }
        //public async Task<AuthInfo?> GetAuthInfoByLogin(string login)
        //{
        //    var result = await dbContext?.AuthInfo?.FirstOrDefaultAsync(u => u.login == login);
        //    return result ?? null;
        //}

        public AuthInfo? GetAuthInfoByLogin(string login)
        {
            return _dbContext?.AuthInfo?.FirstOrDefault(u => u.login == login);
        }
    }
}
