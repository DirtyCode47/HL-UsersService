using Microsoft.EntityFrameworkCore;

namespace UsersService.Repository
{
    public interface IRepository<TEntity>
    {
        public TEntity Create(TEntity entity);

        public Task<TEntity> CreateAsync(TEntity entity);

        public Task<TEntity> Delete(Guid id);

        public TEntity Update(TEntity entity);

        public TEntity? Get(Guid id);

        public Task<TEntity?> GetAsync(Guid id);

        public IEnumerable<TEntity> GetAll();

        public void Complete();

        public Task<int> CompleteAsync();
    }
}
