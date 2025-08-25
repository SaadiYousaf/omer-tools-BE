using UserService.Domain.Entities;

public interface IUserRepository
{
    Task<User> GetByIdAsync(string id);
    Task<User> GetByEmailAsync(string email);
    Task<bool> UserExistsAsync(string email);
    Task CreateAsync(User user);
    Task UpdateAsync(User user);
    Task<bool> DeleteAsync(string id);
    Task AddRefreshTokenAsync(string userId, string refreshToken, DateTime expiry);
    Task<User> GetUserByRefreshTokenAsync(string refreshToken);
    Task RevokeRefreshTokenAsync(string userId);
}