using Microsoft.EntityFrameworkCore;
using System.Data;
using UsersService.Entities;
using static Google.Protobuf.Reflection.UninterpretedOption.Types;

namespace UsersService.Repository
{
    public class UsersRepository : Repository<User>
    {
        private UserAuthDbContext dbContext { get; set; }

        public UsersRepository(UserAuthDbContext _dbContext):base(_dbContext) 
        { 
            
        }

        //public User CreateUser(User user)
        //{
        //    return dbContext.Users.Add(user).Entity;
        //}

        //public async Task<User> CreateUserAsync(User user)
        //{
        //    var added_user = await dbContext.Users.AddAsync(user);
        //    return added_user.Entity;
        //}

        //public User DeleteUser(Guid id)
        //{
        //    return dbContext.Users.Remove(GetUser(id)).Entity;
        //}

        //public User UpdateUser(User user)
        //{
        //    return dbContext.Users.Update(user).Entity;
        //}

        //public User GetUser(Guid id)
        //{
        //    return dbContext.Users.Find(id);
        //}

        //public async Task<User> GetUserAsync(Guid id)
        //{
        //    return await dbContext.Users.FindAsync(id);
        //}

        //public User GetUserByLogin(string login)
        //{
        //    var user = dbContext.Users.FirstOrDefault(u => u.login == login);
        //    return user;
        //}

        public IEnumerable<User> FindUsersWithFilters(string fullname, string role, string post_code)
        {
            var splittedFullName = fullname.Split(' ');
            return dbContext.Users.Where(u => isNeededUser(u, splittedFullName, role, post_code));
        }

        public IEnumerable<User> GetAllUsers()
        {
            return dbContext.Users;
        }

        public async Task<User> FindByPostCode(string post_code)
        {
            //// Проверка на null для безопасности
            //if (dbContext != null && dbContext.Users != null)
            //{
            //    return dbContext.Users.FirstOrDefault(u => u.post_code == post_code);
            //}

            //// Обработка случая, когда _dbContext или _dbContext.Users равны null
            //return null;
            return await dbContext?.Users?.FirstOrDefaultAsync(u => u.postCode == post_code);
        }

        //public void Complete()
        //{
        //    dbContext.SaveChanges();
        //}

        //public async Task<int> CompleteAsync()
        //{
        //    return await dbContext.SaveChangesAsync();
        //}

        private bool isNeededUser(User user, string[] nameParts, string role, string post_code)
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
            if (role == "")
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
                    if (user.firstName.Contains(part) ||
                        user.middleName.Contains(part) ||
                        user.lastName.Contains(part))
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
                if (user.postCode == post_code)
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
