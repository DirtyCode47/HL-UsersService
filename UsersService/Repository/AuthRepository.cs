using Microsoft.EntityFrameworkCore;
using UsersService.Entities;

namespace UsersService.Repository
{
    public class AuthRepository:Repository<AuthInfo>
    {
        public UserAuthDbContext dbContext { get; set; }

        public AuthRepository(UserAuthDbContext dbContext):base(dbContext)
        {
            
        }
        //public async Task<AuthInfo?> GetAuthInfoByLogin(string login)
        //{
        //    var result = await dbContext?.AuthInfo?.FirstOrDefaultAsync(u => u.login == login);
        //    return result ?? null;
        //}

        public AuthInfo? GetAuthInfoByLogin(string login)
        {
            return dbContext?.AuthInfo?.FirstOrDefault(u => u.login == login);
        }
    }
}
