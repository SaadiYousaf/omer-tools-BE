using Microsoft.EntityFrameworkCore;
using ProductService.DataAccess.Data;
using ProductService.Domain.Entites;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ProductService.DataAccess.Repositories
{
    public class Repository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly ProductDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(ProductDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<T> GetByIdAsync(string id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<T> GetByIdAsync(string id, params string[] includeProperties)
        {
            var query = _dbSet.AsQueryable();

            foreach (var includeProperty in includeProperties)
            {
                query = query.Include(includeProperty);
            }

            return await query.FirstOrDefaultAsync(e => e.Id == id);
        }
		public async Task<Blog> GetBySlugAsync(string slug, params string[] includeProperties)
		{
			var query = _context.Set<Blog>().AsQueryable();

			foreach (var includeProperty in includeProperties)
			{
				query = query.Include(includeProperty);
			}

			return await query.FirstOrDefaultAsync(b => b.Slug == slug && b.IsActive);
		}


		public async Task<T> GetByIdAsync(string id, params Expression<Func<T, object>>[] includeProperties)
        {
            var query = _dbSet.AsQueryable();

            foreach (var include in includeProperties)
            {
                query = query.Include(include);
            }

            return await query.FirstOrDefaultAsync(e => e.Id == id);
        }

		public async Task<Product> CheckByNameAsync(string name)
		{
			return await _context.Products
				.AsNoTracking()
				.FirstOrDefaultAsync(p => p.Name.Trim().ToLower() == name.Trim().ToLower());
		}

		public async Task<Product> CheckBySkuAsync(string sku)
		{
			return await _context.Products
				.AsNoTracking()
				.FirstOrDefaultAsync(p => p.SKU.Trim().ToUpper() == sku.Trim().ToUpper());
		}

		public async Task<Product> GetByNameAsync(string name)
		{
			return await _context.Products
				.AsNoTrackingWithIdentityResolution()
				.Include(u => u.Images)
				.Include(u => u.Brand)
				.Include(u => u.Subcategory)
				.Include(u => u.Subcategory.Category)
				.FirstOrDefaultAsync(u => u.CanonicalUrl == name.ToLower());
		}
		public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<IEnumerable<T>> GetAllAsync(params string[] includeProperties)
        {
            var query = _dbSet.AsQueryable();

            foreach (var includeProperty in includeProperties)
            {
                query = query.Include(includeProperty);
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includeProperties)
        {
            var query = _dbSet.AsQueryable();

            foreach (var include in includeProperties)
            {
                query = query.Include(include);
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public async Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> predicate, params string[] includeProperties)
        {
            var query = _dbSet.Where(predicate);

            foreach (var includeProperty in includeProperties)
            {
                query = query.Include(includeProperty);
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includeProperties)
        {
            var query = _dbSet.Where(predicate);

            foreach (var include in includeProperties)
            {
                query = query.Include(include);
            }

            return await query.ToListAsync();
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }


        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
        }

        public async Task UpdateAsync(T entity)
        {
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
            await Task.CompletedTask;
        }

        public async Task UpdateRangeAsync(IEnumerable<T> entities)
        {
            _dbSet.UpdateRange(entities);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(T entity)
        {
            if (_context.Entry(entity).State == EntityState.Detached)
            {
                _dbSet.Attach(entity);
            }
            _dbSet.Remove(entity);
            await Task.CompletedTask;
        }

        public async Task DeleteRangeAsync(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
            await Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(string id)
        {
            return await _dbSet.FindAsync(id) != null;
        }

        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        public async Task<int> CountAsync()
        {
            return await _dbSet.CountAsync();
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.CountAsync(predicate);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<T>> GetAsync(
            Expression<Func<T, bool>> predicate,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null)
        {
            IQueryable<T> query = _context.Set<T>().Where(predicate);

            if (orderBy != null)
            {
                query = orderBy(query);
            }

            return await query.ToListAsync();
        }

		public async Task<T> GetByFieldAsync(Expression<Func<T, bool>> predicate)
		{
			return await _dbSet
				.AsNoTracking()
				.FirstOrDefaultAsync(predicate);
		}

		public async Task<bool> ExistsByFieldAsync(Expression<Func<T, bool>> predicate)
		{
			return await _dbSet
				.AsNoTracking()
				.AnyAsync(predicate);
		}
		public async Task<IEnumerable<T>> GetAllAsync(Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null)
        {
            IQueryable<T> query = _dbSet.AsNoTracking();

            if (orderBy != null)
            {
                query = orderBy(query);
            }

            return await query.ToListAsync();
        }

		public IQueryable<T> GetQueryable()
		{
			return _dbSet.AsQueryable();
		}

	}
		public static class ProductRepositoryExtensions
		{
			// Extension method to build optimized query
			public static IQueryable<Product> BuildOptimizedQuery(
				this IQueryable<Product> query,
				string search = null,
				string brandId = null,
				string subcategoryId = null,
				bool? isFeatured = null,
				bool? isRedemption = null,
				bool? isActive = null,
				bool includeImages = true)
			{
				// Apply includes
				if (includeImages)
				{
					query = query.Include(p => p.Images.Where(i => i.IsActive));
				}

				// Apply filters
				if (!string.IsNullOrEmpty(search))
				{
					search = search.ToLower();
					query = query.Where(p =>
						p.Name.ToLower().Contains(search) ||
						p.SKU.ToLower().Contains(search) ||
						(p.TagLine != null && p.TagLine.ToLower().Contains(search)) ||
						(p.Description != null && p.Description.ToLower().Contains(search)));
				}

				if (!string.IsNullOrEmpty(brandId))
					query = query.Where(p => p.BrandId == brandId);

				if (!string.IsNullOrEmpty(subcategoryId))
					query = query.Where(p => p.SubcategoryId == subcategoryId);

				if (isFeatured.HasValue)
					query = query.Where(p => p.IsFeatured == isFeatured.Value);

				if (isRedemption.HasValue)
					query = query.Where(p => p.IsRedemption == isRedemption.Value);

				if (isActive.HasValue)
					query = query.Where(p => p.IsActive == isActive.Value);

				return query;
			}

			// Extension method for pagination
			public static async Task<PaginatedResult<Product>> GetPaginatedAsync(
				this IQueryable<Product> query,
				int page,
				int pageSize,
				string sortBy = "CreatedAt",
				bool sortDescending = true)
			{
				// Apply sorting
				query = ApplySorting(query, sortBy, sortDescending);

				// Get total count
				var total = await query.CountAsync();

				// Apply pagination
				var items = await query
					.Skip((page - 1) * pageSize)
					.Take(pageSize)
					.AsNoTracking()
					.ToListAsync();

				return new PaginatedResult<Product>
				{
					Data = items,
					Total = total,
					Page = page,
					PageSize = pageSize,
					TotalPages = (int)Math.Ceiling(total / (double)pageSize)
				};
			}

			private static IQueryable<Product> ApplySorting(
				IQueryable<Product> query,
				string sortBy,
				bool sortDescending)
			{
				return sortBy?.ToLower() switch
				{
					"name" => sortDescending
						? query.OrderByDescending(p => p.Name)
						: query.OrderBy(p => p.Name),
					"price" => sortDescending
						? query.OrderByDescending(p => p.Price)
						: query.OrderBy(p => p.Price),
					"createdat" => sortDescending
						? query.OrderByDescending(p => p.CreatedAt)
						: query.OrderBy(p => p.CreatedAt),
					"updatedat" => sortDescending
						? query.OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
						: query.OrderBy(p => p.UpdatedAt ?? p.CreatedAt),
					_ => query.OrderByDescending(p => p.CreatedAt)
				};
			}
		}
}