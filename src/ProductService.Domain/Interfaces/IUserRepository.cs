// Domain/Interfaces/IUserRepository.cs
using ProductService.Domain.Entites;
using ProductService.Domain.Entities;
using System.Threading.Tasks;

namespace ProductService.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<User> GetUserByEmailAsync(string email);
        Task CreateUserAsync(User user);
        Task<bool> UserExists(string email);
    }
}