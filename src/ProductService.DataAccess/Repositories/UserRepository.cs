using Microsoft.EntityFrameworkCore;
using ProductService.DataAccess.Data;
using ProductService.Domain.Entites;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using System.Threading.Tasks;

namespace ProductService.DataAccess.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ProductDbContext _context;

        public UserRepository(ProductDbContext context)
        {
            _context = context;
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task CreateUserAsync(User user)
        {
            if (string.IsNullOrEmpty(user.Id))
            {
                user.Id = Guid.NewGuid().ToString();  
            }

            user.CreatedAt = DateTime.UtcNow;
            user.IsActive = true;

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UserExists(string email)
        {
            return await _context.Users
                .AnyAsync(u => u.Email == email);
        }
    }
}