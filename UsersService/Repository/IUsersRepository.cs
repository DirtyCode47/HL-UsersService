using Microsoft.Extensions.Hosting;
using UsersService.Entities;

namespace UsersService.Repository
{
    public interface IUsersRepository
    {
        public User CreateUser(User user);
        public User DeleteUser(Guid id);
        public User UpdateUser(User user);
        public User GetUser(Guid id);
        public IEnumerable<User> FindUsersWithFilters(string fullname, uint role, string post_code);
    }
}
