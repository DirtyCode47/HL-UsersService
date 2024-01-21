using Microsoft.EntityFrameworkCore;
using System.Data;
using UsersService.Entities;
using static Google.Protobuf.Reflection.UninterpretedOption.Types;

namespace UsersService.Repository
{
    public class UsersRepository : IUsersRepository
    {
        private UsersDbContext dbContext { get; set; }

        public UsersRepository(UsersDbContext _dbContext) 
        { 
            dbContext = _dbContext;
        }

        public User CreateUser(User user)
        {
            return dbContext.Users.Add(user).Entity;
        }

        public User DeleteUser(Guid id)
        {
            return dbContext.Users.Remove(GetUser(id)).Entity;
        }

        public User UpdateUser(User user)
        {
            return dbContext.Users.Update(user).Entity;
        }

        public User GetUser(Guid id)
        {
            return dbContext.Users.Find(id);
        }

        public IEnumerable<User> FindUsersWithFilters(string fullname, uint role, string post_code)
        {
            var splittedFullName = fullname.Split(' ');
            return dbContext.Users.Where(u => isNeededUser(u, splittedFullName, role, post_code));
        }

        public void Complete()
        {
            dbContext.SaveChanges();
        }

        private bool isNeededUser(User user, string[] nameParts, uint role, string post_code)
        {
            bool skipName = false;
            bool skipRole = false;
            bool skipPostCode = false;

            int neededCoincidenceCount = 3;
            List<bool> coincidence = new List<bool>();

            if (nameParts.Length == 1 && nameParts[0] == "")
            {
                skipName = true;
                neededCoincidenceCount--;
            }
            if (role == 0)
            {
                skipRole = true;
                neededCoincidenceCount--;
            }
            if (post_code == "")
            {
                skipPostCode = true;
                neededCoincidenceCount--;
            }
            if(neededCoincidenceCount == 0) { return false; }


            if (!skipName)
            {
                //Кол-во совпадений
                int coincidenceCount = 0;

                foreach (var part in nameParts)
                {
                    if (user.first_name.Contains(part) ||
                        user.middle_name.Contains(part) ||
                        user.last_name.Contains(part))
                    {
                        coincidenceCount++;
                    }
                }

                //Если все части ФИО подошли
                if (coincidenceCount == nameParts.Length)
                    coincidence.Add(true);
                else return false;
            }
            if (!skipRole)
            {
                if (user.role == role)
                    coincidence.Add(true);
                else return false;
            }
            if (!skipPostCode)
            {
                if (user.post_code == post_code)
                    coincidence.Add(true);
                else return false;
            }


            if(coincidence.Count == neededCoincidenceCount)
            {
                return true;
            }

            return false;
        }
    }
}
