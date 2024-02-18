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

        public IEnumerable<TEntity> GetAll()
        {
            return _dbContext.Set<TEntity>();
        }

        public void Complete()
        {
            _dbContext.SaveChanges();
        }

        public async Task<int> CompleteAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }
    }
}
