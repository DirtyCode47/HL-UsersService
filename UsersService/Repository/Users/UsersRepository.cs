using Microsoft.EntityFrameworkCore;
using System.Data;
using UsersService.Entities;
using static Google.Protobuf.Reflection.UninterpretedOption.Types;

namespace UsersService.Repository.Users
{
    public class UsersRepository : Repository<User>,IUsersRepository
    {
        private UserAuthDbContext _dbContext { get; set; }

        public UsersRepository(UserAuthDbContext _dbContext) : base(_dbContext)
        {
            this._dbContext = _dbContext;
        }

        public IEnumerable<User> GetAllUsers()
        {
            return _dbContext.Users;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _dbContext.Users.ToListAsync();
        }

        public async Task<User> FindByPostCode(string post_code)
        {
            return await _dbContext?.Users?.FirstOrDefaultAsync(u => u.postCode == post_code);
        }
    }
}
