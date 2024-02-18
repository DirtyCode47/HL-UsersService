using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using UsersService.Entities;

namespace UsersService.Repository.Users
{
    public interface IUsersRepository:IRepository<User>
    {
        public IEnumerable<User> GetAllUsers();

        public Task<IEnumerable<User>> GetAllUsersAsync();

        public Task<User> FindByPostCode(string post_code);
    }
}
