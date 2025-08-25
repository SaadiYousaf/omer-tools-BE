

// ProductService.DataAccess/Repositories/UserPreferencesRepository.cs
using Microsoft.EntityFrameworkCore;
using ProductService.DataAccess.Data;
using ProductService.Domain.Entites;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using System.Threading.Tasks;

namespace ProductService.DataAccess.Repositories
{
    public class UserPreferencesRepository : IUserPreferencesRepository
    {
        private readonly ProductDbContext _context;

        public UserPreferencesRepository(ProductDbContext context)
        {
            _context = context;
        }

        public async Task<UserPreferences> GetByUserIdAsync(string userId)
        {
            return await _context.UserPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }

        public async Task CreateAsync(UserPreferences preferences)
        {
            await _context.UserPreferences.AddAsync(preferences);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(UserPreferences preferences)
        {
            _context.UserPreferences.Update(preferences);
            await _context.SaveChangesAsync();
        }
    }
}