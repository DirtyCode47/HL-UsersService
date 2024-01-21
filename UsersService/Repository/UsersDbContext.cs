using Microsoft.EntityFrameworkCore;
using UsersService.Entities;

namespace UsersService.Repository
{
    public class UsersDbContext: DbContext
    {
        public UsersDbContext(DbContextOptions<UsersDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
    }
}
