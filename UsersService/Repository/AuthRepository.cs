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

    }
}
