using Microsoft.EntityFrameworkCore;
using UsersService.Entities;

namespace UsersService.Repository
{
    public class Repository<TEntity>:IRepository<TEntity> where TEntity : class
    {
        private UserAuthDbContext _dbContext { get; set; }
        public Repository(UserAuthDbContext dbContext)
        {
            this._dbContext = dbContext;
        }
        public TEntity Create(TEntity entity)
        {
            return _dbContext.Set<TEntity>().Add(entity).Entity;
        }

        public async Task<TEntity> CreateAsync(TEntity entity)
        {
            var added_user = await _dbContext.Set<TEntity>().AddAsync(entity);
            return added_user.Entity;
        }

        public async Task<TEntity> Delete(Guid id)
        {
            return _dbContext.Set<TEntity>().Remove(await GetAsync(id)).Entity;
        }

        public TEntity Update(TEntity entity)
        {
            return _dbContext.Set<TEntity>().Update(entity).Entity;
        }

        public TEntity? Get(Guid id)
        {
            return _dbContext.Set<TEntity>().Find(id) ?? null;
        }

        public async Task<TEntity?> GetAsync(Guid id)
        {
            return await _dbContext.Set<TEntity>().FindAsync(id);
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
            return _dbContext.Set<TEntity>();
        }

        //public async Task<User?> FindByPostCode(string post_code)
        //{
        //    return await dbContext.Users.FirstOrDefaultAsync(p => p.post_code == post_code);
        //}

        public void Complete()
        {
            _dbContext.SaveChanges();
        }

        public async Task<int> CompleteAsync()
        {
            return await _dbContext.SaveChangesAsync();
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
