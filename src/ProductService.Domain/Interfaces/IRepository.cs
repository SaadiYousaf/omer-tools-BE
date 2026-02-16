using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ProductService.Domain.Entities;

namespace ProductService.Domain.Interfaces
{
    public interface IRepository<T> where T : BaseEntity
    {
        // Basic CRUD operations
        Task<T> GetByIdAsync(string id);
        Task<T> GetByIdAsync(string id, params string[] includeProperties);
        Task<Product> GetByNameAsync(string name);

		Task<T> GetByIdAsync(string id, params Expression<Func<T, object>>[] includeProperties);

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
        Task<bool> ExistsAsync(string id);
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
        Task<int> CountAsync();
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);
        Task SaveChangesAsync();
        Task<IEnumerable<T>> GetAsync(
            Expression<Func<T, bool>> predicate,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null);

        Task<IEnumerable<T>> GetAllAsync(
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null);

        IQueryable<T> GetQueryable();

		Task<Product> CheckByNameAsync(string name);
		Task<Product> CheckBySkuAsync(string sku);

	}
}