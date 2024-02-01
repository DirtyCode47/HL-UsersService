using Microsoft.EntityFrameworkCore;
using UsersService.Entities;

namespace UsersService.Repository
{
    public class Repository<TEntity>:IRepository<TEntity> where TEntity : class
    {
        private UserAuthDbContext dbContext { get; set; }
        public Repository(UserAuthDbContext dbContext)
        {
            this.dbContext = dbContext;
        }
        public TEntity Create(TEntity entity)
        {
            return dbContext.Set<TEntity>().Add(entity).Entity;
        }

        public async Task<TEntity> CreateAsync(TEntity entity)
        {
            var added_user = await dbContext.Set<TEntity>().AddAsync(entity);
            return added_user.Entity;
        }

        public async Task<TEntity> Delete(Guid id)
        {
            return dbContext.Set<TEntity>().Remove(await GetAsync(id)).Entity;
        }

        public TEntity Update(TEntity entity)
        {
            return dbContext.Set<TEntity>().Update(entity).Entity;
        }

        public TEntity Get(Guid id)
        {
            return dbContext.Set<TEntity>().Find(id);
        }

        public async Task<TEntity> GetAsync(Guid id)
        {
            return await dbContext.Set<TEntity>().FindAsync(id);
        }

        //public User GetUserByLogin(string login)
        //{
        //    var user = dbContext.Users.FirstOrDefault(u => u.login == login);
        //    return user;
        //}

        //public IEnumerable<User> FindUsersWithFilters(string fullname, string role, string post_code)
        //{
        //    var splittedFullName = fullname.Split(' ');
        //    return dbContext.Users.Where(u => isNeededUser(u, splittedFullName, role, post_code));
        //}

        public IEnumerable<TEntity> GetAll()
        {
            return dbContext.Set<TEntity>();
        }

        //public async Task<User?> FindByPostCode(string post_code)
        //{
        //    return await dbContext.Users.FirstOrDefaultAsync(p => p.post_code == post_code);
        //}

        public void Complete()
        {
            dbContext.SaveChanges();
        }

        public async Task<int> CompleteAsync()
        {
            return await dbContext.SaveChangesAsync();
        }

        //private bool isNeededUser(User user, string[] nameParts, string role, string post_code)
        //{
        //    bool skipName = false;
        //    bool skipRole = false;
        //    bool skipPostCode = false;

        //    int neededCoincidenceCount = 3;
        //    List<bool> coincidence = new List<bool>();

        //    if (nameParts.Length == 1 && nameParts[0] == "")
        //    {
        //        skipName = true;
        //        neededCoincidenceCount--;
        //    }
        //    if (role == "")
        //    {
        //        skipRole = true;
        //        neededCoincidenceCount--;
        //    }
        //    if (post_code == "")
        //    {
        //        skipPostCode = true;
        //        neededCoincidenceCount--;
        //    }

        //    if (neededCoincidenceCount == 0) { return false; }


        //    if (!skipName)
        //    {
        //        //Кол-во совпадений
        //        int coincidenceCount = 0;

        //        foreach (var part in nameParts)
        //        {
        //            if (user.first_name.Contains(part) ||
        //                user.middle_name.Contains(part) ||
        //                user.last_name.Contains(part))
        //            {
        //                coincidenceCount++;
        //            }
        //        }

        //        //Если все части ФИО подошли
        //        if (coincidenceCount == nameParts.Length)
        //            coincidence.Add(true);
        //        else return false;
        //    }
        //    if (!skipRole)
        //    {
        //        if (user.role == role)
        //            coincidence.Add(true);
        //        else return false;
        //    }
        //    if (!skipPostCode)
        //    {
        //        if (user.post_code == post_code)
        //            coincidence.Add(true);
        //        else return false;
        //    }


        //    if (coincidence.Count == neededCoincidenceCount)
        //    {
        //        return true;
        //    }

        //    return false;
        //}
    }
}
