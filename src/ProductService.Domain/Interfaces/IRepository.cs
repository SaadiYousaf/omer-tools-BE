using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ProductService.Domain.Interfaces;

public interface IRepository<T> where T : class
{
    // Basic CRUD operations
    Task<T> GetByIdAsync(int id);
    Task<T> GetByIdAsync(int id, params string[] includeProperties);
    Task<T> GetByIdAsync(int id, params Expression<Func<T, object>>[] includeProperties);

    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> GetAllAsync(params string[] includeProperties);
    Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includeProperties);
    Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> predicate);
    Task<IEnumerable<T>> GetAsync(
        Expression<Func<T, bool>> predicate,
        params string[] includeProperties);
     Task<IEnumerable<T>> GetAsync(
        Expression<Func<T, bool>> predicate,
        params Expression<Func<T, object>>[] includeProperties);

    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    Task UpdateAsync(T entity);
    Task UpdateRangeAsync(IEnumerable<T> entities);
    Task DeleteAsync(T entity);
    Task DeleteRangeAsync(IEnumerable<T> entities);

    // Utility methods
    Task<bool> ExistsAsync(int id);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
    Task<int> CountAsync();
    Task<int> CountAsync(Expression<Func<T, bool>> predicate);
    Task SaveChangesAsync();
}