using Microsoft.EntityFrameworkCore;
using ProductService.DataAccess.Data;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using System;
using System.Threading.Tasks;
using UserService.Domain.Entities;

namespace ProductService.DataAccess.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ProductDbContext _context;

        public UserRepository(ProductDbContext context)
        {
            _context = context;
        }

        public async Task<User> GetByIdAsync(string id)
        {
            return await _context.Users
                .Include(u => u.Addresses)
                .Include(u => u.PaymentMethods)
                .Include(u => u.Preferences)
                .FirstOrDefaultAsync(u => u.Id == id && u.IsActive);
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Addresses)
                .Include(u => u.PaymentMethods)
                .Include(u => u.Preferences)
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email && u.IsActive);
        }

        public async Task CreateAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            var trackedEntity = await _context.Users.FindAsync(user.Id);
            if (trackedEntity == null)
            {
                _context.Users.Update(user);
            }
            else
            {
                _context.Entry(trackedEntity).CurrentValues.SetValues(user);
            }
            await _context.SaveChangesAsync();
        }


        public async Task<bool> DeleteAsync(string id)
        {
            var user = await GetByIdAsync(id);
            if (user == null) return false;

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task AddRefreshTokenAsync(string userId, string refreshToken, DateTime expiry)
        {
            var user = await GetByIdAsync(userId);
            if (user != null)
            {
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiry = expiry;
                await _context.SaveChangesAsync(); 
            }
        }

        public async Task<User> GetUserByRefreshTokenAsync(string refreshToken)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken && u.RefreshTokenExpiry > DateTime.UtcNow);
        }

        public async Task RevokeRefreshTokenAsync(string userId)
        {
            var user = await GetByIdAsync(userId);
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiry = null;
                await _context.SaveChangesAsync();
            }
        }
    }
}
