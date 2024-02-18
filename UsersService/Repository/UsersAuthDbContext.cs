using Microsoft.EntityFrameworkCore;
using UsersService.Entities;

namespace UsersService.Repository
{
    public class UserAuthDbContext: DbContext
    {
        public UserAuthDbContext(DbContextOptions<UserAuthDbContext> options) : base(options) 
        { 
            
        }

        public DbSet<User> Users { get; set; }
        public DbSet<AuthInfo> AuthInfo { get; set; }
    }
}
