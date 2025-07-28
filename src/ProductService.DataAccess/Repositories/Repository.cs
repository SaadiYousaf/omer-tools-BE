using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ProductService.DataAccess.Data;
using ProductService.Domain.Interfaces;

namespace ProductService.DataAccess.Repositories
{
    public class Repository<T> : IRepository<T>
        where T : class
    {
        protected readonly ProductDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(ProductDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<T> GetByIdAsync(int id) => await _dbSet.FindAsync(id);

        public async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();

        public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

        public async Task UpdateAsync(T entity)
        {
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
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

        public async Task<bool> ExistsAsync(int id) => await _dbSet.FindAsync(id) != null;

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
