using System.Linq.Expressions;

namespace TaskInventoryApi.Repositories;

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate); // This method allows for filtering based on a predicate
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
}